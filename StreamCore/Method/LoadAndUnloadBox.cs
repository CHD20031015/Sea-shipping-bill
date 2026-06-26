using SqlSugar;
using StreamCore.Model.DTO;
namespace StreamCore.Method
{
    //计算装卸费方法
    public class LoadAndUnloadBox
    {
        // 箱体体积（单位：m³）
        private const double Capacity40 = 12.05 * 2.35 * 2.68 * 1000000.0; // ≈ 75.89 m³
        private const double Capacity20 = 5.69 * 2.13 * 2.18 * 1000000.0;  // ≈ 26.42 m³
        private const double CapacityLoose = 1.0 * 1000000.0;              // 散板 1 m³

        // 装载率
        private const double RateFirst = 0.80;
        private const double RateSecond = 0.85;

        // 费用单价（HKD）
        private const decimal Price40 = 1100m;
        private const decimal Price20 = 890m;
        private const decimal PriceLoose = 40m;

        public static (int count40, int count20, int countLoose,decimal fee40, decimal fee20, decimal feeLoose,decimal totalFee) CalculateDaily(IEnumerable<double> volumes)
        {
            var volumeList = volumes.Where(v => v > 0).OrderByDescending(v => v).ToList();
            if (!volumeList.Any())
                return (0, 0, 0, 0, 0, 0, 0);

            var queue = new Queue<double>(volumeList);
            var fortyContainers = new List<List<double>>(); // 每个40柜商品明细

            // ---------- 第一轮：40柜按80%装载 ----------
            while (queue.Count > 0)
            {
                var container = new List<double>();
                double used = 0;
                while (queue.Count > 0 && used < Capacity40 * RateFirst)
                {
                    var item = queue.Peek();
                    // 不存在放不下，直接放入
                    used += item;
                    container.Add(item);
                    queue.Dequeue();
                }
                if (container.Any())
                    fortyContainers.Add(container);
            }

            // ---------- 第二轮：反复拆最后一个柜，向前填充至85% ----------
            List<double> finalRemaining = new List<double>();
            while (fortyContainers.Count > 1)
            {
                int lastIdx = fortyContainers.Count - 1;
                var sourceItems = new List<double>(fortyContainers[lastIdx]);
                fortyContainers.RemoveAt(lastIdx);

                if (!sourceItems.Any())
                    continue;

                bool anyFilled = false;
                // 尝试填充到前面的柜子（按顺序）
                for (int dest = 0; dest < fortyContainers.Count && sourceItems.Any(); dest++)
                {
                    var target = fortyContainers[dest];
                    double used = target.Sum();
                    double targetLimit = Capacity40 * RateSecond;

                    if (used >= targetLimit)
                        continue;

                    int idx = 0;
                    while (idx < sourceItems.Count)
                    {
                        var item = sourceItems[idx];
                        if (used + item <= targetLimit)
                        {
                            used += item;
                            target.Add(item);
                            sourceItems.RemoveAt(idx);
                            anyFilled = true;
                        }
                        else
                        {
                            break; // 放不下，换下一个目标柜
                        }
                    }
                }

                // 如果源商品全部用完，继续拆下一个柜子
                if (!sourceItems.Any())
                    continue;

                // 如果源商品有剩余，则停止循环，剩余即为最终剩余
                finalRemaining.AddRange(sourceItems);
                break;
            }

            // ---------- 合并所有剩余商品 ----------
            var allRemaining = finalRemaining.Concat(queue).OrderByDescending(v => v).ToList();

            int extra40 = 0, count20 = 0, countLoose = 0;
            decimal feeExtra40 = 0, fee20 = 0, feeLoose = 0;

            if (allRemaining.Any())
            {
                double totalVol = allRemaining.Sum();
                double twentyLimit = Capacity20 * RateSecond;
                bool hasOversize = allRemaining.Any(v => v > twentyLimit);

                // 散板方案（按体积折算板数）
                double looseCapacity = CapacityLoose * RateSecond; // 0.85 m³/板
                int looseCount = (int)Math.Ceiling(totalVol / looseCapacity);
                decimal looseFee = looseCount * PriceLoose;

                // 判断是否可用20柜：无超大件
                if (!hasOversize)
                {
                    int cnt20 = (int)Math.Ceiling(totalVol / twentyLimit);
                    decimal cost20 = cnt20 * Price20;

                    if (cost20 <= looseFee)
                    {
                        count20 = cnt20;
                        fee20 = cost20;
                    }
                    else
                    {
                        countLoose = looseCount;
                        feeLoose = looseFee;
                    }
                }
                else
                {
                    // 有超大件，只能比较40柜和散板
                    int cnt40 = (int)Math.Ceiling(totalVol / (Capacity40 * RateSecond));
                    decimal cost40 = cnt40 * Price40;

                    if (cost40 <= looseFee)
                    {
                        extra40 = cnt40;
                        feeExtra40 = cost40;
                    }
                    else
                    {
                        countLoose = looseCount;
                        feeLoose = looseFee;
                    }
                }
            }

            // 最终统计
            int count40 = fortyContainers.Count + extra40;
            decimal fee40 = count40 * Price40;
            decimal totalFee = fee40 + fee20 + feeLoose;

            return (count40, count20, countLoose, fee40, fee20, feeLoose, totalFee);
        }
    }
}