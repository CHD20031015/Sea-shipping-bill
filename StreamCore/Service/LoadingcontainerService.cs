using OfficeOpenXml;
using SqlSugar;
using StreamCore.Method;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.StreamModel;
using StreamCore.StreamModel.DTO;
using System.ComponentModel;
using static Npgsql.Replication.PgOutput.Messages.RelationMessage;

namespace StreamCore.Service
{
    public class LoadingcontainerService
    {
        private readonly ISqlSugarClient _db;
        public LoadingcontainerService([FromKeyedServices("stream")]ISqlSugarClient db)
        { 
            _db= db;
        }
        // 纸箱实际体积（单位：m³）
        public static readonly double paper_box_5 = 0.29 * 0.17 * 0.19 * 1000000;  // 9376
        public static readonly double paper_box_3 = 0.43 * 0.21 * 0.27 * 1000000;  // 24381
        public static readonly double paper_box_1 = 0.53 * 0.29 * 0.37 * 1000000;  // 56869

        //40海运箱体积
        public static readonly double transportBox_40 = 12.05 * 2.35 * 2.68 * 1000000; //75,890,900 
        //20海运箱体积
        public static readonly double transportBox_20 = 5.69 * 2.13 * 2.18 * 1000000;

        //装箱装柜主函数
        public async Task<Paperbox> SOloadbox(DateTime startTime, DateTime endTime)
        {
            //获取所有SO订单
            var items = await _db.Queryable<Sale, Goods>((s, g) => s.Barcode == g.Barcode)
                    .Where((s, g) => s.Outstore_time >= startTime && s.Outstore_time <= endTime && s.Is_over == 0)
                    .OrderBy((s, g) => s.Outstore_time)
                    .Select((s, g) => new SaleItemDto
                    {
                        OutstoreTime = s.Outstore_time,  //出库时间
                        CustomerName = s.Customer_name,  //买家名字
                        Country = s.Sale_country,        //销售国家
                        SaleNo = s.Sale_no,        //销售单号
                        Barcode = s.Barcode,        //商品编码
                        GoodsName = g.Goods_name, //商品名称
                        Number = SqlFunc.ToDouble(s.Number), //商品数量
                        Volume = SqlFunc.ToDouble(g.Volume), //商品体积
                        Remark = g.Remark,           //商品是否需要装箱
                        SaleId = s.Id
                    }).MergeTable().ToListAsync();
            // 保存所有纸箱和详情
            var allPaperBoxes = new List<Paperbox>();
            var allDetails = new List<Paperboxdetail>();
            var allContainers = new List<Transportbox>(); // 收集所有柜子
            var allrecords = new List<Supplier_record>(); // 收集所有费用记录
            var saleIds = items.Select(i => i.SaleId).Distinct().ToList(); //获取所有销售单号主键
            //按照国家进行分组
            var countryGroups = items.GroupBy(x => x.Country);
            foreach (var countryGroup in countryGroups)
            {
                var country = countryGroup.Key;
                // 该国家订单按时间排序
                var countryItems = countryGroup.OrderBy(x => x.OutstoreTime).ToList();
                // 对该国家内部订单动态按照3天/批次 进行分批处理
                DateTime? batchstart = null;
                var batches = new List<List<SaleItemDto>>(); // 该国家的批次列表
                var daybatch = new List<SaleItemDto>();      // 当前批次数据
                foreach (var item in countryItems)
                {
                    var date = item.OutstoreTime.Value.Date;
                    if (batchstart == null)
                    {
                        batchstart = date;
                        daybatch.Add(item);
                    }
                    else if ((date - batchstart.Value).Days <= 2)
                    {
                        daybatch.Add(item);
                    }
                    else
                    {
                        batches.Add(daybatch);
                        daybatch = new List<SaleItemDto>();
                        batchstart = date;
                        daybatch.Add(item);
                    }
                }
                if (daybatch.Any())
                    batches.Add(daybatch);
                int batchIndex = 1;
                foreach (var batch in batches)
                {
                    batchIndex++;

                    var batchPaperBoxes = new List<Paperbox>();        // 当前批次国家的纸箱
                    var batchDetails = new List<Paperboxdetail>();    // 当前批次国家的纸箱详情
                    // 按照收货人分组
                    var customerGroups = batch.GroupBy(x => x.CustomerName);
                    foreach (var customerGroup in customerGroups)
                    {
                        var customerName = customerGroup.Key;
                        // 可装箱商品（Remark 为空）和大件商品（Remark 不为空）
                        var normalItems = customerGroup.Where(x => string.IsNullOrEmpty(x.Remark)).ToList();
                        var largeItems = customerGroup.Where(x => !string.IsNullOrEmpty(x.Remark)).ToList();
                        // 处理可装箱商品
                        if (normalItems.Any())
                        {
                            //拆分为单件商品列表
                            var saleItems = new List<SaleItemDto>();
                            foreach (var row in normalItems)
                            {
                                int count = (int)row.Number;
                                for (int i = 0; i < count; i++)
                                {
                                    saleItems.Add(new SaleItemDto
                                    {
                                        GoodsName = row.GoodsName,
                                        Volume = row.Volume,
                                        SaleNo = row.SaleNo,
                                        Barcode = row.Barcode
                                    });
                                }
                            }
                            var deliverDate = customerGroup.First().OutstoreTime;
                            //调用装箱方法
                            var (paperBoxes, details) = PackItemsIntoCartons(saleItems, customerName, country, deliverDate);
                            batchPaperBoxes.AddRange(paperBoxes);
                            batchDetails.AddRange(details);
                        }
                        //处理大件商品
                        if (largeItems.Any())
                        {
                            foreach (var row in largeItems)
                            {
                                int count = (int)row.Number;
                                for (int i = 0; i < count; i++)
                                {
                                    string boxNo = $"PB-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                                    var box = new Paperbox
                                    {
                                        Paper_box_no = boxNo,
                                        Type = 0,
                                        Reality_volume = (decimal)row.Volume,
                                        Customer_name = row.CustomerName,
                                        Country_name = row.Country,
                                        Deliver_time = row.OutstoreTime,
                                        Create_time = DateTime.Now,
                                        Is_box = 1,
                                        Sale_no = row.SaleNo,
                                        Barcode = row.Barcode
                                    };
                                    batchPaperBoxes.Add(box);
                                }
                            }
                        }
                    }
                    // 装箱之后，将该批次国家纸箱进行装柜
                    if (batchPaperBoxes.Any())
                    {
                        var cartonItems = batchPaperBoxes.Select(b =>
                        {
                            double Volume;
                            if (b.Type == 1)
                                Volume = paper_box_1;
                            else if (b.Type == 3)
                                Volume = paper_box_3;
                            else if (b.Type == 5)
                                Volume = paper_box_5;
                            else // Type == 0 (大件商品)
                                Volume = (double)b.Reality_volume;
                            return new Item
                            {
                                Name = b.Paper_box_no,
                                Volume = Volume
                            };
                        }).OrderByDescending(i => i.Volume).ToList();

                        var queue = new Queue<Item>(cartonItems);
                        // 调用装柜方法
                        var containerResult = BoxToContainer.LoadBoxs(queue);
                        // 计算该批次统一的 Deliver_time（取所有纸箱中最大的 Deliver_time，即最晚订单出库时间）
                        var batchDeliverTime = batchPaperBoxes.Max(b => b.Deliver_time)?.Date;
                        // 生成柜子记录并更新纸箱柜号await SaveContainers(batchPaperBoxes, containerResult, country,batchDeliverTime);
                        var (containers, records) = await SaveContainers(batchPaperBoxes, containerResult, country, batchDeliverTime);
                        allContainers.AddRange(containers);
                        allrecords.AddRange(records);
                        // 打印调试信息（仅马来西亚示例，可根据需要调整）
                        if (country == "马来西亚")
                        {
                            // 统计纸箱类型
                            var type1Count = batchPaperBoxes.Count(b => b.Type == 1);
                            var type3Count = batchPaperBoxes.Count(b => b.Type == 3);
                            var type5Count = batchPaperBoxes.Count(b => b.Type == 5);
                            var type0Count = batchPaperBoxes.Count(b => b.Type == 0);
                            var totalVolume = batchPaperBoxes.Sum(b =>
                                b.Type == 1 ? paper_box_1 :
                                b.Type == 3 ? paper_box_3 :
                                b.Type == 5 ? paper_box_5 :
                                (double)b.Reality_volume
                            );
                            Console.WriteLine($"国家 {country} 批次（{batchIndex}）：" +
                                    $"商品条数 {batch.Count}，" +
                                    $"纸箱数 {batchPaperBoxes.Count}（1号:{type1Count}, 3号:{type3Count}, 5号:{type5Count}, 大件:{type0Count}），" +
                                    $"总体积 {totalVolume:F0} cm³（约 {totalVolume / 1_000_000:F2} m³），" +
                                    $"40柜 {containerResult.Count40} 个，20柜 {containerResult.Count20} 个"
                            );
                        }
                        // 添加到总列表
                        allPaperBoxes.AddRange(batchPaperBoxes);
                        allDetails.AddRange(batchDetails);
                    }
                }
            }
            // 插入数据库（使用事务保证一致性）
            Console.WriteLine($"开始执行插入数据库，当前时间{DateTime.Now}");
            if (allPaperBoxes.Any())
            {
                try
                {
                    await _db.Ado.BeginTranAsync();
                    await _db.Insertable<Paperbox>(allPaperBoxes).ExecuteCommandAsync();
                    await _db.Insertable<Paperboxdetail>(allDetails).ExecuteCommandAsync();
                    await _db.Insertable<Transportbox>(allContainers).ExecuteCommandAsync();
                    await _db.Insertable<Supplier_record>(allrecords).ExecuteCommandAsync();
                    await _db.Updateable<Sale>().SetColumns(s => new Sale { Is_over = 1 })
                                .Where(s => saleIds.Contains(s.Id)).ExecuteCommandAsync();
                    await _db.Ado.CommitTranAsync();
                }
                catch (Exception ex)
                {
                    await _db.Ado.RollbackTranAsync();
                    throw new Exception("数据插入失败，已回滚", ex);
                }
            }
            var boxCount = allPaperBoxes.Count(b => b.Is_box == 0);
            var largeCount = allPaperBoxes.Count(b => b.Is_box == 1);
            Console.WriteLine($"插入数据库结束，当前时间{DateTime.Now}");
            Console.WriteLine($"装了 {boxCount} 个纸箱，{largeCount} 个大件商品");
            return new Paperbox
            {
                PaperboxdetailList = allDetails
            };
        }
        #region 辅助装箱装柜方法
        //装箱方法-----
        private (List<Paperbox>, List<Paperboxdetail>) PackItemsIntoCartons(List<SaleItemDto> items, string customerName, string country, DateTime?deliverTime)
        {
            var paperBoxes = new List<Paperbox>();
            var details = new List<Paperboxdetail>();
             var remaining = items.OrderByDescending(i => i.Volume).ToList(); //降序排序
            while (remaining.Any())
            {
                //计算总体积
                double totalVolume = remaining.Sum(i => i.Volume);
                // 根据总体积选择纸箱类型和容量
                int selectedType;
                double selectedCapacity;
                if (totalVolume <= paper_box_5)
                {
                    selectedType = 5;
                    selectedCapacity = paper_box_5;
                }
                else if (totalVolume <= paper_box_3)
                {
                    selectedType = 3;
                    selectedCapacity = paper_box_3;
                }
                else if (totalVolume <= paper_box_1)
                {
                    selectedType = 1;
                    selectedCapacity = paper_box_1;
                }
                else //总体积大于1号纸箱，优先使用1号纸箱
                {
                    selectedType = 1;
                    selectedCapacity = paper_box_1;
                }
                // 装入当前纸箱 大件优先
                var boxItems = new List<SaleItemDto>();
                double used = 0;
                for (int i = remaining.Count - 1; i >= 0; i--)
                {
                    var item = remaining[i];
                    if (used + item.Volume <= selectedCapacity)
                    {
                        used += item.Volume;
                        boxItems.Add(item);
                        remaining.RemoveAt(i);
                    }
                    else
                        continue; // 如果当前商品放不下，跳过该商品，装更小的商品
                }
                // 生成纸箱记录
                if (boxItems.Any())
                {
                    string boxNo = $"PB-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}";
                    var box = new Paperbox
                    {
                        Paper_box_no = boxNo,
                        Type = selectedType,
                        Reality_volume = (decimal)used,
                        Customer_name = customerName,
                        Country_name = country,
                        Deliver_time = deliverTime,
                        Create_time = DateTime.Now,
                        Is_box = 0
                    };
                    paperBoxes.Add(box);
                    foreach(var item in boxItems)
                    {
                        var detail = new Paperboxdetail
                        {
                            Paper_box_no = boxNo,
                            Volume = item.Volume,
                            Sale_no = item.SaleNo,
                            Barcode = item.Barcode,
                            Create_time = DateTime.Now
                        };
                        details.Add(detail);
                    }
                }
            }
            return (paperBoxes, details);
        }
        // 保存柜子记录和费用计算-----
        private async Task<(List<Transportbox> containers, List<Supplier_record> records)> SaveContainers(List<Paperbox> paperBoxes, LoadUnloadResult containerResult, string country, DateTime? batchDeliverTime)
        {
            var (portid,supplierId, price20, price40) = await GetBestSupplierPriceAsync(country);
            var containers = new List<Transportbox>();
            var records = new List<Supplier_record>();
            //40柜
            foreach (var containerItems in containerResult.FortyContainers)
            {
                var boxNos = containerItems.Select(i => i.Name).ToList();
                var relatedBoxes = paperBoxes.Where(b => boxNos.Contains(b.Paper_box_no)).ToList();
                var DeliverTime = relatedBoxes.Min(b => b.Deliver_time) ?? DateTime.Now;
                double usedVolume = containerItems.Sum(i => i.Volume);
                double totalVolume = transportBox_40; // 40柜总容积
                var container = new Transportbox
                {
                    Type = 4,
                    Deliver_time = batchDeliverTime ?? DateTime.Now.Date,   // 统一使用批次时间
                    All_volume = (decimal)totalVolume,
                    Reality_volume = (decimal)usedVolume,
                    Country_name = country,
                    Create_time = DateTime.Now,
                    Transport_box_no = $"TB-40-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                    Price_40 = price40,
                    Price_20 = price20,
                    Port_id = portid,
                    Supplier_id= supplierId,
                    Volume_radio = (decimal)(usedVolume / totalVolume)
                };
                containers.Add(container);
                // 更新纸箱柜号
                foreach (var box in paperBoxes.Where(b => boxNos.Contains(b.Paper_box_no)))
                {
                    box.Transport_box_no = container.Transport_box_no;
                }
                //计算费用
                var record = await CalculateFees(price40, usedVolume, totalVolume, container.Transport_box_no, supplierId, batchDeliverTime);
                records.Add(record);
            }
            //20柜
            foreach (var containerItems in containerResult.TwentyContainers)
            {
                var boxNos = containerItems.Select(i => i.Name).ToList();
                var relatedBoxes = paperBoxes.Where(b => boxNos.Contains(b.Paper_box_no)).ToList();
                var DeliverTime = relatedBoxes.Min(b => b.Deliver_time) ?? DateTime.Now;
                double usedVolume = containerItems.Sum(i => i.Volume);
                double totalVolume = transportBox_20; // 20柜总容积
                var container = new Transportbox
                {
                    Type = 2,
                    Deliver_time = batchDeliverTime ?? DateTime.Now.Date,   // 统一使用批次时间
                    All_volume = (decimal)totalVolume,
                    Reality_volume = (decimal)usedVolume,
                    Country_name = country,
                    Create_time = DateTime.Now,
                    Transport_box_no = $"TB-20-{DateTime.Now:yyyyMMddHHmmss}-{Guid.NewGuid().ToString("N").Substring(0, 6)}",
                    Price_40 = price40,
                    Price_20 = price20,
                    Port_id = portid,
                    Supplier_id = supplierId,
                    Volume_radio = (decimal)usedVolume / (decimal)totalVolume
                };
                containers.Add(container);
                // 更新纸箱柜号
                foreach (var box in paperBoxes.Where(b => boxNos.Contains(b.Paper_box_no)))
                {
                    box.Transport_box_no = container.Transport_box_no;
                }
                //计算费用
                var record = await CalculateFees(price40, usedVolume, totalVolume, container.Transport_box_no, supplierId, batchDeliverTime);
                records.Add(record);
            }
            return (containers, records);

        }
        // 辅助函数：根据柜型计算各项费用-----
        private async Task<Supplier_record> CalculateFees(decimal? boxPrice, double volumeUsed, double totalVolume, string transportBoxNo, int? supplierId, DateTime? deliverTime)
        {
            decimal? oceanCost = boxPrice / 0.9m;
            decimal? isf = oceanCost * 0.01m;
            decimal? insurance = oceanCost * 0.01m;
            decimal? portSurcharge = oceanCost * 0.03m;
            decimal? deliveryGoods = oceanCost * 0.05m;
            decimal? reviceGoods = oceanCost * 0.02m;
            decimal? trailer = oceanCost * 0.02m;
            decimal? clearance = oceanCost * 0.01m;
            decimal? allCost = oceanCost + isf + insurance + portSurcharge + deliveryGoods + reviceGoods + trailer + clearance;
            double volumeRadio = volumeUsed / totalVolume;
            decimal ratio;
            if (volumeRadio < 0.2)
                ratio = 0.2m;
            else if (volumeRadio < 0.3)
                ratio = 0.3m;
            else if (volumeRadio < 0.4)
                ratio = 0.4m;
            else if (volumeRadio >= 0.8)
                ratio = 1.0m;
            else // 40% ~ 80% 之间按实际比例
                ratio = (decimal)volumeRadio;

            decimal? realAllCost = allCost * ratio;
            return new Supplier_record
            {
                Supplier_id = supplierId,
                Transport_box_no = transportBoxNo,
                Ocean_cost = oceanCost,
                Isf = isf,
                Insurance = insurance,
                Port_surcharge = portSurcharge,
                Delivery_goods = deliveryGoods,
                Revice_goods = reviceGoods,
                Trailer = trailer,
                Clearance = clearance,
                Create_time = DateTime.Now,
                Deliver_time = deliverTime ?? DateTime.Now.Date,
                All_cost = allCost,
                Real_all_cost = realAllCost
            };
        }
        // 根据国家获取最优供应商价格 ------
        private async Task<(int? portId, int? supplierId, decimal? price20, decimal? price40)> GetBestSupplierPriceAsync(string countryName)
        {
            // 1. 查询国家ID
            var country = await _db.Queryable<Country>()
                .FirstAsync(c => c.Country_name == countryName);

            // 2. 查询该国家下所有港口ID（非空 int 列表）
            var portIds = await _db.Queryable<Port>()
                .Where(p => p.Country_id == country.Id)
                .Select(p => p.Id)
                .ToListAsync(); // List<int>，因为 Id 非空

            // 3. 查询这些港口的所有供应商报价
            //    使用 Where 过滤 Port_id 非空且包含在列表中
            var supplierPrices = await _db.Queryable<Supplier_port>()
                .Where(sp => sp.Port_id != null && portIds.Contains(sp.Port_id.Value))
                .Select(sp => new {sp.Port_id, sp.Supplier_id, sp.Price_20, sp.Price_40 })
                .ToListAsync();
            
            // 4. 按总价（price_20 + price_40）升序取第一个（最便宜）
            var best = supplierPrices
                .Select(x => new {x.Port_id, x.Supplier_id, x.Price_20, x.Price_40, total = x.Price_20 + x.Price_40 })
                .OrderBy(x => x.total)
                .FirstOrDefault();
            return (best.Port_id, best.Supplier_id, best.Price_20, best.Price_40);
        }
        #endregion


