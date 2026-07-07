using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("paper_box")]
    public class Paperbox
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:实际体积
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "reality_volume")]
        public decimal? Reality_volume { get; set; }

        /// <summary>
        /// 备  注:客户名称
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "customer_name")]
        public string? Customer_name { get; set; }

        /// <summary>
        /// 备  注:目的国
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "country_name")]
        public string? Country_name { get; set; }

        /// <summary>
        /// 备  注:发货时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "deliver_time")]
        public DateTime? Deliver_time { get; set; }

        /// <summary>
        /// 备  注:创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 备  注:1 1号纸箱 3 3号纸箱 5 5号纸箱
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "type")]
        public int? Type { get; set; }

        /// <summary>
        /// 备  注:柜子编号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "transport_box_no")]
        public string? Transport_box_no { get; set; }

        /// <summary>
        /// 备  注:0表示纸箱 1表示商品
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "is_box")]
        public int? Is_box { get; set; }

        /// <summary>
        /// 备  注:运单号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "sale_no")]
        public string? Sale_no { get; set; }

        /// <summary>
        /// 备  注:商品码 非纸箱商品时有用
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "barcode")]
        public string? Barcode { get; set; }

        /// <summary>
        /// 备  注:纸箱编号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "paper_box_no")]
        public string? Paper_box_no { get; set; }

        [SugarColumn(IsIgnore =true)]
        public List<Paperboxdetail> PaperboxdetailList { set; get; }  //每个纸箱详情
    }

}
