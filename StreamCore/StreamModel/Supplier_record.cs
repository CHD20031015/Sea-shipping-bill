using SqlSugar;

namespace StreamCore.StreamModel
{
    [SugarTable("supplier_record")]
    public class Supplier_record
    {


        /// <summary>
        /// 备  注:供货商费用明细
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "id", IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        /// <summary>
        /// 备  注:供应商
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "supplier_id")]
        public int? Supplier_id { get; set; }

        /// <summary>
        /// 备  注:集装箱
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "transport_box_no")]
        public string? Transport_box_no { get; set; }

        /// <summary>
        /// 备  注:海运费用 OceanCost=箱子费用/0.9
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "ocean_cost")]
        public decimal? Ocean_cost { get; set; }

        /// <summary>
        /// 备  注:isf费用
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "isf")]
        public decimal? Isf { get; set; }

        /// <summary>
        /// 备  注:保险费
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "insurance")]
        public decimal? Insurance { get; set; }

        /// <summary>
        /// 备  注:港杂费
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "port_surcharge")]
        public decimal? Port_surcharge { get; set; }

        /// <summary>
        /// 备  注:提货费
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "delivery_goods")]
        public decimal? Delivery_goods { get; set; }

        /// <summary>
        /// 备  注:收货费
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "revice_goods")]
        public decimal? Revice_goods { get; set; }

        /// <summary>
        /// 备  注:拖车费
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "trailer")]
        public decimal? Trailer { get; set; }

        /// <summary>
        /// 备  注:报关费
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "clearance")]
        public decimal? Clearance { get; set; }

        /// <summary>
        /// 备  注:创建时间
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }

        /// <summary>
        /// 备  注:发货时间(箱子的发货时间)
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "deliver_time")]
        public DateTime? Deliver_time { get; set; }

        /// <summary>
        /// 备  注:总费用=各项费用累加
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "all_cost")]
        public decimal? All_cost { get; set; }

        /// <summary>
        /// 备  注:真实费用=80%容积以上按all_cost 80以下按all_cost*容积
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "real_all_cost")]
        public decimal? Real_all_cost { get; set; }


    }

}

