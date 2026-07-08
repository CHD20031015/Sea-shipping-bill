using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StreamCore.Service;

namespace StreamCore.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class WordtopdfController : ControllerBase
    {
        private readonly WordtopdfService _wordtopdfService;
        public WordtopdfController(WordtopdfService wordtopdfService)
        {
            _wordtopdfService = wordtopdfService;
        }

        [HttpPost]
        [Route("/api/shipping/Excel")]
        public async Task<IActionResult> UploadExcel(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return BadRequest(new { error = "请选择有效的 Excel 文件" });

                var result = await _wordtopdfService.MatchContainersAsync(file);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }


        [HttpPost]
        [Route("/api/shipping/Wordtopdf")]
        public IActionResult Wordtopdf([FromBody] GenerateBillRequest request)
        {
            try
            {
                if (request == null || request.placeholders == null || request.placeholders.Count == 0)
                    return BadRequest(new { error = "缺少占位符数据" });

                string fileName = Guid.NewGuid().ToString() + ".pdf";
                var result = _wordtopdfService.Wordtopdf(request.templateType, fileName, request.placeholders);
                return File(result, "application/pdf", fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Wordtopdf error: {ex.Message}");
                Console.WriteLine(ex.StackTrace);
                return StatusCode(500, new { error = ex.Message });
            }
        }
        public class GenerateBillRequest
        {
            public int templateType { get; set; }
            public Dictionary<string, string> placeholders { get; set; }
        }
    }
}
