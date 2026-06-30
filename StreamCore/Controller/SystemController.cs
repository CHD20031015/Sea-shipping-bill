using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCore.Model;
using StreamCore.Model.DTO;
using StreamCore.Service;

namespace StreamCore.Controller
{
    
    [ApiController]
    public class SystemController : ControllerBase
    {
        private SystemService _systemService;
        public SystemController(SystemService systemService)
        {
            _systemService = systemService;
        }

        [HttpGet]
        [Route("api/shipping/getCountry")]
        public async Task<IActionResult> Getcountry()
        {
            try
            {
                var procure = await _systemService.GetallCountry();
                return Ok(procure);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetCountry error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }
        [HttpPost]
        [Route("api/shipping/addCountry")]
        public async Task<IActionResult> addcountry([FromBody] Country country)
        {
            try
            {
                bool addcountry = await _systemService.AddCountry(country);
                if (addcountry)
                {
                    return Ok(true); // 200 OK，返回 true
                }
                else
                {
                    return StatusCode(500, "添加国家失败，未影响任何记录");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"add error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // 返回 500 Internal Server Error
                return StatusCode(500, new { error = "服务器内部错误", detail = ex.Message });
            }
        }
        [HttpPut]
        [Route("api/shipping/updateCountry")]
        public async Task<IActionResult> UpdateCountry([FromBody] Country country)
        {
            try
            {
                bool result = await _systemService.UpdateCountry(country);
                if (result)
                {
                    return Ok(true);
                }
                else
                {
                    return StatusCode(500, "更新国家失败，未影响任何记录");
                }
            }
            catch (ArgumentException ex) // 业务校验异常（如名称空、不存在）
            {
                return BadRequest(new { error = ex.Message });
            }
        }
        [HttpDelete]
        [Route("api/shipping/deleteCountry")]
        public async Task<IActionResult> deletecountry(int id)
        {
            try
            {
                bool deletecountry = await _systemService.DeleteCountry(id);
                if (deletecountry)
                {
                    return Ok(true); // 200 OK，返回 true
                }
                else
                {
                    return StatusCode(500, "添加国家失败，未影响任何记录");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"add error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // 返回 500 Internal Server Error
                return StatusCode(500, new { error = "服务器内部错误", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/getPort")]
        public async Task<IActionResult> GetPort()
        {
            try
            {
                var port = await _systemService.Getallport();
                return Ok(port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPort error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpPost]
        [Route("api/shipping/addPort")]
        public async Task<IActionResult> AddPort([FromBody] Port port)
        {
            try
            {
                bool result = await _systemService.AddPort(port);
                if (result)
                    return Ok(true);
                else
                    return StatusCode(500, "添加港口失败");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
           
        }
        [HttpPut]
        [Route("api/shipping/updatePort")]
        public async Task<IActionResult> UpdatePort([FromBody] Port port)
        {
            try
            {
                bool result = await _systemService.UpdatePort(port);
                if (result)
                    return Ok(true);
                else
                    return StatusCode(500, "更新港口失败");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
            
        }
        [HttpDelete]
        [Route("api/shipping/deletePort")]
        public async Task<IActionResult> DeletePort(int id)
        {
            try
            {
                bool result = await _systemService.DeletePort(id);
                if (result)
                    return Ok(true);
                else
                    return StatusCode(500, "删除港口失败");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/getPortsupplier")]
        public async Task<IActionResult> GetPortsupplier()
        {
            try
            {
                var port = await _systemService.Getallportsupplier();
                return Ok(port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPortSupplier error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpGet]
        [Route("api/shipping/getTemplates")]
        public async Task<IActionResult> GetTemplates()
        {
            try
            {
                var template = await _systemService.GetAllTemplates();
                return Ok(template);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetTemplate error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpPost]
        [Route("api/shipping/addPortSupplier")]
        public async Task<IActionResult> AddPortSupplier([FromBody] StreamModel.Supplier supplier)
        {
            try
            {
                bool result = await _systemService.AddPortSupplier(supplier);
                if (result)
                    return Ok(true);
                else
                    return StatusCode(500, "添加供应商失败");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpPost]
        [Route("api/shipping/saveSupplierRates")]
        public async Task<IActionResult> SaveSupplierRates([FromBody] StreamModel.DTO.SaveRatesRequestDTO request)
        {
            try
            {
                bool result = await _systemService.SaveRate(request.SupplierId, request.Rates);
                if (result)
                    return Ok(true);
                else
                    return StatusCode(500, "保存失败");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/exportSupplierRates")]
        public async Task<IActionResult> ExportSupplierRates()
        {
            try
            {
                var data = await _systemService.GetSupplierPortRatesForExport();

                using var package = new OfficeOpenXml.ExcelPackage();
                var worksheet = package.Workbook.Worksheets.Add("Sheet1");

                // 设置表头
                var headers = new[] { "供应商名", "国家", "港口", "20柜价格", "40柜价格" };
                for (int i = 0; i < headers.Length; i++)
                {
                    worksheet.Cells[1, i + 1].Value = headers[i];
                    worksheet.Cells[1, i + 1].Style.Font.Bold = true;
                }
                // 填充数据
                int row = 2;
                foreach (var item in data)
                {
                    worksheet.Cells[row, 1].Value = item.SupplierName;
                    worksheet.Cells[row, 2].Value = item.CountryName;
                    worksheet.Cells[row, 3].Value = item.PortName;
                    worksheet.Cells[row, 4].Value = item.Price20;
                    worksheet.Cells[row, 5].Value = item.Price40;
                    row++;
                }

                // 自动调整列宽
                worksheet.Cells[1, 1, row - 1, 5].AutoFitColumns();

                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                var fileName = "供应商口岸.xlsx";
                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "导出失败", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/getSku")]
        public async Task<IActionResult> GetSku(string? barcode = null, string? goodsName = null)
        {
            try
            {
                var port = await _systemService.GetSkuList();
                return Ok(port);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetPortSupplier error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpDelete]
        [Route("api/shipping/deleteSku")]
        public async Task<IActionResult> DeleteSku(int id)
        {
            try
            {
                bool result = await _systemService.DeleteSku(id);
                if (result)
                    return Ok(true);
                else
                    return StatusCode(500, "删除商品信息失败");
            }
            catch (ArgumentException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/getSolist")]
        public async Task<IActionResult> GetSoList(DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var result = await _systemService.Getsolist(startDate, endDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

    }
}
