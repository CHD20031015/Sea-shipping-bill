using SqlSugar;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.StreamModel;
using System.Diagnostics.Eventing.Reader;
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
        private const decimal Price40 = 1100;
        private const decimal Price20 = 890;
        private const decimal PriceLoose = 40;

        public static LoadUnloadResult CalculateDaily(Queue<Item> everygood)
        {
            var queue = everygood;
            if (!queue.Any())
                return new LoadUnloadResult();

            var fortyContainers = new List<List<Item>>();

            // ---------- 第一轮：40柜按80%装载 ----------
            while (queue.Count > 0)
            {
                var container = new List<Item>();
                double used = 0;
                while (queue.Count > 0 && used < Capacity40 * RateFirst)
                {
                    var item = queue.Peek();
                    if (used + item.Volume < Capacity40 * RateFirst)
                    {
                        used += item.Volume;
                        container.Add(item);
                        queue.Dequeue();
                    }
                    else
                        break;
                }
                if (container.Any())
                    fortyContainers.Add(container);
            }

            // ---------- 第二轮：拆最后一个柜，按箱子顺序逐个填充至85% ----------
            List<Item> finalRemaining = new List<Item>();
            while (fortyContainers.Count > 0)
            {
                int lastIdx = fortyContainers.Count - 1;
                var remaining = new List<Item>(fortyContainers[lastIdx]);
                fortyContainers.RemoveAt(lastIdx);

                if (!remaining.Any())
                    continue;

                remaining = remaining.OrderByDescending(v => v.Volume).ToList();

                for (int dest = 0; dest < fortyContainers.Count && remaining.Any(); dest++)
                {
                    var target = fortyContainers[dest];
                    double used = target.Sum(i => i.Volume);
                    double targetLimit = Capacity40 * RateSecond;

                    if (used >= targetLimit)
                        continue;

                    bool placed;
                    do
                    {
                        placed = false;
                        for (int i = 0; i < remaining.Count; i++)
                        {
                            var item = remaining[i];
                            if (used + item.Volume <= targetLimit)
                            {
                                used += item.Volume;
                                target.Add(item);
                                remaining.RemoveAt(i);
                                placed = true;
                                break;
                            }
                        }
                    } while (placed && remaining.Any());
                }

                if (!remaining.Any())
                    continue;

                finalRemaining.AddRange(remaining);
                break;
            }

            // ---------- 合并所有剩余商品 ----------
            var allRemaining = finalRemaining.OrderByDescending(v => v.Volume).ToList();
            int extra40 = 0, count20 = 0, countLoose = 0;
            decimal feeExtra40 = 0, fee20 = 0, feeLoose = 0;
            var twentyContainers = new List<List<Item>>();

            if (allRemaining.Any())
            {
                double totalVol = allRemaining.Sum(i => i.Volume);
                double twentyLimit = Capacity20 * RateSecond;
                double fortyLimit = Capacity40 * RateSecond;

                // ========== 修正点：按理论计算柜数，不模拟装箱 ==========
                if (totalVol > twentyLimit)   // 总体积超过20柜容量，使用40柜
                {
                    int cnt40 = (int)Math.Ceiling(totalVol / fortyLimit);
                    extra40 = cnt40;
                    feeExtra40 = cnt40 * Price40;

                    // 按理论柜数分配商品到各个40柜
                    var extraContainers = new List<List<Item>>();
                    for (int i = 0; i < cnt40; i++)
                        extraContainers.Add(new List<Item>());

                    int idx = 0;
                    foreach (var item in allRemaining)
                    {
                        extraContainers[idx % cnt40].Add(item);
                        idx++;
                    }
                    fortyContainers.AddRange(extraContainers);
                }
                else   // 总体积 <= 20柜容量，比较20柜与散板
                {
                    // 20柜方案（理论）
                    int cnt20_theory = (int)Math.Ceiling(totalVol / twentyLimit);
                    decimal cost20 = cnt20_theory * Price20;

                    // 散板方案
                    double looseCapacity = CapacityLoose * RateSecond;
                    int looseCount = (int)Math.Ceiling(totalVol / looseCapacity);
                    decimal looseFee = looseCount * PriceLoose;

                    if (cost20 <= looseFee)
                    {
                        // 采用20柜
                        var extraContainers = new List<List<Item>>();
                        for (int i = 0; i < cnt20_theory; i++)
                            extraContainers.Add(new List<Item>());

                        int idx = 0;
                        foreach (var item in allRemaining)
                        {
                            extraContainers[idx % cnt20_theory].Add(item);
                            idx++;
                        }
                        twentyContainers.AddRange(extraContainers);
                        count20 = cnt20_theory;
                        fee20 = cost20;
                    }
                    else
                    {
                        // 采用散板（无明细，只记录数量）
                        countLoose = looseCount;
                        feeLoose = looseFee;
                    }
                }
            }
            // 最终统计
            int count40 = fortyContainers.Count;
            decimal fee40 = count40 * Price40;
            decimal totalFee = fee40 + fee20 + feeLoose;
            return new LoadUnloadResult
            {
                Count40 = count40,
                Count20 = count20,
                CountLoose = countLoose,
                Fee40 = fee40,
                Fee20 = fee20,
                FeeLoose = feeLoose,
                TotalFee = totalFee,
                FortyContainers = fortyContainers,
                TwentyContainers = twentyContainers
            };
        }
    }
}