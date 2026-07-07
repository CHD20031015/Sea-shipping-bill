using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("paper_box_detail")]
    public class Paperboxdetail
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:纸箱编号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "paper_box_no")]
        public string? Paper_box_no { get; set; }

        /// <summary>
        /// 备  注:商品id
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "barcode")]
        public string? Barcode { get; set; }

        /// <summary>
        /// 备  注:SO单编号
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "sale_no")]
        public string? Sale_no { get; set; }

        /// <summary>
        /// 备  注:商品体积
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "volume")]
        public double? Volume { get; set; }

        /// <summary>
        /// 备  注:创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }


    }

}
