namespace Domain.Models
{
    public class Stock
    {
        public Ulid Id { get; init; }
        public Ulid BinId { get; set; }
        public Ulid ProductId { get; set; }
        public int Quantity { get; set; }
        public Stock(Ulid binId, Ulid productId, int quantity)
        {
            Id = Ulid.NewUlid();
            BinId = binId;
            ProductId = productId;
            Quantity = quantity;
        }
    }
}