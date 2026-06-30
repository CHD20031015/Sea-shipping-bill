using SqlSugar;

namespace StreamCore.Model
{
    [SugarTable("port")]
    public class Port
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "country_id")]
        public int? Country_id { get; set; }

        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "port_name")]
        public string? Port_name { get; set; }

        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }


    }

}
