namespace Domain.Models
{
    public class Warehouse
    {
        public Ulid Id { get; init; }
        public string Name { get; set; }
        public Address Address { get; set; }

        // navigation properties
        public Company? Company { get; set; } = null;

        public Warehouse(string name, Address address)
        {
            Name = name;
            Address = address;
        }
    }
}