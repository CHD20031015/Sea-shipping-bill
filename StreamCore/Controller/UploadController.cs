using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCore.Model.DTO;
using StreamCore.Service;

namespace StreamCore.Controller
{
    [ApiController]
    public class UploadController : ControllerBase
    {
        private readonly UploadService _uploadService;
        public UploadController(UploadService uploadService)
        { 
            _uploadService = uploadService;
        }

        [HttpPost]
        [Route("api/upload/procure")]
        public async Task<ActionResult<UploadResult>> UpdateProcure([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new UploadResult { Success = false, Message = "未选择文件" });
            // 可选：校验文件扩展名
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new UploadResult { Success = false, Message = "仅支持 .xlsx 或 .xls 文件" });
            using var stream = file.OpenReadStream();
            var result = await _uploadService.ImportProcureFromExcelAsync(stream);
            return Ok(result);
        }
        [HttpPost]
        [Route("api/upload/sale")]
        public async Task<ActionResult<UploadResult>> UpdateSale([FromForm] IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new UploadResult { Success = false, Message = "未选择文件" });
            // 可选：校验文件扩展名
            var ext = Path.GetExtension(file.FileName).ToLower();
            if (ext != ".xlsx" && ext != ".xls")
                return BadRequest(new UploadResult { Success = false, Message = "仅支持 .xlsx 或 .xls 文件" });
            using var stream = file.OpenReadStream();
            var result = await _uploadService.ImportSaleFromExcelAsync(stream);
            return Ok(result);
        }
    }
}
