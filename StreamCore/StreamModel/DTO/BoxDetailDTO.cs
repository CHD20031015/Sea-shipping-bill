namespace StreamCore.StreamModel.DTO
{
    public class BoxDetailDto
    {
        public string PaperBoxNo { get; set; }
        public DateTime? OutstoreTime { get; set; }   // 出库时间
        public string CountryName { get; set; }       // 销售国家
        public string GoodsName { get; set; }         // 商品名称 或 纸箱型号（含编号）
        public int? Quantity { get; set; }            // 数量
        public decimal? Volume { get; set; }          // 总体积（可选）
        public string CustomerName { get; set; }      // 收货人
        public int is_box { get; set; }                 // 0=商品, 1=纸箱
        
    }
}
    
