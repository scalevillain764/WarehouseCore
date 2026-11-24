using Microsoft.AspNetCore.Identity;

namespace Domain.Models
{
    public class User
    {
        public Ulid Id { get; init; }
        public string FirstName { get; set; }
        public string SecondName { get; set; }
        public string Email { get; set; }
        public string Login { get; set; }
        public string? PasswordHash { get; set; }
        public string? RefreshTokenHash { get; set; }
        public DateTime? RefreshTokenExpiersAt { get; set; }
        public UserType Type { get; set; }
        // company
        public Ulid CompanyId { get; set; }

        // Tests
        public User(string firstName, Ulid companyId)
        {
            Id = Ulid.NewUlid();
            CompanyId = companyId;
            FirstName = firstName;
            SecondName = string.Empty;
            Type = UserType.Admin;
            Email = string.Empty;
            Login = string.Empty;
            PasswordHash = string.Empty;
            RefreshTokenHash = string.Empty;
            RefreshTokenExpiersAt = null;
        }

        public User(Ulid id, string firstName, Ulid companyId, UserType type)
        {
            Id = id;
            CompanyId = companyId;
            FirstName = firstName;
            SecondName = string.Empty;
            Type = type;
            Login = string.Empty;
            Email = string.Empty;
            PasswordHash = string.Empty;
            RefreshTokenHash = string.Empty;
            RefreshTokenExpiersAt = null;
        }

        // Main

        // for first admin
        public User(Ulid id, string firstName, string secondName, string login, string? passwordHash, Ulid companyId)
        {
            Id = id;
            CompanyId = companyId;
            FirstName = firstName;
            SecondName = secondName;
            Login = login;
            Type = UserType.Admin;
            Email = string.Empty;
            PasswordHash = passwordHash;
            RefreshTokenHash = string.Empty;
            RefreshTokenExpiersAt = null;
            CompanyId = companyId;
        }

        public User(Ulid id, UserType type, string firstName, string secondName, string login, string? passwordHash, Ulid companyId)
        {
            Id = id;
            Type = type;
            FirstName = firstName;
            SecondName = secondName;
            Login = login;
            PasswordHash = passwordHash;
            RefreshTokenHash = string.Empty;
            Email = string.Empty;
            RefreshTokenExpiersAt = null;
            CompanyId = companyId;
        }

    }
}