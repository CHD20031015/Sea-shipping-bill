using Microsoft.Identity.Client;
using SqlSugar;
using SqlSugar.Extensions;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.Method;

namespace StreamCore.Service
{
    public class ProcureService
    {
        private readonly ISqlSugarClient _db;
        public ProcureService([FromKeyedServices("stream")] ISqlSugarClient db)
        {
            _db = db;
        }

        // pocure表分页查询
        public async Task<object> getallProcure(DateTime?startTime, DateTime?endTime,int page, int pageSize)
        {
            try
            {
                var query = _db.Queryable<Procure>();
                //时间查询
                if (startTime.HasValue && endTime.HasValue)
                {
                    query = query.Where(p => p.Create_time >= startTime.Value && p.Create_time <= endTime.Value);
                }
                else 
                {
                    if (startTime.HasValue)
                    {
                        query = query.Where(p => p.Create_time >= startTime.Value);
                    }
                    if (endTime.HasValue)
                    {
                        query = query.Where(p => p.Create_time <= endTime.Value);
                    }
                }
                // 分页查询
                var totalCount = await query.CountAsync();
                var procure = await _db.Queryable<Procure>()
                                      .OrderBy(p => p.Id)  // 按ID排序
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();
                return new
                {
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Data = procure
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcureService 错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        //sale表分页查询
        public async Task<object> getallSale(DateTime? startTime, DateTime? endTime, int page, int pageSize)
        {
            try
            {
                var query = _db.Queryable<Sale>();
                //时间查询
                if (startTime.HasValue && endTime.HasValue)
                {
                    query = query.Where(s => s.Create_time >= startTime.Value && s.Create_time <= endTime.Value);
                }
                else
                {
                    if (startTime.HasValue)
                    {
                        query = query.Where(s => s.Create_time >= startTime.Value);
                    }
                    if (endTime.HasValue)
                    {
                        query = query.Where(s => s.Create_time <= endTime.Value);
                    }
                }
                // 分页查询
                var totalCount = await query.CountAsync();
                var sale = await _db.Queryable<Sale>()
                                      .OrderBy(p => p.Id)  // 按ID排序
                                      .Skip((page - 1) * pageSize)
                                      .Take(pageSize)
                                      .ToListAsync();
                return new
                {
                    Total = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    Data = sale
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ProcureService 错误: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                throw;
            }
        }
        //计算当月仓租费
        public async Task<Cost> Warehouserent(DateTime startTime,DateTime endTime)
        {
            const double PalletVolume = 0.85;     // 每板标准体积 m³
            const double DailyRate = 8.0;         // 每板每天费用 HKD
            //----------------------------------------------------------PO采购费用计算----------------------------------------------
            // 1. 查询当月每日采购总体积（按日期分组）
            var dailyVolumes = await _db.Queryable<Procure, Goods>((p, g) => p.Barcode == g.Barcode)
                .Where((p, g) => p.Instore_time >= startTime && p.Instore_time <= endTime)
                .GroupBy((p, g) => p.Instore_time.Value.Date)
                .Select((p, g) => new {
                    Date = p.Instore_time.Value.Date,
                    totalVolume = SqlFunc.AggregateSum(
                        // 安全转换字符串为 double（空值视为0）
                        SqlFunc.ToDouble(SqlFunc.IsNull(p.Number, "0")) *
                        // SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume_m, "0"))
                        (SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0")) / 1000000.0)
                        )
                }).MergeTable().OrderBy(x => x.Date).ToListAsync();
            // 2. 顺序累积成板
            double poAccumulated = 0;          // PO 当前累积体积
            int poTotalPallets = 0;            // PO 完整板数
            double poTotalFee = 0.0;           // PO 总费用
            double poTotalVolume = dailyVolumes.Sum(d => d.totalVolume); // PO 总体积
            Console.WriteLine($"PO总体积：{poTotalVolume}");
            DateTime? poBatchStart = null;     // 记录当前不足板批次的起始日期（用于月底计费）
            foreach (var day in dailyVolumes.OrderBy(d => d.Date))
            {
                if (day.totalVolume <= 0) continue;
                poAccumulated += day.totalVolume;
                //成板
                while (poAccumulated >= PalletVolume)
                {
                    // 成板日 = 当天
                    DateTime formedDate = day.Date;
                    // 计费天数 = 从成板日到月末（含成板日）
                    int days = (endTime - formedDate).Days + 1;
                    if (days < 0) days = 0;
                    // 增加费用：1板 × 天数 × 日费率
                    poTotalFee += days * DailyRate;
                    poTotalPallets++;
                    poAccumulated -= PalletVolume;
                    // 若还有剩余体积，重置批次起始日期为当前日期（剩余部分从今天开始累积）
                    if (poAccumulated > 0)
                        poBatchStart = day.Date;
                    else
                        poBatchStart = null;
                }
                // 若累积后仍不足一板，保留至下一天继续累积
            }
            // 3. 月底剩余不足板按照1板计算
            if (poAccumulated > 0 && poBatchStart.HasValue)
            {
                // 计费天数：从批次起始日到月末（含起始日）
                int days = (endTime - poBatchStart.Value).Days + 1;
                if (days < 0) days = 0;
                poTotalFee += days * DailyRate; // 增加一板费用
                poTotalPallets++;               // 板数 +1
            }
            //===============================================================SO出库费用计算========================================
            // 查询当月 SO 商品体积，按出库日期分组（优先 Outstore_time，否则用 Create_time）
            var dailySoVolumes = await _db.Queryable<Sale, Goods>((s, g) => s.Barcode == g.Barcode)
                .Where((s, g) => s.Outstore_time >= startTime &&s.Outstore_time <= endTime)
                .GroupBy((s, g) => s.Outstore_time .Value.Date)
                .Select((s, g) => new
                {
                    Date = s.Outstore_time.Value.Date,
                    TotalVolume = SqlFunc.AggregateSum(
                        SqlFunc.ToDouble(SqlFunc.IsNull(s.Number, "0")) *
                        //SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume_m, "0"))
                         (SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0")) / 1000000.0)
                    )
                }).MergeTable().OrderBy(x => x.Date).ToListAsync();
            double soAccumulated = 0;          // SO 当前累积体积
            double soDeduction = 0.0;          // 需扣除的总费用
            double soTotalVolume = dailySoVolumes.Sum(d => d.TotalVolume); // SO 总体积
            // 同样采用顺延成板逻辑，每形成一个完整板，扣除从成板日到月末的费用（不含成板日当天）
            foreach (var day in dailySoVolumes.OrderBy(d => d.Date))
            {
                if (day.TotalVolume <= 0) continue;
                soAccumulated += day.TotalVolume;
                while (soAccumulated >= PalletVolume)
                {
                    DateTime formedDate = day.Date; // 成板日
                    // 扣除天数：从成板日的下一天到月末
                    int daysToDeduct = (endTime - formedDate).Days; // 不含成板日当天
                    if (daysToDeduct < 0) daysToDeduct = 0;
                    soDeduction += daysToDeduct * DailyRate; // 1板 × 多算天数 × 日费率
                    soAccumulated -= PalletVolume;
                }
            }
            //===============================================================查询历史费用计算====================================
            // 所有时间总PO体积（到当月为止）
            double allPoVolume = await _db.Queryable<Procure, Goods>((p, g) => p.Barcode == g.Barcode)
                                .Where((p, g) => p.Instore_time <= endTime)
                                .SumAsync((p, g) => SqlFunc.ToDouble(SqlFunc.IsNull(p.Number, "0")) *
                                        (SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0")) / 1000000.0));
            //所有时间SO体积
            double allSoVolume = await _db.Queryable<Sale, Goods>((s, g) => s.Barcode == g.Barcode)
                                 .Where((s, g) => s.Outstore_time <= endTime)
                                 .SumAsync((s, g) => SqlFunc.ToDouble(SqlFunc.IsNull(s.Number, "0")) *
                                                   (SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0")) / 1000000.0));
            double historyPoVolume = allPoVolume - poTotalVolume; // 历史PO体积
            double historySoVolume = allSoVolume - soTotalVolume; // 历史SO体积
            double historyVolume = historyPoVolume - historySoVolume;
            if (historyVolume < 0) historyVolume = 0;
            int historyPallets = (int)Math.Ceiling(historyVolume / PalletVolume); // 向上取整
            int daysInMonth = (endTime - startTime).Days + 1;
            double historyFee = historyPallets * daysInMonth * DailyRate;
            poTotalPallets += historyPallets;
            // ==========================  最终费用 ==========================
            double finalFee = poTotalFee + historyFee - soDeduction;
            if (finalFee < 0) finalFee = 0; // 费用不能为负
            // 组装返回结果
            var cost = new Cost
            {
                PO_volume_m = poTotalVolume.ToString("F2"), // 返回PO总体积
                SO_volume_m = soTotalVolume.ToString("F2"), // 返回SO总体积
                Number = poTotalPallets.ToString(),         // PO 完整板数
                All_cost = finalFee.ToString("F2")
            };
            return cost;
        }
        # region 计算PO装箱费
        public async Task<LoadUnloadResult> PoUploadunload(DateTime startTime, DateTime endTime)
        {
            // 查询PO订单：按日期分组，获取每个商品的单个体积（m³）和数量
            var items = await _db.Queryable<Procure, Goods>((p, g) => p.Barcode == g.Barcode)
                .Where((p, g) => p.Instore_time >= startTime && p.Instore_time <= endTime)
                .Select((p, g) => new
                {
                    Date = p.Instore_time.Value.Date,
                    Number = SqlFunc.ToDouble(SqlFunc.IsNull(p.Number, "0")),          // 数量（double）
                    VolumePerUnit = SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0")) // 单个体积（cm³）
                }).MergeTable().ToListAsync();

            if (!items.Any())
                return new LoadUnloadResult();
            // 按日期分组
            var grouped = items.GroupBy(x => x.Date);
            int total40 = 0, total20 = 0, totalLoose = 0;
            decimal totalFee40 = 0, totalFee20 = 0, totalFeeLoose = 0, totalFee = 0;

            foreach (var group in grouped)
            {
                // 拆分每个商品为多个单位体积
                var unitVolumes = new List<double>();
                foreach (var item in group)
                {
                    int count = (int)item.Number; // 数量取整
                    for (int i = 0; i < count; i++)
                    {
                        unitVolumes.Add(item.VolumePerUnit);
                    }
                }
                // 调用算法（单位体积列表降序排序在算法内部）
                var (cnt40, cnt20, cntLoose, fee40, fee20, feeLoose, fee) = LoadAndUnloadBox.CalculateDaily(unitVolumes);
                total40 += cnt40;
                total20 += cnt20;
                totalLoose += cntLoose;
                totalFee40 += fee40;
                totalFee20 += fee20;
                totalFeeLoose += feeLoose;
                totalFee += fee;
            }

            return new LoadUnloadResult
            {
                Count40 = total40,
                Count20 = total20,
                CountLoose = totalLoose,
                Fee40 = totalFee40,
                Fee20 = totalFee20,
                FeeLoose = totalFeeLoose,
                TotalFee = totalFee
            };
        }
        #endregion

        #region 计算 SO 出库装箱费（按出库日期分组）
        public async Task<LoadUnloadResult> SoUploadunload(DateTime startTime, DateTime endTime)
        {
            // 查询 SO 订单：按出库日期分组，获取每个商品的单个体积（m³）和数量
            var items = await _db.Queryable<Sale, Goods>((s, g) => s.Barcode == g.Barcode)
                .Where((s, g) => s.Outstore_time >= startTime && s.Outstore_time <= endTime)
                .Select((s, g) => new
                {
                    Date = s.Outstore_time.Value.Date,
                    Number = SqlFunc.ToDouble(SqlFunc.IsNull(s.Number, "0")),          // 数量（double）
                    VolumePerUnit = SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0")) // 单个体积（cm³）
                })
                .MergeTable()
                .ToListAsync();

            if (!items.Any())
                return new LoadUnloadResult();

            // 按日期分组
            var grouped = items.GroupBy(x => x.Date);
            int total40 = 0, total20 = 0, totalLoose = 0;
            decimal totalFee40 = 0, totalFee20 = 0, totalFeeLoose = 0, totalFee = 0;

            foreach (var group in grouped)
            {
                // 拆分每个商品为多个单位体积
                var unitVolumes = new List<double>();
                foreach (var item in group)
                {
                    int count = (int)item.Number; // 数量取整（若为小数，可考虑四舍五入，此处简单截断）
                    for (int i = 0; i < count; i++)
                    {
                        unitVolumes.Add(item.VolumePerUnit);
                    }
                }

                // 调用算法
                var (cnt40, cnt20, cntLoose, fee40, fee20, feeLoose, fee) = LoadAndUnloadBox.CalculateDaily(unitVolumes);
                total40 += cnt40;
                total20 += cnt20;
                totalLoose += cntLoose;
                totalFee40 += fee40;
                totalFee20 += fee20;
                totalFeeLoose += feeLoose;
                totalFee += fee;
            }

            return new LoadUnloadResult
            {
                Count40 = total40,
                Count20 = total20,
                CountLoose = totalLoose,
                Fee40 = totalFee40,
                Fee20 = totalFee20,
                FeeLoose = totalFeeLoose,
                TotalFee = totalFee
            };
        }
        #endregion
        //计算高价值打板费
        public async Task<HighCardResult> Highpallet(DateTime startTime, DateTime endTime)
        {
            //查询PO单，根据订单进行分类，选取订单中的单价大于1500的商品总体积
            var allVolumes = await _db.Queryable<Procure,Goods>((p,g)=>p.Barcode==g.Barcode)
                             .Where((p,g)=>p.Instore_time>=startTime && p.Instore_time<=endTime && p.Price>1500)
                             .GroupBy((p,g)=>p.Procure_no)
                             .Select((p, g) => new {
                                 No = p.Procure_no,
                                 totalVolume = SqlFunc.AggregateSum(SqlFunc.ToDecimal(SqlFunc.IsNull(p.Number, "0")) *
                                 (SqlFunc.ToDecimal(SqlFunc.IsNull(g.Volume, "0")) / 1000000m))
                             }).MergeTable().OrderBy(x => x.No).ToListAsync();
            if (!allVolumes.Any())
                return new HighCardResult();
            int highPallet = 0;
            decimal highFee = 0;
            foreach (var highVolume in allVolumes)
            {
                int number = (int)Math.Floor(highVolume.totalVolume);
                highPallet += number;
                decimal fee = number * 100;
                highFee += fee;
            }
            return new HighCardResult
            {
                Number = highPallet,
                All_cost = highFee
            };

        }
    }
}