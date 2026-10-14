namespace Domain.Models
{
    public record Address(
        string Country,
        string City,
        string Street,
        string NumberBuilding);
}