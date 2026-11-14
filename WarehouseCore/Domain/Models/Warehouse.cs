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

        // warehouse zones
        public ICollection<WarehouseZone> Zones { get; set; } = new List<WarehouseZone>();
        private Warehouse() // ef
        {
            Name = null!;
            Address = null!;
        }

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