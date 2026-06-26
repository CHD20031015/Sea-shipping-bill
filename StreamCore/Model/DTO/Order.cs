using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StreamCore.Model.DTO
{
    [SugarTable("order")]
    public class Order
    {
        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }


        [SugarColumn(ColumnName = "push_order_count")]
        public int? Push_Order_Count { get; set; }


        [SugarColumn(ColumnName = "imported_order_count")]
        public int? Imported_Order_Count { get; set; }

        [SugarColumn(ColumnName = "into_order_count")]
        public int? Into_Order_Count { get; set; }

        [SugarColumn(ColumnName = "notinto_order_count")]
        public int? NotInto_Order_Count { get; set; }

        [SugarColumn(ColumnName = "order_price")]
        public decimal? Order_Price { get; set; }

        [SugarColumn(ColumnName = "supplier_price")]
        public decimal? Supplier_Price { get; set; }

    }

}
