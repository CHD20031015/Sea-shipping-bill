namespace StreamCore.Model.DTO
{
    public class UploadResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? InsertCount { get; set; }
        public List<string> Errors { get; set; } = new();
    }
}
