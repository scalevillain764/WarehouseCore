namespace Domain.Models
{
    public class User
    {
        public Ulid Id { get; init; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Email { get; set; }
        public string? PasswordHash { get; set; }
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiersAt { get; set; }
        public UserType Type { get; set; }
        // company
        public Ulid CompanyId { get; set; }

        public User(UserType type, string firstName, string secondName, string email, string? passwordHash, string? refreshTokenHash, DateTime? refreshTokenExpiersAt, Ulid companyId)
        {
            Id = Ulid.NewUlid();
            CompanyId = companyId;
            FirstName = firstName;
            SecondName = secondName;
            Type = type;
            Email = email;
            PasswordHash = passwordHash;
            RefreshTokenHash = refreshTokenHash;
            RefreshTokenExpiersAt = refreshTokenExpiersAt;
        }
    }
}