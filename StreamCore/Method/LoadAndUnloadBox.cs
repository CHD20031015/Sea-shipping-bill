using SqlSugar;
using StreamCore.Model.DTO;
namespace StreamCore.Method
{
    //计算装卸费方法
    public class LoadAndUnloadBox
    {
        // 实际海运箱体积（单位：立方米）
        private const double Capacity40 = 12.05 * 2.35 * 2.68; // ≈ 75.89 m³
        private const double Capacity20 = 5.69 * 2.13 * 2.18;  // ≈ 26.42 m³
        // 装载率
        private const double RateFirst = 0.80;
        private const double RateSecond = 0.85;
        // 费用单价（HKD）
        private const decimal Price40 = 1100;
        private const decimal Price20 = 890;
        private const decimal PriceLoose = 40; // 散板按每板计费
        // 计算单日装卸费用
        public static (int count40, int count20, int countLoose, decimal fee40, decimal fee20, decimal feeLoose, decimal totalFee) CalculateDaily(IEnumerable<double> volumes)
        {
            var volumeList = volumes.Where(v => v > 0).OrderByDescending(v => v).ToList();
            if (!volumeList.Any())
                return (0, 0, 0, 0, 0, 0, 0);

            var queue = new Queue<double>(volumeList);
            var fortyContainers = new List<double>();

            // ---------- 第一轮：按80%装载率装40柜 ----------
            while (queue.Count > 0)
            {
                double currentUsed = 0;
                while (queue.Count > 0 && currentUsed < Capacity40 * RateFirst)
                {
                    var item = queue.Peek();
                    if (currentUsed + item <= Capacity40 * RateFirst)
                    {
                        currentUsed += item;
                        queue.Dequeue();
                    }
                    else
                    {
                        break;
                    }
                }
                if (currentUsed > 0)
                    fortyContainers.Add(currentUsed);
            }

            // ---------- 第二轮：调整40柜至85%装载率 ----------
            if (fortyContainers.Count > 0)
            {
                //for (int i=fortyContainers.Count-1;i>0;i--)
                //{
                //    if (fortyContainers[i] < 0)
                //        continue;
                //    var item = fortyContainers[i];
                //    for (int j = 0; j < fortyContainers.Count - 1; j++)
                //    {
                //        double needVolume = Capacity40 * RateSecond - fortyContainers[j];
                //        item = item - needVolume;
                //        if (item <= 0)
                //            break;
                //    }
                //}






                for (int i = fortyContainers.Count - 1; i >= 0; i--)
                {
                    if (fortyContainers[i] < 0)
                        continue;

                    double needed = Capacity40 * RateSecond - fortyContainers[i];

                    for (int j = i + 1; j < fortyContainers.Count; j++)
                    {
                        if (needed <= 0) break;
                        double available = fortyContainers[j] - Capacity40 * RateSecond;
                        if (available > 0)
                        {
                            double taken = Math.Min(available, needed);
                            fortyContainers[j] -= taken;
                            fortyContainers[i] += taken;
                            needed -= taken;
                        }
                    }

                    if (needed > 0 && queue.Count > 0)
                    {
                        while (queue.Count > 0 && needed > 0)
                        {
                            var item = queue.Peek();
                            if (item <= needed)
                            {
                                needed -= item;
                                fortyContainers[i] += item;
                                queue.Dequeue();
                            }
                            else
                            {
                                fortyContainers[i] += item;
                                queue.Dequeue();
                                needed = 0;
                            }
                        }
                    }
                }
                fortyContainers = fortyContainers.Where(v => v > 0).ToList();
            }

            // ---------- 剩余商品处理 ----------
            int extra40 = 0, count20 = 0, countLoose = 0;
            decimal feeExtra40 = 0, fee20 = 0, feeLoose = 0;
            var remaining = queue.ToList();

            if (remaining.Any())
            {
                var normalItems = new List<double>();
                var oversizeItems = new List<double>();
                double twentyLimit = Capacity20 * RateSecond;

                foreach (var vol in remaining)
                {
                    if (vol > twentyLimit)
                        oversizeItems.Add(vol);
                    else
                        normalItems.Add(vol);
                }

                // 处理超大件：使用40柜
                if (oversizeItems.Any())
                {
                    var oversizeQueue = new Queue<double>(oversizeItems.OrderByDescending(v => v));
                    while (oversizeQueue.Count > 0)
                    {
                        double used = 0;
                        while (oversizeQueue.Count > 0 && used < Capacity40 * RateSecond)
                        {
                            var item = oversizeQueue.Peek();
                            if (used + item <= Capacity40 * RateSecond)
                            {
                                used += item;
                                oversizeQueue.Dequeue();
                            }
                            else
                            {
                                used += item;
                                oversizeQueue.Dequeue();
                                break;
                            }
                        }
                        if (used > 0)
                        {
                            extra40++;
                            feeExtra40 += Price40;
                        }
                    }
                }

                // 处理普通件：比较20柜和散板
                if (normalItems.Any())
                {
                    double twentyUsed = 0;
                    int tempCount20 = 0;
                    decimal tempFee20 = 0;
                    var sortedNormal = normalItems.OrderByDescending(v => v).ToList();
                    foreach (var vol in sortedNormal)
                    {
                        if (twentyUsed + vol <= twentyLimit)
                        {
                            twentyUsed += vol;
                        }
                        else
                        {
                            if (twentyUsed > 0)
                            {
                                tempCount20++;
                                tempFee20 += Price20;
                            }
                            twentyUsed = vol;
                        }
                    }
                    if (twentyUsed > 0)
                    {
                        tempCount20++;
                        tempFee20 += Price20;
                    }

                    int normalCount = normalItems.Count;
                    decimal looseFeeForNormal = normalCount * PriceLoose;

                    if (tempFee20 <= looseFeeForNormal)
                    {
                        count20 = tempCount20;
                        fee20 = tempFee20;
                    }
                    else
                    {
                        countLoose = normalCount;
                        feeLoose = looseFeeForNormal;
                    }
                }
            }

            int count40 = fortyContainers.Count + extra40;
            decimal fee40 = count40 * Price40;
            decimal totalFee = fee40 + fee20 + feeLoose;

            return (count40, count20, countLoose, fee40, fee20, feeLoose, totalFee);
        }
    }
}
