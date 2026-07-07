using OfficeOpenXml;
using SqlSugar;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.StreamModel;

namespace StreamCore.Service
{
    public class UploadService
    {
        private readonly ISqlSugarClient _db;
        public UploadService([FromKeyedServices("stream")]ISqlSugarClient db)
        { 
            _db = db;
        }
        // po表解析插入数据库
        public async Task<UploadResult> ImportProcureFromExcelAsync(Stream excelStream)
        {
            var result = new UploadResult();
            var errors = new List<string>();
            try
            {
                using var package = new ExcelPackage(excelStream);
                var worksheet = package.Workbook.Worksheets[0]; // 读取第一个工作表
                if (worksheet == null || worksheet.Dimension == null)
                {
                    result.Success = false;
                    result.Message = "Excel 文件为空或格式不正确";
                    return result;
                }
                var rowCount = worksheet.Dimension.Rows;
                if (rowCount < 2) // 假设第一行为标题行
                {
                    result.Success = false;
                    result.Message = "Excel 中没有数据行";
                    return result;
                }
                var procureList = new List<Procure>();
                // 从第二行开始读取（跳过标题）
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var procure = new Procure
                        {
                            Procure_no = GetCellString(worksheet, row, 1),          // 采购单号
                            Create_time = GetCellDateTime(worksheet, row, 2),      // 采购单创建时间
                            State = GetCellString(worksheet, row, 3),              // 采购订单状态
                            Cspu_id = GetCellString(worksheet, row, 4),            // cspu_id
                            Barcode = GetCellString(worksheet, row, 5),            // 条形码
                            Goods_name = GetCellString(worksheet, row, 6),         // 商品名称
                            Instore_no = GetCellString(worksheet, row, 7),         // 入库单号
                            Instore_time = GetCellDateTime(worksheet, row, 8),     // 入库时间
                            Number = GetCellString(worksheet, row, 9),             // 采购数量（文本）
                            Price = GetCellDecimal(worksheet, row, 10),            // 含税单价
                            Price_notax = GetCellString(worksheet, row, 11),       // 不含税单价（文本）
                            System_create_time = DateTime.Now                      //当前时间
                        };

                        // 简单校验：采购单号不能为空
                        if (string.IsNullOrWhiteSpace(procure.Procure_no))
                        {
                            errors.Add($"第 {row} 行采购单号为空，跳过");
                            continue;
                        }
                        procureList.Add(procure);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"第 {row} 行解析失败：{ex.Message}");
                    }
                }

