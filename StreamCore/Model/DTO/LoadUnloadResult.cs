namespace StreamCore.Model.DTO
{
    public class LoadUnloadResult
    {
        public int Count40 { get; set; }      // 40柜数量
        public decimal Fee40 { get; set; }      // 40柜总费用
        public int Count20 { get; set; }      // 20柜数量
        public decimal Fee20 { get; set; }      // 20柜总费用
        public int CountLoose { get; set; }   // 散板数量
        public decimal FeeLoose { get; set; }   // 散板总费用
        public decimal TotalFee { get; set; } // 总费用（HKD）
    }
}
