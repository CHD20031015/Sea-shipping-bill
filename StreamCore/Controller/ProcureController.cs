using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCore.Model;
using StreamCore.Service;

namespace StreamCore.Controller
{
    [ApiController]
    public class ProcureController : ControllerBase
    {
        private ProcureService _procureService;
        public ProcureController(ProcureService procureService)
        {
            _procureService = procureService;
        }
        [HttpGet]
        [Route("api/shipping/procure")]
        public async Task<IActionResult> Getprocure(DateTime? startTime, DateTime? endTime, int page, int pageSize)
        {
            try
            {
                // 使用分页查询
                var procure = await _procureService.getallProcure(startTime, endTime, page, pageSize);
                return Ok(procure);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrders error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }
        
        [HttpGet]
        [Route("api/shipping/sale")]
        public async Task<IActionResult> GetSale(DateTime? startTime, DateTime? endTime, int page, int pageSize)
        {
            try
            {
                // 使用分页查询
                var sale = await _procureService.getallSale(startTime, endTime, page, pageSize);
                return Ok(sale);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrders error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message, detail = ex.StackTrace });
            }
        }

        [HttpGet]
        [Route("api/shipping/Warehouserent")]
        public async Task<IActionResult> Warehouserent(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });

                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                // 调用服务层
                var result = await _procureService.Warehouserent(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"仓租费计算异常: {ex}");
                return StatusCode(500, new { message = "计算失败", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/PoUpload")]
        public async Task<IActionResult> PoUpload(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });

                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                // 调用服务层
                var result = await _procureService.PoUploadunload(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"仓租费计算异常: {ex}");
                return StatusCode(500, new { message = "计算失败", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/SoUpload")]
        public async Task<IActionResult> SoUpload(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });

                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                // 调用服务层
                var result = await _procureService.SoUploadunload(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"仓租费计算异常: {ex}");
                return StatusCode(500, new { message = "计算失败", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/Highpallet")]
        public async Task<IActionResult> Highpallet(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });
                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                //调用服务层
                var result = await _procureService.Highpallet(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                // 记录日志
                Console.WriteLine($"高价值打板费计算异常: {ex}");
                return StatusCode(500, new { message = "计算失败", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/Photo")]
        public async Task<IActionResult> Photo(DateTime startTime,DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });
                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                //调用服务层
                var result = await _procureService.PhotoFee(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                //记录错误
                Console.WriteLine($"拍照费计算异常：{ex}");
                return StatusCode(500, new { message = "计算错误", detail = ex.Message });
        }
    }

        [HttpGet]
        [Route("api/shipping/Organ")]
        public async Task<IActionResult> Organ(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });
                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                //调用服务层
                var result = await _procureService.Organ(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                //记录错误
                Console.WriteLine($"理货费计算异常：{ex}");
                return StatusCode(500, new { message = "计算错误", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/Register")]
        public async Task<IActionResult> Register(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });
                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                //调用服务层
                var result = await _procureService.Register(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                //记录错误
                Console.WriteLine($"理货费计算异常：{ex}");
                return StatusCode(500, new { message = "计算错误", detail = ex.Message });
            }
        }

        [HttpGet]
        [Route("api/shipping/Overtime")]
        public async Task<IActionResult> Overtime(DateTime startTime, DateTime endTime)
        {
            try
            {
                // 参数验证
                if (startTime == default || endTime == default)
                    return BadRequest(new { message = "日期参数无效" });
                if (startTime > endTime)
                    return BadRequest(new { message = "开始日期不能晚于结束日期" });
                //调用服务层
                var result = await _procureService.Overtime(startTime, endTime);
                return Ok(result);
            }
            catch (Exception ex)
            {
                //记录错误
                Console.WriteLine($"理货费计算异常：{ex}");
                return StatusCode(500, new { message = "计算错误", detail = ex.Message });
            }
        }

        
    }
}