using Aspose.Words;
using Aspose.Words.Drawing;
using Aspose.Words.Fonts;
using Aspose.Words.Saving;
using Aspose.Words.Tables;
using OfficeOpenXml;
using SqlSugar;
using StreamCore.StreamModel;
using System.Collections;

namespace StreamCore.Service
{
    public class WordtopdfService
    {
        private readonly ISqlSugarClient _db;
        public WordtopdfService([FromKeyedServices("stream")] ISqlSugarClient db)
        {
            _db = db;
        }

        public async Task<object> MatchContainersAsync(IFormFile file)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("文件不能为空");

            // 解析 Excel，获取表头映射和所有行数据
            var excelRows = new List<Dictionary<string, string>>();
            var headerMap = new Dictionary<int, string>();
            using (var stream = new MemoryStream())
            {
                await file.CopyToAsync(stream);
                using (var package = new ExcelPackage(stream))
                {
                    var worksheet = package.Workbook.Worksheets[0];
                    if (worksheet == null)
                        throw new Exception("Excel 工作表为空");

                    // 读取表头（第一行）
                    for (int col = 1; col <= worksheet.Dimension.Columns; col++)
                    {
                        var cellValue = worksheet.Cells[1, col].Text?.Trim();
                        if (!string.IsNullOrEmpty(cellValue))
                            headerMap[col] = cellValue;
                    }

                    // 读取数据行（从第2行开始）
                    for (int row = 2; row <= worksheet.Dimension.Rows; row++)
                    {
                        var rowData = new Dictionary<string, string>();
                        foreach (var kv in headerMap)
                        {
                            var cell = worksheet.Cells[row, kv.Key];
                            rowData[kv.Value] = cell.Text?.Trim() ?? "";
                        }
                        excelRows.Add(rowData);
                    }
                }
            }

            // 提取所有货柜编号（假设列名为 "货柜编号" 或 "Container No."）
            var boxNoColumn = headerMap.Values.FirstOrDefault(k => k.Contains("货柜编号") || k.Contains("Container No."));
            var allBoxNos = excelRows
                .Select(r => r.ContainsKey(boxNoColumn) ? r[boxNoColumn] : null)
                .Where(v => !string.IsNullOrEmpty(v))
                .Distinct()
                .ToList();

            // 查询这些柜子的容积率
            var containers = await _db.Queryable<Transportbox>()
                .Where(t => allBoxNos.Contains(t.Transport_box_no))
                .Select(t => new { t.Transport_box_no, t.Volume_radio })
                .ToListAsync();

            // 构造返回结果，包含 excelData
            var result = allBoxNos.Select(boxNo =>
            {
                var container = containers.FirstOrDefault(c => c.Transport_box_no == boxNo);
                bool canDownload = container != null && container.Volume_radio > 0.8m;
                var excelData = excelRows.FirstOrDefault(r => r[boxNoColumn] == boxNo) ?? new Dictionary<string, string>();
                return new
                {
                    transportBoxNo = boxNo,
                    canDownload = canDownload,
                    excelData = excelData
                };
            }).ToList();

            return result;
        }

        public byte[] Wordtopdf(int templateType, string outFileName, Dictionary<string, string> placeholderValues)
        {
            string pdfFilePath = @$"TempDocument/{outFileName}";
            string tempDocx = @$"Template/test{templateType}.docx";
            var fontSource = @"Resource/fonts";

            // 使用完全限定名
            Aspose.Words.Document doc = new Aspose.Words.Document(tempDocx);
            // 替换占位符
            foreach (var placeholder in placeholderValues)
            {
                doc.Range.Replace(placeholder.Key, placeholder.Value, new Aspose.Words.Replacing.FindReplaceOptions());
            }
            //设置字体
            ArrayList arry = new ArrayList(FontSettings.DefaultInstance.GetFontsSources());
            FolderFontSource folderFontSource = new FolderFontSource(fontSource, true);
            arry.Add(folderFontSource);
            FontSourceBase[] fontSourceBases = (FontSourceBase[])arry.ToArray(typeof(FontSourceBase));
            FontSettings.DefaultInstance.SetFontsSources(fontSourceBases);
            doc.Save(pdfFilePath, SaveFormat.Pdf);


            byte[] pdfBytes = File.ReadAllBytes(pdfFilePath);
            File.Delete(pdfFilePath);
            return pdfBytes;
        }
    }

}