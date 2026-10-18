namespace Domain.Models
{
    public class Stock
    {
        public Ulid Id { get; init; }
        
        // bin
        public Ulid BinId { get; set; }
        public Bin Bin { get; set; } = null!;

        // product
        public Ulid ProductId { get; set; }
        public Product Product { get; set; } = null!;

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