using Domain.Models;

namespace Application.Auth.Requests;
public sealed record RegistrateFirstAdminRequest(
    string FirstName,
    string SecondName,
    string Email,
    string Password);