        //装柜列表 第二步骤用
        public async Task<object> GetTransport(DateTime? startTime, DateTime? endTime)
        {
           var list = await _db.Queryable<Transportbox,StreamModel.Supplier,Port>((t,s,p)=>t.Supplier_id==s.Id && t.Port_id==p.Id)
                .Where(t => t.Deliver_time >= startTime && t.Deliver_time <= endTime)
                .Select((t, s, p) => new { 
                    t.Country_name,
                    p.Port_name,
                    s.Supplier_name,
                    t.Type,
                    t.Price_20,
                    t.Price_40,
                    t.Deliver_time,
                    t.All_volume,
                    t.Reality_volume,
                    t.Volume_radio
                }).ToListAsync();
            return list;
        }
        //第三步：获取费用明细
        public async Task<object> GetCostDetails(DateTime startTime, DateTime endTime)
        {
            // 1. 按国家分组统计运输柜数据
            var stats = await _db.Queryable<Transportbox>()
                .Where(t => t.Deliver_time >= startTime && t.Deliver_time <= endTime)
                .GroupBy(t => t.Country_name)
                .Select(t => new
                {
                    CountryName = t.Country_name,
                    Count40 = SqlFunc.AggregateSum(SqlFunc.IIF(t.Type == 4, 1, 0)),
                    Count20 = SqlFunc.AggregateSum(SqlFunc.IIF(t.Type == 2, 1, 0)),
                    SumPrice40 = SqlFunc.AggregateSum(SqlFunc.IIF(t.Type == 4, t.Price_40, 0)),
                    SumPrice20 = SqlFunc.AggregateSum(SqlFunc.IIF(t.Type == 2, t.Price_20, 0)),
                    Price40 = SqlFunc.AggregateAvg(t.Price_40),
                    Price20 = SqlFunc.AggregateAvg(t.Price_20)
                })
                .ToListAsync();

            // 2. 计算各项费用
            var result = new List<CountryCostDetailDto>();
            foreach (var s in stats)
            {
                // 箱子总费用（40柜+20柜）
                decimal totalBoxPrice = (s.SumPrice40 ?? 0) + (s.SumPrice20 ?? 0);
                // 海运费 = 箱子总费用 / 0.9
                decimal oceanCost = totalBoxPrice / 0.9m;
                // 各项费用按海运费比例计算（比例可根据业务调整）
                decimal isf = oceanCost * 0.01m;            // ISF 1%
                decimal insurance = oceanCost * 0.01m;       // 保险 1%
                decimal portSurcharge = oceanCost * 0.03m;   // 港杂 3%
                decimal deliveryGoods = oceanCost * 0.05m;   // 提货 5%
                decimal reviceGoods = oceanCost * 0.02m;     // 收货+装柜 2%
                decimal trailer = oceanCost * 0.02m;         // 拖车 2%
                decimal clearance = oceanCost * 0.01m;       // 报关 1%
                // 合计费用 = 各项费用之和
                decimal totalCost = totalBoxPrice + oceanCost + isf + insurance + portSurcharge + deliveryGoods + reviceGoods + trailer + clearance;

                result.Add(new CountryCostDetailDto
                {
                    CountryName = s.CountryName,
                    Count40 = s.Count40,
                    Count20 = s.Count20,
                    Price40 = s.Price40,          // 40柜价格
                    Price20 = s.Price20,          // 20柜价格
                    OceanCost = oceanCost,
                    ISF = isf,
                    Insurance = insurance,
                    PortSurcharge = portSurcharge,
                    DeliveryGoods = deliveryGoods,
                    ReviceGoods = reviceGoods,
                    Trailer = trailer,
                    Clearance = clearance,
                    TotalCost = totalCost
                });
            }
            return result;
        }




