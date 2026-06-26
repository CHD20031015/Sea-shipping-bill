using SqlSugar;

namespace StreamCore.Model
{
    [SugarTable("sale")]
    public class Sale
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:销售单号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "sale_no")]
        public string? Sale_no { get; set; }

        /// <summary>
        /// 备  注:销售单创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 备  注:销售国家
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "sale_country")]
        public string? Sale_country { get; set; }

        /// <summary>
        /// 备  注:客户国家
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "country")]
        public string? Country { get; set; }

        /// <summary>
        /// 备  注:贸易术语
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "incoterms")]
        public string? Incoterms { get; set; }

        /// <summary>
        /// 备  注:品牌
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "brand")]
        public string? Brand { get; set; }

        /// <summary>
        /// 备  注:一级类目名称
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "category_1")]
        public string? Category_1 { get; set; }

        /// <summary>
        /// 备  注:二级类目名称
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "category_2")]
        public string? Category_2 { get; set; }

        /// <summary>
        /// 备  注:三级类目名称
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "category_3")]
        public string? Category_3 { get; set; }

        /// <summary>
        /// 备  注:sku_code
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "sku_code")]
        public string? Sku_code { get; set; }

        /// <summary>
        /// 备  注:条形码 关联goods
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "barcode")]
        public string? Barcode { get; set; }

        /// <summary>
        /// 备  注:商品名称
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "goods_name")]
        public string? Goods_name { get; set; }

        /// <summary>
        /// 备  注:逻辑批次号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "logic_no")]
        public string? Logic_no { get; set; }

        /// <summary>
        /// 备  注:出库通知单
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "outstore_notice")]
        public string? Outstore_notice { get; set; }

        /// <summary>
        /// 备  注:运输方式
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "transport_type")]
        public string? Transport_type { get; set; }

        /// <summary>
        /// 备  注:出库通知单创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "outstore_notice_time")]
        public string? Outstore_notice_time { get; set; }

        /// <summary>
        /// 备  注:发货时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "deliver_time")]
        public string? Deliver_time { get; set; }

        /// <summary>
        /// 备  注:出库单号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "outstore_number")]
        public string? Outstore_number { get; set; }

        /// <summary>
        /// 备  注:出库时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "outstore_time")]
        public DateTime? Outstore_time { get; set; }

        /// <summary>
        /// 备  注:出库数量
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "number")]
        public string? Number { get; set; }

        /// <summary>
        /// 备  注:采购单号 关联po
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "procure_no")]
        public string? Procure_no { get; set; }

        /// <summary>
        /// 备  注:销售数量
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "sale_number")]
        public string? Sale_number { get; set; }

        /// <summary>
        /// 备  注:收货人
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "customer_name")]
        public string? Customer_name { get; set; }

        /// <summary>
        /// 备  注:系统创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "system_create_time")]
        public DateTime? System_create_time { get; set; }

        /// <summary>
        /// 备  注:0代表未装箱 1代表已装箱
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "is_over")]
        public int? Is_over { get; set; }
    }
}
