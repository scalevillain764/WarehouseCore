namespace Domain.Models
{
    public class Company
    {
        public Ulid Id { get; init; }
        public required string Name { get; init; }
        public required string SchemaName { get; init; }
        public required string Code { get; init; }
        public bool IsActive { get; set; }

        // navigation properties
        public ICollection<Warehouse> Warehouses { get; set; } = [];
        public ICollection<User> Workers { get; set; } = [];

        public Company(string name, string code)
        {
            Id = Ulid.NewUlid();
            Name = name;
            SchemaName = $"company_{Id.ToString().ToLowerInvariant()}";
            Code = code;
            IsActive = true;
        }
    }
}