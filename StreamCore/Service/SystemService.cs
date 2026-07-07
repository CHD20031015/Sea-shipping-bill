using Microsoft.AspNetCore.Components.Web;
using SqlSugar;
using StreamCore.Model;
using StreamCore.StreamModel;
using StreamCore.StreamModel.DTO;

namespace StreamCore.Service
{
    public class SystemService
    {
        private readonly ISqlSugarClient _db;
        public SystemService([FromKeyedServices("stream")] ISqlSugarClient db)
        {
            _db = db;
        }

        //获取国家数据
        public async Task<object> GetallCountry()
        {
            var country = await _db.Queryable<Country>().ToListAsync();
            return country;
        }
        //添加国家
        public async Task<bool> AddCountry(Country country)
        {
            try
            {
                // ExecuteCommandAsync 返回受影响的行数（int）
                int rows = await _db.Insertable<Country>(country).ExecuteCommandAsync();
                return rows > 0; // 插入成功返回 true
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        //更新国家
        public async Task<bool> UpdateCountry(Country country)
        {
            try
            {
                var exist = await _db.Queryable<Country>().Where(c => c.Id == country.Id).FirstAsync();
                if (exist == null)
                    throw new ArgumentException($"Id 为 {country.Id} 的国家不存在");
                exist.Country_name = country.Country_name;
                exist.Code = country.Code;
                // ExecuteCommandAsync 返回受影响的行数（int）
                int rows = await _db.Updateable<Country>(exist).ExecuteCommandAsync();
                return rows > 0; // 插入成功返回 true
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        //删除国家
        public async Task<bool> DeleteCountry(int id)
        {
            try
            {
                // ExecuteCommandAsync 返回受影响的行数（int）
                int rows = await _db.Deleteable<Country>(c=>c.Id==id).ExecuteCommandAsync();
                return rows > 0; // 删除成功返回 true
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }



        //获取港口数据
        public async Task<object> Getallport()
        {
            var port = await _db.Queryable<Country,Port>((c,p)=>c.Id==p.Country_id)
                     .Select((c, p) => new {
                         Id = p.Id,                     // 港口ID
                         Country_id = p.Country_id,     // 国家ID
                         Country_name = c.Country_name,   // 国家名
                         Port_name = p.Port_name,         // 港口名
                         Create_time = p.Create_time      // 假设创建时间属于港口表
                     })
                    .ToListAsync();
            return port;
        }
        //添加港口
        public async Task<bool> AddPort(Port port)
        {
            try
            {
                if (port == null)
                    throw new ArgumentNullException(nameof(port));
                if (string.IsNullOrWhiteSpace(port.Port_name))
                    throw new ArgumentException("港口名称不能为空");
                if (!port.Country_id.HasValue || port.Country_id <= 0)
                    throw new ArgumentException("请选择有效的国家");
                // 设置创建时间
                port.Create_time = DateTime.Now;
                int rows = await _db.Insertable(port).ExecuteCommandAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        //编辑港口
        public async Task<bool> UpdatePort(Port port)
        {
            try 
            {
                if (port == null)
                    throw new ArgumentNullException(nameof(port));
                if (string.IsNullOrWhiteSpace(port.Port_name))
                    throw new ArgumentException("港口名称不能为空");
                if (!port.Country_id.HasValue || port.Country_id <= 0)
                    throw new ArgumentException("请选择有效的国家");
                // 查询现有记录
                var exist = await _db.Queryable<Port>().Where(p => p.Id == port.Id).FirstAsync();
                // 更新允许修改的字段（保留 Create_time）
                exist.Country_id = port.Country_id;
                exist.Port_name = port.Port_name;
                int rows = await _db.Updateable(exist).ExecuteCommandAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        //删除港口
        public async Task<bool> DeletePort(int id)
        {
            try
            {
                // ExecuteCommandAsync 返回受影响的行数（int）
                int rows = await _db.Deleteable<Port>().Where(p => p.Id == id).ExecuteCommandAsync();
                return rows > 0; // 删除成功返回 true
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }

        //获取物流供货商数据
        public async Task<object> Getallportsupplier()
        {
            var portsupplier = await _db.Queryable<StreamModel.Supplier, Template>((s, t) => s.Template_no == t.Id)
                .Select((s, t) => new
                {
                    s.Id,
                    s.Supplier_name,
                    t.Name
                }).ToListAsync();
            return portsupplier;
        }
        //获取所有模板
        public async Task<List<Template>> GetAllTemplates()
        {
            return await _db.Queryable<Template>().OrderBy(t => t.Id).ToListAsync();
        }
        
        // 添加物流供应商
        public async Task<bool> AddPortSupplier(StreamModel.Supplier supplier)
        {
            try
            {
                if (supplier == null)
                    throw new ArgumentNullException(nameof(supplier));
                if (string.IsNullOrWhiteSpace(supplier.Supplier_name))
                    throw new ArgumentException("供应商名称不能为空");
                if (supplier.Template_no <= 0)
                    throw new ArgumentException("请选择有效的模板");
                // 设置创建时间
                supplier.Create_time = DateTime.Now;
                int rows = await _db.Insertable(supplier).ExecuteCommandAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        // 添加费率设置
        public async Task<bool> SaveRate(int supplierId, List<StreamModel.Supplier_port> rates)
        {
            try
            {
                if (supplierId <= 0) throw new ArgumentException("无效供应商ID");
                if (rates == null || rates.Count == 0) throw new ArgumentException("费率数据为空");
                // 验证供应商存在
                var supplier = await _db.Queryable<StreamModel.Supplier>().Where(s => s.Id == supplierId).FirstAsync();
                if (supplier == null) throw new ArgumentException("供应商不存在");
                // 构造新数据，设置供应商ID和创建时间
                var list = new List<StreamModel.Supplier_port>();
                foreach (var item in rates)
                {
                    list.Add(new StreamModel.Supplier_port
                    {
                        Supplier_id = supplierId,
                        Port_id = item.Port_id,
                        Price_20 = item.Price_20,
                        Price_40 = item.Price_40,
                        Create_time = DateTime.Now
                    });
                }
                int rows = await _db.Insertable(list).ExecuteCommandAsync();
                return rows > 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return false;
            }
        }
        // 导出excel文件
        public async Task<List<StreamModel.DTO.SupplierPortRateExportDTO>> GetSupplierPortRatesForExport()
        {
            try 
            {
                var data = await _db.Queryable<StreamModel.Supplier, Supplier_port, Port, Country>(
                        (s, sp, p, c) => s.Id == sp.Supplier_id && sp.Port_id == p.Id && p.Country_id == c.Id)
                    .Select((s, sp, p, c) => new SupplierPortRateExportDTO
                    {
                        SupplierName = s.Supplier_name,
                        CountryName = c.Country_name,
                        PortName = p.Port_name,
                        Price20 = sp.Price_20 ?? 0,
                        Price40 = sp.Price_40 ?? 0
                    }).OrderBy(s => s.SupplierName)
                    .ToListAsync();
                return data;
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== GetSupplierPortRatesForExport 异常 ===");
                Console.WriteLine(ex.ToString());
                throw; // 让 Controller 捕获
            }
        }

        // 获取SKU商品列表（关联goods和sale）
        public async Task<List<SkuDto>> GetSkuList(string? barcode = null, string? goodsName = null)
        {
            var list = await _db.Queryable<StreamModel.Goods, StreamModel.Sale>((g, s) => g.Barcode == s.Barcode)
                    .Select((g, s) => new SkuDto{
                        Id = g.Id,
                        Barcode = g.Barcode,
                        GoodsName = g.Goods_name,
                        Volume = g.Volume,
                        NetWeight = g.Net_weight,
                        IsOverText = SqlFunc.IIF(s.Is_over == 1, "需入纸箱", "未装箱"),  // 根据 is_over 显示文本
                        CreateTime = g.System_create_time
                    }).ToListAsync();
            // 添加模糊查询条件
            if (!string.IsNullOrWhiteSpace(barcode))
                list = list.Where(dto => dto.Barcode.Contains(barcode)).ToList();
            if (!string.IsNullOrWhiteSpace(goodsName))
                list = list.Where(dto => dto.GoodsName.Contains(goodsName)).ToList();
            return list;
        }
        // 删除SKU商品
        public async Task<bool> DeleteSku(int id)
        {
            var list = await _db.Queryable<Goods>().Where(g => g.Id == id).FirstAsync();
            // 先删除订单
            await _db.Deleteable<StreamModel.Sale>().Where(s => s.Barcode == list.Barcode).ExecuteCommandAsync();
            // 先删除goods
            int rows = await _db.Deleteable<Goods>().Where(g => g.Id == id).ExecuteCommandAsync();
            return rows > 0;
        }

        //获取货柜生成列表
        public async Task<object> Getsolist(DateTime? startDate = null, DateTime? endDate = null)
        {
            var query = _db.Queryable<Sale, Procure>((s, p) => s.Procure_no == p.Procure_no);

            if (startDate.HasValue)
                query = query.Where((s, p) => SqlFunc.ToDate(s.Deliver_time) >= startDate.Value);
            if (endDate.HasValue)
            {
                var end = endDate.Value.AddDays(1);
                query = query.Where((s, p) => SqlFunc.ToDate(s.Deliver_time) < end);
            }

            var solist = await query
                .Select((s, p) => new {
                    Sale_no = s.Sale_no,
                    Procure_no = p.Procure_no,
                    Create_time = s.Create_time,
                    Barcode = s.Barcode,
                    Good = p.Goods_name,
                    Transport = s.Transport_type,
                    So_createtime = s.System_create_time,
                    Store_time = s.Outstore_time,
                    Outstore_number = s.Outstore_number,
                    Outstore_time = s.Outstore_time,
                    Sale_number = s.Sale_number
                })
                .ToListAsync();
            return solist;
        }
    }
}
