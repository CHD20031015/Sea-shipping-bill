using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace StreamCore.Model.DTO
{
    /// <summary>
    /// 
    ///</summary>
    [SugarTable("supplier")]
    public class Supplier
    {


        /// <summary>
        /// 备  注:
        /// 默认值:
        ///</summary>
        [SugarColumn(ColumnName = "create_time")]
        public DateTime? Create_time { get; set; }


        [SugarColumn(ColumnName = "push_supplier_count")]
        public int? Push_Supplier_Count { get; set; }


        [SugarColumn(ColumnName = "imported_supplier_count")]
        public int? Imported_Supplier_Count { get; set; }


        [SugarColumn(ColumnName = "into_supplier_count")]
        public int? Into_Supplier_Count { get; set; }

        [SugarColumn(ColumnName = "notinto_supplier_count")]
        public int? NotInto_Supplier_Count { get; set; }
    }

}
