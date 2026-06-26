using Azure.Messaging;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using SqlSugar;
using StackExchange.Redis;
using StreamCore.Model;
using StreamCore.Model.DTO;
using System.Text.Json;


namespace StreamCore.Service
{
    public class ShippingService
    {
        private readonly ISqlSugarClient _db;
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _redisDb;
        public ShippingService([FromKeyedServices("downstream")] ISqlSugarClient db, IConnectionMultiplexer redis)
        {
            _db = db;
            _redis = redis;
            _redisDb = _redis.GetDatabase();
        }
        // 查询订单全表数据方法
        public async Task<List<Model.DTO.Order>> GetOrdersAsync()
        { 
            var orders = await _db.Queryable<Record>()
                .Select(x => new Model.DTO.Order(){
                    Create_time = x.Create_time,
                    Push_Order_Count = x.Push_order_count,
                    Imported_Order_Count = x.Imported_order_count,
                    Into_Order_Count = x.Imported_order_count,
                    NotInto_Order_Count = x.Push_order_count-x.Imported_order_count,
                    Order_Price = x.Order_price,
                    Supplier_Price = x.Supplier_price
                }).ToListAsync();
            return orders;
        }
        //查询买家全表数据方法
        public async Task<List<Model.DTO.Buyer>> GetBuyersAsync()
        {
            var buyers = await _db.Queryable<Record>()
                .Select(x => new Model.DTO.Buyer()
                {
                    Create_time = x.Create_time,
                    Push_Buyer_Count = x.Push_buyer_count,
                    Imported_Buyer_Count = x.Imported_buyer_count,
                    Into_Buyer_Count = x.Imported_buyer_count,
                    NotInto_Buyer_Count = x.Push_buyer_count - x.Imported_buyer_count
                }).ToListAsync();
            return buyers;
        }
        //查询供应商全表数据方法
        public async Task<List<Model.DTO.Supplier>> GetSuppliersAsync()
        {
            var suppliers = await _db.Queryable<Record>()
                .Select(x => new Model.DTO.Supplier()
                {
                    Create_time = x.Create_time,
                    Push_Supplier_Count = x.Push_supplier_count,
                    Imported_Supplier_Count = x.Imported_supplier_count,
                    Into_Supplier_Count = x.Imported_supplier_count,
                    NotInto_Supplier_Count = x.Push_supplier_count - x.Imported_supplier_count
                }).ToListAsync();
            return suppliers;
        }
        //切换链路点击方法
        public async Task<bool> SwitchLinkAsync(DateTime targetDate)
        {
            //var today = targetDate.Date; // 确保只取年月日，忽略时间部分
            //var startTime = today.AddHours(23).AddMinutes(55);   // 当天 23:55:00
            //var endTime = today.AddDays(1).AddHours(0).AddMinutes(1); // 次日 00:01:00
            var today = targetDate.Date; // 确保只取年月日，忽略时间部分
            var startTime = today.AddHours(00).AddMinutes(00);
            var endTime = today.AddDays(1).AddHours(0).AddMinutes(0); // 次日 00:00:00

            #region 获取要删除的数据
            // 查询待删除的订单
            var ordersToDelete = await _db.Queryable<Model.Order>()
                .Where(o => o.Deliver_time >= startTime && o.Deliver_time <= endTime)
                .ToListAsync();
            // 提取要删除订单中的买家ID和供应商ID（用于关联删除）
            var buyerIdsFromOrders = ordersToDelete.Select(o => o.Buyer_id).Distinct().ToList();
            var supplierIdsFromOrders = ordersToDelete.Select(o => o.Supplier_id).Distinct().ToList();

            // 查询待删除的买家：
            var buyersToDelete = await _db.Queryable<Model.Buyer>()
                .Where(b => b.Create_time_buyer >= startTime && b.Create_time_buyer <= endTime)
                .ToListAsync();

            if (buyerIdsFromOrders.Any())
            {
                var relatedBuyers = await _db.Queryable<Model.Buyer>()
                    .Where(b => buyerIdsFromOrders.Contains(b.Buyer_id))
                    .ToListAsync();
                buyersToDelete = buyersToDelete.Union(relatedBuyers).Distinct().ToList();
            }

            // 查询待删除的供应商：
            var suppliersToDelete = await _db.Queryable<Model.Supplier>()
                .Where(s => s.Create_time_supplier >= startTime && s.Create_time_supplier <= endTime)
                .ToListAsync();

            if (supplierIdsFromOrders.Any())
            {
                var relatedSuppliers = await _db.Queryable<Model.Supplier>()
                    .Where(s => supplierIdsFromOrders.Contains(s.Supplier_id))
                    .ToListAsync();
                suppliersToDelete = suppliersToDelete.Union(relatedSuppliers).Distinct().ToList();
            }
            #endregion

            #region 获取redis中当天的订单、买家、供应商数据
            var gb = _redis.GetDatabase();
            // 订单（处理 Data 包装）
            var orderValues = await gb.ListRangeAsync("order_day_messages");
            foreach (var order in orderValues)
            {
                var gomessage = await _redisDb.ListLeftPopAsync("order_day_messages");
                if (gomessage.IsNull) break;
            }
            var orders = new List<Model.Order>();
            foreach (var value in orderValues)
            {
                using var doc = JsonDocument.Parse(value.ToString());
                var orderJson = doc.RootElement.GetProperty("Data").GetRawText();
                var order = JsonConvert.DeserializeObject<Model.Order>(orderJson);
                if (order != null) orders.Add(order);
            }
            //买家
            var buyerValues = await gb.ListRangeAsync("buyer_day_messages");
            foreach (var buyer in buyerValues)
            {
                var gomessage = await _redisDb.ListLeftPopAsync("buyer_day_messages");
                if (gomessage.IsNull) break;
            }
            var buyerValues2 = await gb.ListRangeAsync("new_buyer");
            foreach (var buyer in buyerValues2)
            {
                var gomessage = await _redisDb.ListLeftPopAsync("new_buyer");
                if (gomessage.IsNull) break;
            }
            var allbuyerValues = buyerValues.Union(buyerValues2).ToList();
            var buyers = new List<Model.Buyer>();
            foreach (var value in allbuyerValues)
            {
                using var doc = JsonDocument.Parse(value.ToString());
                var buyerJson = doc.RootElement.GetProperty("Data").GetRawText();
                var buyer = JsonConvert.DeserializeObject<Model.Buyer>(buyerJson);
                if (buyer != null) buyers.Add(buyer);
            }
            // 供应商
            var supplierValues = await gb.ListRangeAsync("supplier_day_messages");
            foreach (var supplier in supplierValues)
            {
                var gomessage = await _redisDb.ListLeftPopAsync("supplier_day_messages");
                if (gomessage.IsNull) break;
            }
            var supplierValues2 = await gb.ListRangeAsync("new_supplier");
            foreach (var supplier in supplierValues2)
            {
                var gomessage = await _redisDb.ListLeftPopAsync("new_supplier");
                if (gomessage.IsNull) break;
            }
            var allsupplierValues = supplierValues.Union(supplierValues2).ToList();
            var suppliers = new List<Model.Supplier>();
            foreach (var value in allsupplierValues)
            {
                using var doc = JsonDocument.Parse(value.ToString());
                var supplierJson = doc.RootElement.GetProperty("Data").GetRawText();
                var supplier = JsonConvert.DeserializeObject<Model.Supplier>(supplierJson);
                if (supplier != null) suppliers.Add(supplier);
            }
            foreach (var value in supplierValues2)
            {
                using var doc = JsonDocument.Parse(value.ToString());
                var supplierJson = doc.RootElement.GetProperty("Data").GetRawText();
                var supplier = JsonConvert.DeserializeObject<Model.Supplier>(supplierJson);
                if (supplier != null) suppliers.Add(supplier);
            }
            if (orders.Count == 0 || buyers.Count == 0 || suppliers.Count == 0)
            {
                throw new Exception("Redis 中某类数据为空，请检查");
            }

            #endregion
            // 2. 开启数据库事务
            await _db.Ado.BeginTranAsync();
            try
            {
                //删除三个表当天的数据
                if (ordersToDelete.Any())
                {
                    var orderIds = ordersToDelete.Select(o => o.Id).ToList();
                    await _db.Deleteable<Model.Order>().Where(o => orderIds.Contains(o.Id)).ExecuteCommandAsync();
                }
                if (buyersToDelete.Any())
                {
                    var buyerIds = buyersToDelete.Select(b => b.Buyer_id).ToList();
                    await _db.Deleteable<Model.Buyer>().Where(b => buyerIds.Contains(b.Buyer_id)).ExecuteCommandAsync();
                }
                if (suppliersToDelete.Any())
                {
                    var supplierIds = suppliersToDelete.Select(s => s.Supplier_id).ToList();
                    await _db.Deleteable<Model.Supplier>().Where(s => supplierIds.Contains(s.Supplier_id)).ExecuteCommandAsync();
                }
                // 插入从 Redis 获取的全量数据
                if (orders.Any()) await _db.Insertable(orders).MySqlIgnore().ExecuteCommandAsync();
                if (buyers.Any()) await _db.Insertable(buyers).MySqlIgnore().ExecuteCommandAsync();
                if (suppliers.Any()) await _db.Insertable(suppliers).MySqlIgnore().ExecuteCommandAsync();
                //提交事务
                await _db.Ado.CommitTranAsync();
                #region 重新计算当天的订单统计数据
                var orderStats = await _db.Queryable<Model.Order>()
                    .Where(o => o.Deliver_time >= startTime && o.Deliver_time <= endTime)
                    .Select(o => new
                    {
                        Imported_order_count = SqlFunc.AggregateCount(o.Deliver_time), // 注意 SqlSugar 的聚合
                        Order_Price = Convert.ToDecimal(SqlFunc.AggregateSum(o.Total_price)),
                        Supplier_Price = Convert.ToDecimal(SqlFunc.AggregateSum(o.Supplier_price))
                    }).ToListAsync();
                #endregion
                #region 重新计算当天的用户统计数据
                var buyerStats = await _db.Queryable<Model.Buyer>()
                    .Where(b => b.Create_time_buyer >= startTime && b.Create_time_buyer <= endTime)
                    .Select(b => new
                    {
                        Imported_buyer_count = SqlFunc.AggregateCount(b.Create_time_buyer)
                    }).ToListAsync();
                #endregion
                #region 重新计算当天的供应商统计数据
                var supplierStats = await _db.Queryable<Model.Supplier>()
                    .Where(s => s.Create_time_supplier >= startTime && s.Create_time_supplier <= endTime)
                    .Select(s => new
                    {
                        Imported_supplier_count = SqlFunc.AggregateCount(s.Create_time_supplier)
                    }).ToListAsync();
                #endregion
                var record = await _db.Queryable<Record>().FirstAsync(r => r.Create_time == today);
                record.Imported_order_count = orderStats.FirstOrDefault()?.Imported_order_count ?? 0;
                record.Imported_supplier_count = supplierStats.FirstOrDefault()?.Imported_supplier_count ?? 0;
                record.Imported_buyer_count = buyerStats.FirstOrDefault()?.Imported_buyer_count ?? 0;
                record.Order_price = orderStats.FirstOrDefault()?.Order_Price ?? 0;
                record.Supplier_price = orderStats.FirstOrDefault()?.Supplier_Price ?? 0;
                await _db.Updateable(record).WhereColumns(r => r.Create_time).ExecuteCommandAsync();
                return true;
            }
            catch
            {
                //回滚事务
                await _db.Ado.RollbackTranAsync();
                throw;
            }
        }
    }
}
