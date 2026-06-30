using Microsoft.Identity.Client;
using SqlSugar;
using SqlSugar.Extensions;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.Method;
using StreamCore.StreamModel;

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
                    totalVolume = SqlFunc.AggregateSum(SqlFunc.ToDouble(p.Number) *
                    (SqlFunc.ToDouble(g.Volume) / 1000000.0))
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
                    // 若还有剩余体积，记录剩余部分从今天开始
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
                // 计费天数：从未成板日到月末（含起始日）
                int days = (endTime - poBatchStart.Value).Days + 1;
                if (days < 0) days = 0;
                poTotalFee += days * DailyRate; // 增加一板费用
                poTotalPallets++;               // 板数 +1
            }
            //===============================================================SO出库费用计算========================================
            // 查询当月 SO 商品体积，按出库日期分组
            var dailySoVolumes = await _db.Queryable<Sale, Goods>((s, g) => s.Barcode == g.Barcode)
                .Where((s, g) => s.Outstore_time >= startTime &&s.Outstore_time <= endTime)
                .GroupBy((s, g) => s.Outstore_time .Value.Date)
                .Select((s, g) => new
                {
                    Date = s.Outstore_time.Value.Date,
                    TotalVolume = SqlFunc.AggregateSum(
                        SqlFunc.ToDouble(s.Number) *(SqlFunc.ToDouble(g.Volume) / 1000000.0)
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
        #region 计算PO装箱费
        public async Task<LoadUnloadResult> PoUploadunload(DateTime startTime, DateTime endTime)
        {
            // 查询PO订单：获取每个商品的单个体积（m³）和数量
            var items = await _db.Queryable<Procure, Goods>((p, g) => p.Barcode == g.Barcode)
                .Where((p, g) => p.Instore_time >= startTime && p.Instore_time <= endTime)
                .Select((p, g) => new
                {
                    Date = p.Instore_time.Value.Date,
                    Name = p.Goods_name,                       // 商品名字
                    Number = SqlFunc.ToDouble(p.Number),       // 数量
                    VolumePerUnit = SqlFunc.ToDouble(g.Volume) // 单个体积（cm³）
                }).MergeTable().ToListAsync();

            if (!items.Any())
                return new LoadUnloadResult();
            // 按日期分组排序
            var grouped = items.GroupBy(x => x.Date);
            int total40 = 0, total20 = 0, totalLoose = 0;
            decimal totalFee40 = 0, totalFee20 = 0, totalFeeLoose = 0, totalFee = 0;
            var FortyContainers = new List<List<Item>>();
            var TwentyContainers = new List<List<Item>>();

            foreach (var group in grouped)
            {
                var sortedItems = group.OrderByDescending(x => x.VolumePerUnit).ToList();
                // 拆分为每个商品多个单位体积
                var everygood = new Queue<Item>();
                foreach (var item in sortedItems)
                {
                    int count = (int)item.Number; // 数量取整
                    for (int i = 0; i < count; i++)
                    {
                        everygood.Enqueue(new Item { Name = item.Name, Volume = item.VolumePerUnit });

                    }
                }
                // 调用算法（单位体积列表降序排序在算法内部）
                var result = LoadAndUnloadBox.CalculateDaily(everygood);
                total40 += result.Count40;
                total20 += result.Count20;
                totalLoose += result.CountLoose;
                totalFee40 += result.Fee40;
                totalFee20 += result.Fee20;
                totalFeeLoose += result.FeeLoose;
                totalFee += result.TotalFee;
                FortyContainers.AddRange(result.FortyContainers);
                TwentyContainers.AddRange(result.TwentyContainers);
            }
           
            return new LoadUnloadResult
            {
                Count40 = total40,
                Count20 = total20,
                CountLoose = totalLoose,
                Fee40 = totalFee40,
                Fee20 = totalFee20,
                FeeLoose = totalFeeLoose,
                TotalFee = totalFee,
                FortyContainers = FortyContainers,
                TwentyContainers = TwentyContainers
            };
        }
        #endregion

        #region 计算 SO 出库装箱费（按出库日期分组）
        public async Task<LoadUnloadResult> SoUploadunload(DateTime startTime, DateTime endTime)
        {
            // 查询 SO 订单：按出库日期分组，获取每个商品的单个体积（cm³）和数量
            var items = await _db.Queryable<Sale, Goods>((s, g) => s.Barcode == g.Barcode)
                .Where((s, g) => s.Outstore_time >= startTime && s.Outstore_time <= endTime)
                .Select((s, g) => new
                {
                    Date = s.Outstore_time.Value.Date,
                    Name = s.Goods_name,
                    Number = SqlFunc.ToDouble(SqlFunc.IsNull(s.Number, "0")),
                    VolumePerUnit = SqlFunc.ToDouble(SqlFunc.IsNull(g.Volume, "0"))
                })
                .MergeTable()
                .ToListAsync();

            if (!items.Any())
                return new LoadUnloadResult();

            // 按日期分组
            var grouped = items.GroupBy(x => x.Date);
            int total40 = 0, total20 = 0, totalLoose = 0;
            decimal totalFee40 = 0, totalFee20 = 0, totalFeeLoose = 0, totalFee = 0;
            var FortyContainers = new List<List<Item>>();
            var TwentyContainers = new List<List<Item>>();

            foreach (var group in grouped)
            {
                // 按体积降序排序
                var sortedItems = group.OrderByDescending(x => x.VolumePerUnit).ToList();
                var everygood = new Queue<Item>();
                foreach (var item in sortedItems)
                {
                    int count = (int)item.Number; // 数量取整
                    for (int i = 0; i < count; i++)
                    {
                        everygood.Enqueue(new Item { Name = item.Name, Volume = item.VolumePerUnit });
                    }
                }

                // 调用算法（返回 LoadUnloadResult，包含统计和明细）
                var result = LoadAndUnloadBox.CalculateDaily(everygood);
                total40 += result.Count40;
                total20 += result.Count20;
                totalLoose += result.CountLoose;
                totalFee40 += result.Fee40;
                totalFee20 += result.Fee20;
                totalFeeLoose += result.FeeLoose;
                totalFee += result.TotalFee;
                FortyContainers.AddRange(result.FortyContainers);
                TwentyContainers.AddRange(result.TwentyContainers);
            }
            return new LoadUnloadResult
            {
                Count40 = total40,
                Count20 = total20,
                CountLoose = totalLoose,
                Fee40 = totalFee40,
                Fee20 = totalFee20,
                FeeLoose = totalFeeLoose,
                TotalFee = totalFee,
                FortyContainers = FortyContainers,
                TwentyContainers = TwentyContainers
            };
            
        }
        #endregion

        #region 计算高价值打板费
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
        #endregion

        #region 计算拍照费（一个订单拍一次 6HKD/3张）
        public async Task<PhotoResult> PhotoFee(DateTime startTime, DateTime endTime)
        {

            int Po_number = await _db.Queryable<Procure>()
                        .Where(p => p.Instore_time >= startTime && p.Instore_time <= endTime)
                        .Select(p => p.Procure_no).Distinct().CountAsync();
            var So_number = await _db.Queryable<Sale>()
                        .Where(s => s.Outstore_time >= startTime && s.Outstore_time <= endTime)
                        .Select(s => s.Sale_no).Distinct().CountAsync();
            decimal Po_fee = Po_number * 6;
            decimal So_fee = So_number * 6;
            return new PhotoResult
            {
                Po_number = Po_number,
                Po_fee = Po_fee,
                So_number=So_number,
                So_fee = So_fee,
                All_fee = Po_fee+So_fee,
            };
            

        }

        #endregion

        #region 计算理货费
        public async Task<OrganResult> Organ(DateTime startTime, DateTime endTime)
        {
            int pocount = await _db.Queryable<Procure>()
                    .Where(p => p.Instore_time >= startTime && p.Instore_time <= endTime)
                    .SumAsync(p => SqlFunc.ToInt32(p.Number));
            int socount = await _db.Queryable<Sale>()
                    .Where(s => s.Outstore_time >= startTime && s.Outstore_time <= endTime)
                    .SumAsync(s => SqlFunc.ToInt32(s.Number));
            decimal PO_Fee = pocount * ((decimal)0.3);
            decimal SO_Fee = socount * ((decimal)0.3);
            decimal Fee = (pocount + socount) *((decimal)0.3);
            return new OrganResult()
            {
                PO_Number = pocount,
                PO_Fee = PO_Fee,
                SO_Fee = SO_Fee,
                SO_Number = socount,
                ALLFee = Fee
            };

        }
        #endregion

        #region 计算登记费
        public async Task<Register> Register(DateTime startTime, DateTime endTime)
        {
            var poResult = await PoUploadunload(startTime, endTime);
            var soResult = await SoUploadunload(startTime, endTime);

            int Number = poResult.Count40+soResult.Count40+poResult.Count20+soResult.Count20;
            decimal RegisterFee = Number * 600;
            return new Register()
            {
                Number = Number,
                RegisterFee = RegisterFee,
            };
        }

        #endregion

        #region 计算加班费
        public async Task<decimal> Overtime(DateTime startTime, DateTime endTime)
        {
            const decimal pricePerHalfHour = 100;      // 每半小时单价（元）
            const double morningWorkStart = 9.0;       // 上午开始时间（小时）
            const double afternoonWorkEnd = 18.0;      // 下午结束时间（小时）
            const double saturdayWorkEnd = 12.5;       // 周六下班时间（小时）
            var poTimes = await _db.Queryable<Procure>()
                        .Where(p => p.Instore_time >= startTime && p.Instore_time <= endTime)
                        .Select(p => p.Instore_time.Value).ToListAsync();

            var soTimes = await _db.Queryable<Sale>()
                        .Where(s => s.Outstore_time >= startTime && s.Outstore_time <= endTime)
                        .Select(s => s.Outstore_time.Value).ToListAsync();
            var allTimes = poTimes.Concat(soTimes).ToList();
            // 然后按日期分组,获取每天最早和最晚时间
            var allday = allTimes.GroupBy(t => t.Date)
                .Select(g => new { Date = g.Key, Earliest = g.Min(), Latest = g.Max(),Count = g.Count() })
                .OrderBy(x => x.Date).ToList();
            decimal totalfee = 0;
            //遍历每一天
            foreach (var day in allday)
            {
                //获取当前是星期几
                var workday = await _db.Queryable<Workday>().Where(w => SqlFunc.ToDate(w.Date)==SqlFunc.ToDate(day.Date)).FirstAsync();
                int week = workday?.Week ?? (int)day.Date.DayOfWeek; // DayOfWeek 周日=0
                if (week == 0) week = 7; // 统一周日为7
                double morningMinutes= 0;
                double afternoonMinutes = 0;
                double weekendMinutes = 0;
                //根据星期几算出加班时间
                if (week >= 1 && week <= 5) //周一到周五
                {
                    //上午加班时间；早于9点
                    var workstart = day.Date.AddHours(morningWorkStart);
                    if (day.Earliest < workstart)
                        morningMinutes = (workstart - day.Earliest).TotalMinutes;
                    //下午加班时间：晚于18点
                    var workend = day.Date.AddHours(afternoonWorkEnd);
                    if (day.Latest > workend)
                        afternoonMinutes = (day.Latest - workend).TotalMinutes;
                }
                else if (week == 6) // 周六
                {
                    // 上午加班：早于09:00
                    var workStart = day.Date.AddHours(morningWorkStart);
                    if (day.Earliest < workStart)
                        morningMinutes = (workStart - day.Earliest).TotalMinutes;

                    // 下午加班：晚于12:00
                    var satEnd = day.Date.AddHours(saturdayWorkEnd);
                    if (day.Latest > satEnd)
                        afternoonMinutes = (day.Latest - satEnd).TotalMinutes;
                }
                else if (week == 7) // 周日
                {
                    // 周日仅当当天记录总数 >= 2 时才计算
                    if (day.Count >= 2)
                    {
                        // 整个区间都算加班
                        weekendMinutes = (day.Latest - day.Earliest).TotalMinutes;
                    }
                }
                //折算为半小时工时（规则：<15分钟不计，≥15分钟算半小时）
                double ConvertToHalfHourUnits(double minutes)
                {
                    if (minutes <= 0) return 0;
                    int halfHourCount = (int)Math.Floor(minutes / 30);
                    double remainder = minutes % 30;
                    if (remainder >= 15)
                        halfHourCount++;
                    return halfHourCount ; // 返回工时数
                }
                double totalUnits = 0; // 总工时数
                if (week >= 1 && week <= 5 || week == 6)
                {
                    // 工作日和周六：上下班合计计算
                    double allMinutes = ConvertToHalfHourUnits(morningMinutes + afternoonMinutes);
                    totalUnits = allMinutes;
                }
                else if (week == 7)
                {
                    // 周日：只计算总工时（若满足条件）
                    if (day.Count >= 2)
                    {
                        totalUnits = ConvertToHalfHourUnits(weekendMinutes); // weekendMinutes存总分钟数
                    }
                }
                // 计算当日费用
                if (totalUnits > 0)
                {
                    int halfUnits = (int)Math.Round(totalUnits); // 此时已是整数
                    decimal dayFee;
                    if (week == 6 || week == 7)
                    {
                        // 周六日：双倍人员
                        dayFee = halfUnits * pricePerHalfHour * 2;
                    }
                    else
                    {
                        dayFee = halfUnits * pricePerHalfHour;
                    }
                    totalfee += dayFee;
                }


            }
            return totalfee;
        }

        #endregion
    }
}