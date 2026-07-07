namespace StreamCore.StreamModel.DTO
{
    public class ContainerListDTO
    {
        public string SupplierName { get; set; }
        public string CountryName { get; set; }
        public DateTime? DeliverTime { get; set; }
        public string TypeDisplay { get; set; }     // "20'柜" 或 "40'柜"
        public decimal? AllVolume { get; set; }
        public decimal? RealityVolume { get; set; }
        public decimal? VolumeRadio { get; set; }   // 0~1 小数
        public string? Transport_box_no { get; set; } //货柜编号
        public decimal? ContainerPrice { get; set; } // 对应柜型价格
        public decimal? OceanCost { get; set; }
        public decimal? Isf { get; set; }
        public decimal? Insurance { get; set; }
        public decimal? PortSurcharge { get; set; }
        public decimal? DeliveryGoods { get; set; }
        public decimal? ReviceGoods { get; set; }
        public decimal? Trailer { get; set; }
        public decimal? Clearance { get; set; }
        public decimal? AllCost { get; set; }
        public decimal? RealAllCost { get; set; }
        public string TransportBoxNo { get; set; }
        public int? Type { get; set; }  // 2或4，用于前端判断
        public bool IsMixed { get; set; }           //判断是否散装
    }
}
