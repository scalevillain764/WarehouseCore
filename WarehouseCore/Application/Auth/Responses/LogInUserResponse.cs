namespace Application.Auth.Responses;

public sealed record LogInUserResponse(
    Ulid userId,
    string Login,
    string Password);