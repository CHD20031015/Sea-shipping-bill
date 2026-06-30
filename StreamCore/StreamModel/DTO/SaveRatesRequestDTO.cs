namespace StreamCore.StreamModel.DTO
{
    public class SaveRatesRequestDTO
    {
        public int SupplierId { get; set; }
        public List<Supplier_port> Rates { get; set; }
    }
}
