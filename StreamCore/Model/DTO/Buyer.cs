using SqlSugar;

namespace StreamCore.Model.DTO
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("buyer")]
    public class Buyer
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }


        [SugarColumn(ColumnName = "push_buyer_count")]
        public int? Push_Buyer_Count { get; set; }


        [SugarColumn(ColumnName = "imported_buyer_count")]
        public int? Imported_Buyer_Count { get; set; }

        [SugarColumn(ColumnName = "into_buyer_count")]
        public int? Into_Buyer_Count { get; set; }

        [SugarColumn(ColumnName = "notinto_buyer_count")]
        public int? NotInto_Buyer_Count { get; set; }

    }

}
