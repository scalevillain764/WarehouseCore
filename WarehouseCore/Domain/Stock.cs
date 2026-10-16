namespace Domain.Models
{
    public class Stock
    {
        public Ulid Id { get; init; }
        public Ulid BinId { get; set; }
        public Ulid ProductId { get; set; }
        public int Quantity { get; set; }
    }
}