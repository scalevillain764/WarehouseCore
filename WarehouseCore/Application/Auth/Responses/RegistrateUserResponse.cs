namespace Application.Auth.Responses;

public sealed record RegistrateUserResponse(Ulid userId, string login);