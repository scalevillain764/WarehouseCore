namespace Domain.Models
{
    public class User
    {
        public Ulid Id { get; init; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string RefreshTokenHash { get; set; }
        public DateTime RefreshTokenExpiresAt { get; set; }
        
        // navigation properties
        public Company? Company { get; set; } = null;

        public User(string firstName, string secondName, string email, string passwordHash, string refreshTokenHash, DateTime refreshTokenExpiersAt)
        {
            Id = Ulid.NewUlid();
            FirstName = firstName;
            SecondName = secondName;
            Email = email;
            PasswordHash = passwordHash;
            RefreshTokenHash = refreshTokenHash;
            RefreshTokenExpiresAt = refreshTokenExpiersAt;
        }
    }
}