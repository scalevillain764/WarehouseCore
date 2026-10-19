namespace Application.Auth.Requests;

public sealed record RegistrateFirstAdminRequest(string Email, string Password);