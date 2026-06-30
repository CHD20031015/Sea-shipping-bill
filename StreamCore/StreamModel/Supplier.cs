using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("supplier")]
    public class Supplier
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id")]
        public int? Id { get; set; }

        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "supplier_name")]
        public string? Supplier_name { get; set; }
        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "template_no")]
        public int? Template_no { get; set; }


    }

}
