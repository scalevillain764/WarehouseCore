using System.Drawing;

namespace Domain.Models
{
    public class Product
    {
        public Ulid Id { get; init; }
        public required string Name { get; init; }
        public string? Description { get; set; }
        public ProductCategory Category { get; set; }
        public required string Brand { get; set; }
        public UnitOfMeasure UnitOfMeasure { get; set; }
        public decimal WeightPerUnit { get; set; }
        public bool IsActive { get; set; }
        public string? Color { get; set; }
        public required string SKU { get; init; }

        // stocks
        public ICollection<Stock> Stocks { get; set; } = [];
        
        private string BuildSKU()
        {
            string idString = Id.ToString().ToUpper();

            var idPart = idString
                .AsSpan(idString.Length - 5, 5);

            string namePart = Name.Replace(" ", "")
                .Substring(0, Math.Min(3, Name.Length)).ToUpper();
            string brandPart = Brand.Replace(" ", "")
                .Substring(0, Math.Min(3, Brand.Length)).ToUpper();
            string catPart = Category.ToString().Replace(" ", "")
                .Substring(0, Math.Min(3, Category.ToString().Length)).ToUpper();

            return $"{idPart}-{namePart}-{brandPart}-{catPart}";
        }
        public Product(string name, string? description, ProductCategory category, string brand, UnitOfMeasure unitOfMeasure,       
           decimal weightPerUnit ,string? color)
        {
            Id = Ulid.NewUlid();
            Name = name;
            Description = description;
            Category = category;
            Brand = brand;
            UnitOfMeasure = unitOfMeasure;
            WeightPerUnit = weightPerUnit;
            Color = color;
            SKU = BuildSKU();
            IsActive = true;
        }
    }
}