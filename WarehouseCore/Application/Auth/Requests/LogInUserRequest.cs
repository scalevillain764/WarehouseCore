namespace Application.Auth.Requests;

public record LogInUserRequest(
    string CompanyCode,
    string Login,
    string Password
    );