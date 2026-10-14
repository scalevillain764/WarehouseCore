namespace Domain.Models
{
    public class Company
    {
        public Ulid Id { get; init; }
        public string Name { get; init; }
        public bool IsActive { get; set; }

        // navigation properties
        public ICollection<Warehouse> Warehouses { get; set; } = [];
        public ICollection<User> Workers { get; set; } = [];

        public Company(string name)
        {
            Id = Ulid.NewUlid();
            Name = name;
            IsActive = true;
        }
    }
}