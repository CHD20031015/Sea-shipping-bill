#region 一轮装柜
using SqlSugar;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.StreamModel;
using System.Diagnostics.Eventing.Reader;
namespace StreamCore.Method
{
    public class BoxToContainer
    {
        // 箱体体积（单位：cm³）
        private const double Capacity40 = 12.05 * 2.35 * 2.68 * 1000000.0; // ≈ 75.89 m³
        private const double Capacity20 = 5.69 * 2.13 * 2.18 * 1000000.0;  // ≈ 26.42 m³

        // 装载率（固定85%）
        private const double Rate = 0.85;

        public static LoadUnloadResult LoadBoxs(Queue<Item> boxs)
        {
            // 1. 过滤有效物品并按体积降序排序
            var items = boxs.Where(i => i.Volume > 0).OrderByDescending(i => i.Volume).ToList();
            if (!items.Any())
                return new LoadUnloadResult();

            var fortyContainers = new List<List<Item>>(); // 40柜列表
            var remainingItems = new List<Item>(items);    // 未装物品列表

            double fortyLimit = Capacity40 * Rate; // 40柜可用容量
            double twentyLimit = Capacity20 * Rate; // 20柜可用容量

            // 2. 循环装40柜，直到剩余总体积 <= 20柜容量
            while (remainingItems.Any())
            {
                double totalRemainingVol = remainingItems.Sum(i => i.Volume);
                if (totalRemainingVol <= twentyLimit)
                    break; // 剩余体积 ≤ 20柜容量，改用20柜

                // 创建一个40柜
                var container = new List<Item>();
                double used = 0;

                // 贪心装入：遍历剩余物品（已降序），能装则装
                for (int i = 0; i < remainingItems.Count; i++)
                {
                    var item = remainingItems[i];
                    if (used + item.Volume <= fortyLimit)
                    {
                        used += item.Volume;
                        container.Add(item);
                        remainingItems.RemoveAt(i);
                        i--; // 删除后索引回退
                    }
                    else
                        continue; // 当前物品体积过大，尝试下一个
                }

                if (container.Any())
                    fortyContainers.Add(container);
                else
                    break; // 无物品可装入
            }

            // 3. 处理剩余物品（总体积 ≤ 20柜容量），全部装入一个20柜
            var twentyContainers = new List<List<Item>>();
            if (remainingItems.Any())
            {
                twentyContainers.Add(remainingItems);
            }

            // 4. 返回结果（费用置0）
            return new LoadUnloadResult
            {
                Count40 = fortyContainers.Count,
                Count20 = twentyContainers.Count,
                CountLoose = 0,
                Fee40 = 0,
                Fee20 = 0,
                FeeLoose = 0,
                TotalFee = 0,
                FortyContainers = fortyContainers,
                TwentyContainers = twentyContainers
            };
        }
    }
}
#endregion

//#region 二轮装柜
//using SqlSugar;
//using StreamCore.Model;
//using StreamCore.Model.DTO;
//using StreamCore.StreamModel;
//using System.Diagnostics.Eventing.Reader;
//namespace StreamCore.Method
//{
//    public class BoxToContainer
//    {
//        // 箱体体积（单位：cm³）
//        private const double Capacity40 = 12.05 * 2.35 * 2.68 * 1000000.0; // ≈ 75.89 m³ 
//        private const double Capacity20 = 5.69 * 2.13 * 2.18 * 1000000.0;  // ≈ 26.42 m³

//        // 装载率
//        private const double RateFirst = 0.80;
//        private const double RateSecond = 0.85;

//        public static LoadUnloadResult LoadBoxs(Queue<Item> everygood)
//        {
//            var queue = everygood;
//            if (!queue.Any())
//                return new LoadUnloadResult();

//            var fortyContainers = new List<List<Item>>();

//            // ---------- 第一轮：40柜按80%装载 ----------
//            while (queue.Count > 0)
//            {
//                var container = new List<Item>();
//                double used = 0;
//                while (queue.Count > 0 && used < Capacity40 * RateFirst)
//                {
//                    var item = queue.Peek();
//                    if (used + item.Volume < Capacity40 * RateFirst)
//                    {
//                        used += item.Volume;
//                        container.Add(item);
//                        queue.Dequeue();
//                    }
//                    else
//                        break;
//                }
//                if (container.Any())
//                    fortyContainers.Add(container);
//            }

//            // ---------- 第二轮：拆最后一个柜，按箱子顺序逐个填充至85% ----------
//            List<Item> finalRemaining = new List<Item>();
//            while (fortyContainers.Count > 0)
//            {
//                int lastIdx = fortyContainers.Count - 1;
//                var remaining = new List<Item>(fortyContainers[lastIdx]);
//                fortyContainers.RemoveAt(lastIdx);

//                if (!remaining.Any())
//                    continue;

//                remaining = remaining.OrderByDescending(v => v.Volume).ToList();

//                for (int dest = 0; dest < fortyContainers.Count && remaining.Any(); dest++)
//                {
//                    var target = fortyContainers[dest];
//                    double used = target.Sum(i => i.Volume);
//                    double targetLimit = Capacity40 * RateSecond;

//                    if (used >= targetLimit)
//                        continue;

//                    bool placed;
//                    do
//                    {
//                        placed = false;
//                        for (int i = 0; i < remaining.Count; i++)
//                        {
//                            var item = remaining[i];
//                            if (used + item.Volume <= targetLimit)
//                            {
//                                used += item.Volume;
//                                target.Add(item);
//                                remaining.RemoveAt(i);
//                                placed = true;
//                                break;
//                            }
//                        }
//                    } while (placed && remaining.Any());
//                }

//                if (!remaining.Any())
//                    continue;

//                finalRemaining.AddRange(remaining);
//                break;
//            }

//            // ---------- 合并所有剩余商品 ----------
//            var allRemaining = finalRemaining.OrderByDescending(v => v.Volume).ToList();

//            // 剩余商品处理（无费用，直接装柜）
//            var twentyContainers = new List<List<Item>>();
//            if (allRemaining.Any())
//            {
//                double totalVol = allRemaining.Sum(i => i.Volume);
//                double twentyLimit = Capacity20 * RateSecond;
//                double fortyLimit = Capacity40 * RateSecond;

//                if (totalVol > twentyLimit)   // 剩余总体积超过20柜容量，使用40柜
//                {
//                   fortyContainers.Add(allRemaining);
//                }
//                else   // 剩余总体积 <= 20柜容量，装入一个20柜
//                {
//                    // 将所有剩余物品放入一个20柜
//                    twentyContainers.Add(allRemaining);
//                }
//            }

//            // 最终统计（费用置0）
//            int count40 = fortyContainers.Count;
//            int count20 = twentyContainers.Count;
//            return new LoadUnloadResult
//            {
//                Count40 = count40,
//                Count20 = count20,
//                CountLoose = 0,
//                Fee40 = 0,
//                Fee20 = 0,
//                FeeLoose = 0,
//                TotalFee = 0,
//                FortyContainers = fortyContainers,
//                TwentyContainers = twentyContainers
//            };
//        }
//    }
//}
//#endregion