                if (procureList.Count == 0)
                {
                    result.Success = false;
                    result.Message = "没有有效数据可导入";
                    result.Errors = errors;
                    return result;
                }
                // 开启事务，保证数据一致性
                _db.Ado.BeginTran();
                // 批量插入（SqlSugar 自动处理主键自增）
                var insertCount = await _db.Insertable(procureList).ExecuteCommandAsync();
                _db.Ado.CommitTran();
                result.Success = true;
                result.Message = $"成功导入 {insertCount} 条 PO 数据";
                result.InsertCount = insertCount;
                if (errors.Any())
                {
                    result.Errors = errors;
                    result.Message += $"，但有 {errors.Count} 条错误记录（已跳过）";
                }
            }
            catch (Exception ex)
            {
                // 发生异常时回滚事务
                _db.Ado.RollbackTran();

                result.Success = false;
                result.Message = $"导入失败：{ex.Message}";
                result.Errors.Add(ex.StackTrace);
            }
            return result;
        }

        //so表解析插入数据库
        public async Task<UploadResult> ImportSaleFromExcelAsync(Stream excelStream)
        {
            var result = new UploadResult();
            var errors = new List<string>();

            try
            {
                using var package = new ExcelPackage(excelStream);
                var worksheet = package.Workbook.Worksheets[0];
                if (worksheet == null || worksheet.Dimension == null)
                {
                    result.Success = false;
                    result.Message = "Excel 文件为空或格式不正确";
                    return result;
                }
                var rowCount = worksheet.Dimension.Rows;
                if (rowCount < 2)
                {
                    result.Success = false;
                    result.Message = "Excel 中没有数据行";
                    return result;
                }
                var saleList = new List<Sale>();
                // 从第二行开始读取（跳过标题）
                for (int row = 2; row <= rowCount; row++)
                {
                    try
                    {
                        var sale = new Sale
                        {
                            Sale_no = GetCellString(worksheet, row, 1),                // 销售单号
                            Create_time = GetCellDateTime(worksheet, row, 2),         // 销售单创建时间
                            Sale_country = GetCellString(worksheet, row, 3),          // 销售国家
                            Country = GetCellString(worksheet, row, 4),               // 客户国家
                            Incoterms = GetCellString(worksheet, row, 5),             // 贸易术语
                            Brand = GetCellString(worksheet, row, 6),                 // 品牌
                            Category_1 = GetCellString(worksheet, row, 7),            // 一级类目
                            Category_2 = GetCellString(worksheet, row, 8),            // 二级类目
                            Category_3 = GetCellString(worksheet, row, 9),            // 三级类目
                            Sku_code = GetCellString(worksheet, row, 10),             // sku_code
                            Barcode = GetCellString(worksheet, row, 11),              // 条形码
                            Goods_name = GetCellString(worksheet, row, 12),           // 商品名称
                            Logic_no = GetCellString(worksheet, row, 13),             // 逻辑批次号
                            Outstore_notice = GetCellString(worksheet, row, 14),      // 出库通知单
                            Transport_type = GetCellString(worksheet, row, 15),       // 运输方式
                            Outstore_notice_time = GetCellString(worksheet, row, 16), // 出库通知单创建时间（文本）
                            Deliver_time = GetCellString(worksheet, row, 17),         // 发货时间（文本）
                            Outstore_number = GetCellString(worksheet, row, 18),      // 出库单号
                            Outstore_time = GetCellDateTime(worksheet, row, 19),      // 出库时间
                            Number = GetCellString(worksheet, row, 20),               // 出库数量（文本）
                            Procure_no = GetCellString(worksheet, row, 21),           // 采购单号
                            Sale_number = GetCellString(worksheet, row, 22),          // 销售数量（文本）
                            Customer_name = GetCellString(worksheet, row, 23),        // 收货人
                            System_create_time = DateTime.Now,
                            Is_over = 0                                               // Is_over 默认值0
                        };
                        
                        // 校验：销售单号不能为空
                        if (string.IsNullOrWhiteSpace(sale.Sale_no))
                        {
                            errors.Add($"第 {row} 行销售单号为空，跳过");
                            continue;
                        }
                        saleList.Add(sale);
                    }
                    catch (Exception ex)
                    {
                        errors.Add($"第 {row} 行解析失败：{ex.Message}");
                    }
                }

                if (saleList.Count == 0)
                {
                    result.Success = false;
                    result.Message = "没有有效数据可导入";
                    result.Errors = errors;
                    return result;
                }
                // 开启事务
                _db.Ado.BeginTran();
                var insertCount = await _db.Insertable(saleList).ExecuteCommandAsync();
                //事务结束
                _db.Ado.CommitTran();
                result.Success = true;
                result.Message = $"成功导入 {insertCount} 条 SO 数据";
                result.InsertCount = insertCount;
                if (errors.Any())
                {
                    result.Errors = errors;
                    result.Message += $"，但有 {errors.Count} 条错误记录（已跳过）";
                }
            }
            catch (Exception ex)
            {
                _db.Ado.RollbackTran();
                result.Success = false;
                result.Message = $"导入失败：{ex.Message}";
                result.Errors.Add(ex.StackTrace);
            }
            return result;
        }


        # region 辅助方法：安全读取单元格数据

        private string GetCellString(ExcelWorksheet worksheet, int row, int col)
        {
            return worksheet.Cells[row, col]?.Text?.Trim() ?? string.Empty;
        }
        private DateTime? GetCellDateTime(ExcelWorksheet worksheet, int row, int col)
        {
            var val = worksheet.Cells[row, col]?.Text?.Trim();
            if (string.IsNullOrEmpty(val)) return null;
            // 尝试解析标准日期时间
            if (DateTime.TryParse(val, out var dt)) return dt;
            // 如果 Excel 存储的是数字（OADate 格式），尝试转换
            if (double.TryParse(val, out var oaDate))
            {
                try
                {
                    return DateTime.FromOADate(oaDate);
                }
                catch
                {
                    // 转换失败则返回 null
                }
            }
            return null;
        }
        private decimal? GetCellDecimal(ExcelWorksheet worksheet, int row, int col)
        {
            var val = worksheet.Cells[row, col]?.Text?.Trim();
            if (string.IsNullOrEmpty(val)) return null;
            if (decimal.TryParse(val, out var dec)) return dec;
            return null;
        }
        #endregion



    }

}

