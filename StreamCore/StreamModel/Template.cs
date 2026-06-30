using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("template")]
    public class Template
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
        [SugarColumn(ColumnName = "name")]
        public string? Name { get; set; }

        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "img")]
        public string? Img { get; set; }


    }

}
