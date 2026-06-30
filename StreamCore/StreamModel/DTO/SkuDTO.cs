namespace StreamCore.StreamModel.DTO
{
    public class SkuDto
    {
        public int Id { get; set; }          // goods.Id，用于删除
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public string? Volume { get; set; }
        public string? NetWeight { get; set; }
        public string IsOverText { get; set; } // 装箱状态文本
        public DateTime? CreateTime { get; set; }
    }
}
