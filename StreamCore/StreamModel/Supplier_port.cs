using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("supplier_port")]
    public class Supplier_port
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:港口id
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "port_id")]
        public int? Port_id { get; set; }

        /// <summary>
        /// 备  注:20箱价格
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "price_20")]
        public decimal? Price_20 { get; set; }

        /// <summary>
        /// 备  注:40箱价格
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "price_40")]
        public decimal? Price_40 { get; set; }

        /// <summary>
        /// 备  注:供应商id
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "supplier_id")]
        public int? Supplier_id { get; set; }

        /// <summary>
        /// 备  注:创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }


    }

}
