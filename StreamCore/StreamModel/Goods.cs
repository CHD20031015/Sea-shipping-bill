using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("goods")]
    public class Goods
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:条码
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
        /// 备  注:长
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "length")]
        public string? Length { get; set; }

        /// <summary>
        /// 备  注:宽
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "width")]
        public string? Width { get; set; }

        /// <summary>
        /// 备  注:高
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "height")]
        public string? Height { get; set; }

        /// <summary>
        /// 备  注:体积 立方厘米
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "volume")]
        public string? Volume { get; set; }

        /// <summary>
        /// 备  注:净重
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "net_weight")]
        public string? Net_weight { get; set; }

        /// <summary>
        /// 备  注:毛重kg
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "gross_weight")]
        public string? Gross_weight { get; set; }

        /// <summary>
        /// 备  注:商品名称2
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "goods_name2")]
        public string? Goods_name2 { get; set; }

        /// <summary>
        /// 备  注:j1
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "j1")]
        public string? J1 { get; set; }

        /// <summary>
        /// 备  注:立方米
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "volume_m")]
        public string? Volume_m { get; set; }

        /// <summary>
        /// 备  注:纸箱装箱字段，如果字段是不为空则不需要装箱
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "remark")]
        public string? Remark { get; set; }

        /// <summary>
        /// 备  注:系统创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "system_create_time")]
        public DateTime? System_create_time { get; set; }


    }

}
