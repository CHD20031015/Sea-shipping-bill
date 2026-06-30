using SqlSugar;

namespace StreamCore.StreamModel.DTO
{
    public class SupplierPortRateExportDTO
    {
        [SugarColumn(ColumnName = "supplier_name")]
        public string SupplierName { get; set; }
        [SugarColumn(ColumnName = "country_name")]
        public string CountryName { get; set; }
        [SugarColumn(ColumnName = "port_name")]
        public string PortName { get; set; }
        [SugarColumn(ColumnName = "price_20")]
        public decimal Price20 { get; set; }
        [SugarColumn(ColumnName = "price_40")]
        public decimal Price40 { get; set; }
    }
}