        //获取海运装柜列表
        public async Task<List<ContainerListDTO>> GetContainerList(DateTime? startTime, DateTime? endTime, int? supplierId, string?countryName)
        {
            var query = await _db.Queryable<Supplier_record, StreamModel.Supplier, Transportbox>(
                (sr, s, tb) => sr.Supplier_id == s.Id && sr.Transport_box_no == tb.Transport_box_no
            )
            .WhereIF(startTime.HasValue, (sr, s, tb) => tb.Deliver_time >= startTime)
            .WhereIF(endTime.HasValue, (sr, s, tb) => tb.Deliver_time <= endTime)
            .WhereIF(supplierId.HasValue, (sr, s, tb) => sr.Supplier_id == supplierId)
            .WhereIF(!string.IsNullOrEmpty(countryName), (sr, s, tb) => tb.Country_name == countryName)
            .OrderBy((sr, s, tb) => tb.Deliver_time)
            .Select((sr, s, tb) => new ContainerListDTO
            {
                SupplierName = s.Supplier_name,
                CountryName = tb.Country_name,
                DeliverTime = tb.Deliver_time,
                Type = tb.Type,
                TypeDisplay = SqlFunc.IIF(tb.Type == 4, "40'柜", "20'柜"),
                AllVolume = tb.All_volume,
                RealityVolume = tb.Reality_volume,
                VolumeRadio = tb.Volume_radio,
                Transport_box_no = tb.Transport_box_no,
                ContainerPrice = SqlFunc.IIF(tb.Type == 4, tb.Price_40, tb.Price_20),
                OceanCost = sr.Ocean_cost,
                Isf = sr.Isf,
                Insurance = sr.Insurance,
                PortSurcharge = sr.Port_surcharge,
                DeliveryGoods = sr.Delivery_goods,
                ReviceGoods = sr.Revice_goods,
                Trailer = sr.Trailer,
                Clearance = sr.Clearance,
                AllCost = sr.All_cost,
                RealAllCost = sr.Real_all_cost,
                TransportBoxNo = tb.Transport_box_no
            }).ToListAsync();
            return query;
        }
        //获取柜子所有纸箱详情
        public async Task<List<BoxDetailDto>> GetBoxDetails(string transportBoxNo)
        {
            var result = new List<BoxDetailDto>();
            // 获取该柜子下所有的纸箱（包括纸箱和大件商品）
            var paperBoxes = await _db.Queryable<Paperbox>()
                .Where(p => p.Transport_box_no == transportBoxNo)
                .ToListAsync();
            // 处理纸箱（is_box == 0）
            var cartons = paperBoxes.Where(p => p.Is_box == 0);
            foreach (var pb in cartons)
            {
                double volume = pb.Type switch
                {
                    1 => paper_box_1,
                    3 => paper_box_3,
                    5 => paper_box_5,
                    _ => 0 // 或其他默认值
                };
                string boxTypeName = pb.Type switch
                {
                    1 => "1号纸箱",
                    3 => "3号纸箱",
                    5 => "5号纸箱",
                };
                string goodsDisplay = $"{boxTypeName}（{pb.Paper_box_no}）";
                result.Add(new BoxDetailDto
                {
                    PaperBoxNo = pb.Paper_box_no,
                    OutstoreTime = pb.Deliver_time,
                    CountryName = pb.Country_name,
                    GoodsName = goodsDisplay,
                    Quantity = 1,
                    Volume = SqlFunc.ToDecimal(volume),
                    CustomerName = pb.Customer_name,
                    is_box = 1  
                });
            }

            // 2. 处理大件商品（is_box == 1）
            var largeItems = paperBoxes.Where(p => p.Is_box == 1).ToList();
            var grouped = largeItems.GroupBy(p => p.Barcode);
            foreach (var group in grouped)
            {
                var first = group.First();
                var goods = await _db.Queryable<Goods>().FirstAsync(g => g.Barcode == first.Barcode);
                string goodsName = goods?.Goods_name ?? "未知商品";
                decimal? volume = SqlFunc.ToDecimal(goods?.Volume);

                result.Add(new BoxDetailDto
                {
                    PaperBoxNo = first.Paper_box_no, // 取组内第一个的编号（如果需要展示一个代表）
                    OutstoreTime = first.Deliver_time,
                    CountryName = first.Country_name,
                    GoodsName = goodsName,
                    Quantity = group.Count(),        // 该柜子下该商品的总数
                    Volume = volume,
                    CustomerName = first.Customer_name,
                    is_box = 0  // 大件商品标记为 0（根据你的字段含义）
                });
            }

            return result;
        }
        //获取纸箱所有商品详情
        public async Task<object> GetPaperBoxDetails(string paperBoxNo)
        {
            var detail = await _db.Queryable<Paperboxdetail, Goods>((p, g) => p.Barcode == g.Barcode)
                .Where(p => p.Paper_box_no == paperBoxNo)
                .GroupBy((p, g) => new { p.Barcode, p.Sale_no, g.Goods_name })
                .Select((p, g) => new
                {
                    Sale_no = p.Sale_no,
                    Barcode = p.Barcode,
                    GoodName = g.Goods_name,
                    Number = SqlFunc.AggregateCount(p.Barcode)    //箱子中该商品的数量
                }).ToListAsync();
            return detail;
        }
        //明细导出
        public async Task<byte[]> ExportContainerListAsync(DateTime? startTime, DateTime? endTime, int? supplierId, string? countryName)
        {
            // 1. 获取柜子主列表
            var containers = await GetContainerList(startTime, endTime, supplierId, countryName);
            if (!containers.Any()) return Array.Empty<byte>();

            // 2. 获取所有柜子下的纸箱及商品明细（包括纸箱和独立商品）
            var transportBoxNos = containers.Select(c => c.TransportBoxNo).Distinct().ToList();
            var paperBoxes = await _db.Queryable<Paperbox>()
                .Where(p => transportBoxNos.Contains(p.Transport_box_no))
                .Select(p => new {
                    p.Transport_box_no,
                    p.Paper_box_no,
                    p.Is_box,
                    p.Sale_no,
                    p.Barcode
                })
                .ToListAsync();

            // 建立纸箱编号到柜号的映射（所有记录都包含）
            var paperBoxToContainer = paperBoxes
                .GroupBy(p => p.Paper_box_no)
                .ToDictionary(g => g.Key, g => g.First().Transport_box_no);

            // 分别处理纸箱（Is_box == 0）和独立商品（Is_box == 1）
            var paperBoxNos = paperBoxes.Where(p => p.Is_box == 0).Select(p => p.Paper_box_no).Distinct().ToList();
            var directItems = paperBoxes.Where(p => p.Is_box == 1).Select(p => new {
                p.Transport_box_no,
                p.Sale_no,
                p.Barcode,
                Quantity = 1
            }).ToList();

            // 查询纸箱明细（关联 Paperboxdetail 和 Goods）
            var detailsQuery = new List<dynamic>();
            if (paperBoxNos.Any())
            {
                var queryResult = await _db.Queryable<Paperboxdetail, Goods>((d, g) => d.Barcode == g.Barcode)
                    .Where(d => paperBoxNos.Contains(d.Paper_box_no))
                    .Select((d, g) => new
                    {
                        d.Paper_box_no,
                        d.Sale_no,
                        d.Barcode,
                        GoodsName = g.Goods_name,
                        Quantity = 1 // 若有实际数量字段请替换
                    }).ToListAsync();
                detailsQuery = queryResult.Cast<dynamic>().ToList();
            }

            // 将纸箱明细映射到柜子
            var containerDetailsFromBoxes = detailsQuery
                .GroupBy(d => paperBoxToContainer[d.Paper_box_no])
                .ToDictionary(g => g.Key, g => g.ToList());

            // 处理独立商品：获取 GoodsName
            var containerDetailsFromDirect = new Dictionary<string, List<dynamic>>();
            if (directItems.Any())
            {
                var barcodes = directItems.Select(i => i.Barcode).Distinct().ToList();
                var goodsDict = await _db.Queryable<Goods>()
                    .Where(g => barcodes.Contains(g.Barcode))
                    .Select(g => new { g.Barcode, g.Goods_name })
                    .ToDictionaryAsync(g => g.Barcode, g => g.Goods_name);

                // 按柜子分组，填充商品名称
                var directGrouped = directItems
                    .GroupBy(i => i.Transport_box_no)
                    .Select(g => new
                    {
                        TransportBoxNo = g.Key,
                        Items = g.Select(i => new
                        {
                            i.Sale_no,
                            i.Barcode,
                            GoodsName = goodsDict.TryGetValue(i.Barcode, out var name) ? name : "未知商品",
                            i.Quantity
                        }).ToList()
                    })
                    .ToDictionary(g => g.TransportBoxNo, g => g.Items.Cast<dynamic>().ToList());
                containerDetailsFromDirect = directGrouped;
            }

            // 合并两个来源的明细到 containerDetails（优先纸箱明细，再添加独立商品）
            var containerDetails = new Dictionary<string, List<dynamic>>();
            foreach (var kv in containerDetailsFromBoxes)
                containerDetails[kv.Key] = kv.Value.ToList();

            foreach (var kv in containerDetailsFromDirect)
            {
                if (containerDetails.ContainsKey(kv.Key))
                    containerDetails[kv.Key].AddRange(kv.Value);
                else
                    containerDetails[kv.Key] = kv.Value.ToList();
            }

            // 3. 创建 Excel 文件
            using var package = new ExcelPackage();
            var worksheet = package.Workbook.Worksheets.Add("装柜列表");

            // 定义全局表头
            int col = 1;
            worksheet.Cells[1, col++].Value = "供应商";
            worksheet.Cells[1, col++].Value = "目的国";
            worksheet.Cells[1, col++].Value = "发往时间";
            worksheet.Cells[1, col++].Value = "柜型";
            worksheet.Cells[1, col++].Value = "总容积";
            worksheet.Cells[1, col++].Value = "实际容积";
            worksheet.Cells[1, col++].Value = "容积率";
            worksheet.Cells[1, col++].Value = "集装箱价格";
            worksheet.Cells[1, col++].Value = "海运费";
            worksheet.Cells[1, col++].Value = "港杂费";
            worksheet.Cells[1, col++].Value = "ISF";
            worksheet.Cells[1, col++].Value = "保险费";
            worksheet.Cells[1, col++].Value = "提货费";
            worksheet.Cells[1, col++].Value = "收货费";
            worksheet.Cells[1, col++].Value = "拖车费";
            worksheet.Cells[1, col++].Value = "报关费";
            worksheet.Cells[1, col++].Value = "总费用";
            worksheet.Cells[1, col++].Value = "实际费用";
            worksheet.Cells[1, col++].Value = "整柜/散柜";

            int row = 2;

            foreach (var container in containers)
            {
                int mainRow = row;
                col = 1;
                worksheet.Cells[mainRow, col++].Value = container.SupplierName;
                worksheet.Cells[mainRow, col++].Value = container.CountryName;
                worksheet.Cells[mainRow, col++].Value = container.DeliverTime?.ToString("yyyy-MM-dd") ?? "";
                worksheet.Cells[mainRow, col++].Value = container.TypeDisplay;
                worksheet.Cells[mainRow, col++].Value = container.AllVolume;
                worksheet.Cells[mainRow, col++].Value = container.RealityVolume;
                worksheet.Cells[mainRow, col++].Value = container.VolumeRadio.HasValue ? (container.VolumeRadio.Value * 100).ToString("F2") + "%" : "";
                worksheet.Cells[mainRow, col++].Value = container.ContainerPrice;
                worksheet.Cells[mainRow, col++].Value = container.OceanCost;
                worksheet.Cells[mainRow, col++].Value = container.PortSurcharge;
                worksheet.Cells[mainRow, col++].Value = container.Isf;
                worksheet.Cells[mainRow, col++].Value = container.Insurance;
                worksheet.Cells[mainRow, col++].Value = container.DeliveryGoods;
                worksheet.Cells[mainRow, col++].Value = container.ReviceGoods;
                worksheet.Cells[mainRow, col++].Value = container.Trailer;
                worksheet.Cells[mainRow, col++].Value = container.Clearance;
                worksheet.Cells[mainRow, col++].Value = container.AllCost;
                worksheet.Cells[mainRow, col++].Value = container.RealAllCost;
                // 第19列：整柜/散柜（根据容积率判断）
                bool isMixed = container.VolumeRadio.HasValue && SqlFunc.ToDouble(container.VolumeRadio.Value) < 0.8;
                worksheet.Cells[mainRow, col++].Value = isMixed ? "散柜" : "整柜";
                // 写入“装柜详情”及明细列标题（第20~24列）
                worksheet.Cells[row, 20].Value = "装柜详情";
                worksheet.Cells[row, 21].Value = "销售单号";
                worksheet.Cells[row, 22].Value = "商品条码";
                worksheet.Cells[row, 23].Value = "SKU名称";
                worksheet.Cells[row, 24].Value = "数量";
                // 写入明细数据
                if (containerDetails.TryGetValue(container.TransportBoxNo, out var items))
                {
                    // 按 Sale_no + Barcode 分组聚合（数量相加）
                    var groupedItems = items
                        .GroupBy(i => new { i.Sale_no, i.Barcode })
                        .Select(g => new
                        {
                            g.Key.Sale_no,
                            g.Key.Barcode,
                            GoodsName = g.First().GoodsName,
                            Quantity = g.Sum(i => i.Quantity)
                        })
                        .ToList();
                    int dataRow = row + 1;
                    foreach (var item in groupedItems)
                    {
                        worksheet.Cells[dataRow, 21].Value = item.Sale_no;
                        worksheet.Cells[dataRow, 22].Value = item.Barcode;
                        worksheet.Cells[dataRow, 23].Value = item.GoodsName;
                        worksheet.Cells[dataRow, 24].Value = item.Quantity;
                        dataRow++;
                    }
                    row = dataRow;
                }
                else
                {
                    // 无明细，让下一个柜子从标题行下一行开始
                    row += 1;
                }
            }

            // 自动调整列宽（可选）
            // worksheet.Cells[worksheet.Dimension.Address].AutoFitColumns();
            return package.GetAsByteArray();
        }
    }
}
