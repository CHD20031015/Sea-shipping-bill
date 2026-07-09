using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCore.Service;

namespace StreamCore.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class LoadingcontainerController : ControllerBase
    {
        private readonly LoadingcontainerService _loadingcontainerService;
        public LoadingcontainerController(LoadingcontainerService loadingcontainerService)
        {
            _loadingcontainerService = loadingcontainerService;
        }
        [HttpGet]
        [Route("/api/shipping/Loadcontainer")]
        public async Task<IActionResult> Loadcontainer(DateTime startTime, DateTime endTime)
        {
            {
                try
                {
                    var procure = await _loadingcontainerService.SOloadbox(startTime, endTime);
                    return Ok(procure);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetCountry error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
                }
            }
        }

        [HttpGet]
        [Route("/api/shipping/Containerdetail")]
        public async Task<IActionResult> Containerdetail(DateTime startTime, DateTime endTime)
        {
            {
                try
                {
                    var list = await _loadingcontainerService.GetTransport(startTime, endTime);
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetCountry error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
                }
            }
        }

        [HttpGet]
        [Route("/api/shipping/GetCostDetails")]
        public async Task<IActionResult> GetCostDetails(DateTime startTime, DateTime endTime)
        {
            {
                try
                {
                    var list = await _loadingcontainerService.GetCostDetails(startTime, endTime);
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetCountry error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
                }
            }
        }


        [HttpGet]
        [Route("/api/shipping/GetAllSupplierPrices")]
        public async Task<IActionResult> GetAllSupplierPrices([FromQuery] string countryName)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(countryName))
                    return BadRequest(new { error = "国家名称不能为空" });

                var list = await _loadingcontainerService.GetAllSupplierPricesByCountry(countryName);
                return Ok(list);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }

        [HttpGet]
        [Route("/api/shipping/GetContainerList")]
        public async Task<IActionResult> GetContainerList(DateTime? startTime, DateTime? endTime, int? supplierId, string?countryName)
        {
            {
                try
                {
                    var list = await _loadingcontainerService.GetContainerList(startTime, endTime, supplierId, countryName);
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetCountry error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
                }
            }
        }


        [HttpGet]
        [Route("/api/shipping/GetBoxDetails")]
        public async Task<IActionResult> GetBoxDetails(string transportBoxNo)
        {
            {
                try
                {
                    var list = await _loadingcontainerService.GetBoxDetails(transportBoxNo);
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetCountry error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
                }
            }
        }

        [HttpGet]
        [Route("/api/shipping/GetPaperBoxDetails")]
        public async Task<IActionResult> GetPaperBoxDetails(string paperBoxNo)
        {
            {
                try
                {
                    var list = await _loadingcontainerService.GetPaperBoxDetails(paperBoxNo);
                    return Ok(list);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"GetCountry error: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
                }
            }
        }

        
        [HttpGet]
        [Route("/api/shipping/ExportContainerListAsync")]
        public async Task<IActionResult> ExportContainerListAsync(DateTime? startTime, DateTime? endTime, int? supplierId, string?countryName)
        {
            {
                try
                {
                    var fileBytes = await _loadingcontainerService.ExportContainerListAsync(startTime, endTime, supplierId, countryName);
                    return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", $"装柜列表_{DateTime.Now:yyyyMMddHHmmss}.xlsx");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"导出错误: {ex.Message}");
                    Console.WriteLine(ex.StackTrace);
                    return StatusCode(500, new { error = "导出失败，请检查数据或联系管理员" });
                }
            }
        }
    }
}
