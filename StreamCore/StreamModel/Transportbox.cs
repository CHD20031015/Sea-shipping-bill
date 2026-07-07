using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("transport_box")]
    public class Transportbox
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:最大容积
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "all_volume")]
        public decimal? All_volume { get; set; }

        /// <summary>
        /// 备  注:实际容积
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "reality_volume")]
        public decimal? Reality_volume { get; set; }

        /// <summary>
        /// 备  注:4代表40集装箱 2代表20集装箱
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "type")]
        public int? Type { get; set; }

        /// <summary>
        /// 备  注:货物装箱时（3天内）
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "deliver_time")]
        public DateTime? Deliver_time { get; set; }

        /// <summary>
        /// 备  注:目的国
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "country_name")]
        public string? Country_name { get; set; }

        /// <summary>
        /// 备  注:创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 备  注:供应商id(费用最低的)
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "supplier_id")]
        public int? Supplier_id { get; set; }

        /// <summary>
        /// 备  注:港口id
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "port_id")]
        public int? Port_id { get; set; }

        /// <summary>
        /// 备  注:20箱价格
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "price_20")]
        public decimal? Price_20 { get; set; }

        /// <summary>
        /// 备  注:40箱价格
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "price_40")]
        public decimal? Price_40 { get; set; }

        /// <summary>
        /// 备  注:运柜编号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "transport_box_no")]
        public string? Transport_box_no { get; set; }

        /// <summary>
        /// 备  注:容积
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "volume_radio")]
        public decimal? Volume_radio { get; set; }

        [SugarColumn(IsIgnore = true)]
        public List<Paperbox> PaperboxlList { set; get; }  //柜子里的所有纸箱

    }

}
