namespace StreamCore.StreamModel.DTO
{
    public class CountryCostDetailDto
    {
        public string CountryName { get; set; }
        public int Count40 { get; set; }
        public int Count20 { get; set; }
        public decimal? Price40 { get; set; }      // 40柜
        public decimal? Price20 { get; set; }      // 20柜
        public decimal? OceanCost { get; set; }      // 海运费
        public decimal? ISF { get; set; }
        public decimal? Insurance { get; set; }
        public decimal? PortSurcharge { get; set; }     // 港杂费
        public decimal? DeliveryGoods { get; set; }     // 提货费
        public decimal? ReviceGoods { get; set; }       // 收货+装柜费
        public decimal? Trailer { get; set; }           // 拖车费
        public decimal? Clearance { get; set; }         // 报关费
        public decimal? TotalCost { get; set; }         // 合计
    }
}
