using Domain.Models;

namespace Application.Auth.Requests;

public record RegistrateUserRequest(
    string FirstName,
    string SecondName,
    string Password,
    UserType Type
    );