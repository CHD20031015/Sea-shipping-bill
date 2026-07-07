namespace StreamCore.StreamModel.DTO
{
    public class SaleItemDto
    {
        public DateTime? OutstoreTime { get; set; }   // 出库时间
        public string CustomerName { get; set; }
        public string Country { get; set; }
        public string SaleNo { get; set; }
        public string Barcode { get; set; }
        public string GoodsName { get; set; }
        public double Number { get; set; }           // 数量
        public double Volume { get; set; }    // 单个体积（cm³）
        public string Remark { get; set; }           // 大件标识
        public int SaleId { get; set; }       // 对应 Sale 表的主键
    }
}
