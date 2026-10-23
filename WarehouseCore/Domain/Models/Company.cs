using System.Diagnostics.CodeAnalysis;

namespace Domain.Models
{
    public class Company
    {
        public Ulid Id { get; init; }
        public required string Name { get; set; }
        public required string SchemaName { get; init; }
        public required string Code { get; init; }
        public bool IsActive { get; set; }

        [SetsRequiredMembers]
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