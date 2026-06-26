using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCore.Service;

namespace StreamCore.Controller
{
    [ApiController]
    public class ShippingController : ControllerBase
    {
        private readonly ShippingService _shippingService;
        public ShippingController(ShippingService shippingService)
        {
            _shippingService = shippingService;
        }


        [HttpGet]
        [Route("api/shipping/order")]
        public async Task<IActionResult> GetOrders()
        {
            try
            {
                var orders = await _shippingService.GetOrdersAsync();
                return Ok(orders);
            }
            catch (Exception ex)
            {
                // 日志记录
                Console.WriteLine($"GetOrders error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // 返回具体错误信息到前端
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpGet]
        [Route("api/shipping/buyer")]
        public async Task<IActionResult> GetBuyers()
        {
            try
            {
                var buyers = await _shippingService.GetBuyersAsync();
                return Ok(buyers);
            }
            catch (Exception ex)
            {
                // 日志记录
                Console.WriteLine($"GetBuyers error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // 返回具体错误信息到前端
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpGet]
        [Route("api/shipping/supplier")]
        public async Task<IActionResult> GetSuppliers()
        {
            try
            {
                var suppliers = await _shippingService.GetSuppliersAsync();
                return Ok(suppliers);
            }
            catch (Exception ex)
            {
                // 日志记录
                Console.WriteLine($"GetSuppliers error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // 返回具体错误信息到前端
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpPost]
        [Route("api/shipping/switchlink")]
        public async Task<IActionResult> SwitchLink([FromBody] DateTime targetDate)
        {
            try
            {
                var result = await _shippingService.SwitchLinkAsync(targetDate);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 日志记录
                Console.WriteLine($"SwitchLink error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                // 返回具体错误信息到前端
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }
    }
}
