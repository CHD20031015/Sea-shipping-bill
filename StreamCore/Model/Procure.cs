using SqlSugar;

namespace StreamCore.Model
{
    [SugarTable("procure")]
    public class Procure
    {
        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:采购单号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "procure_no")]
        public string? Procure_no { get; set; }

        /// <summary>
        /// 备  注:采购单创建时间(ymdhms)
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 备  注:采购订单状态
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "state")]
        public string? State { get; set; }

        /// <summary>
        /// 备  注:cspu_id
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "cspu_id")]
        public string? Cspu_id { get; set; }

        /// <summary>
        /// 备  注:条形码
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
        /// 备  注:入库单号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "instore_no")]
        public string? Instore_no { get; set; }

        /// <summary>
        /// 备  注:入库时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "instore_time")]
        public DateTime? Instore_time { get; set; }

        /// <summary>
        /// 备  注:采购数量
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "number")]
        public string? Number { get; set; }

        /// <summary>
        /// 备  注:采购含税商品单价_人民币
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "price")]
        public decimal? Price { get; set; }

        /// <summary>
        /// 备  注:采购不含税商品单价_人民币
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "price_notax")]
        public string? Price_notax { get; set; }

        /// <summary>
        /// 备  注:系统创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "system_create_time")]
        public DateTime? System_create_time { get; set; }


    }

}
