namespace Domain.Models
{
    public class Warehouse
    {
        public Ulid Id { get; init; }

        public string Name { get; set; }
        public Address Address { get; set; }
        public bool IsActive { get; set; }

        // company
        public Ulid CompanyId { get; set; }
        public Company? Company { get; set; } = null;

        public Warehouse(string name, Address address, Ulid companyId)
        {
            Id = Ulid.NewUlid();
            CompanyId = companyId;
            Name = name;
            Address = address;
            IsActive = true;
        }
    }
}