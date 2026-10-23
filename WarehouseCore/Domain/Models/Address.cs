namespace Domain.Models
{
    public class Address
    {
        public string Country { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string NumberBuilding { get; set; }
        public Address(string country, string city, string street, string numberBuilding)
        {
            Country = country;
            City = city;
            Street = street;
            NumberBuilding = numberBuilding;
        }
    }
}