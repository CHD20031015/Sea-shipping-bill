using SqlSugar;

namespace StreamCore.Model
{
    [SugarTable("workday")]
    public class Workday
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
        [SugarColumn(ColumnName = "date")]
        public string? Date { get; set; }

        /// <summary>
        /// 备  注:星期
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "week")]
        public int? Week { get; set; }

        /// <summary>
        /// 备  注:0工作日1节假日
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "is_work")]
        public int Is_work { get; set; }


    }

}