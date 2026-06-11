namespace Application.Usecases.Auth.Login;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    int ExpiresIn,
    int UserId,
    string Email,
    string FullName,
    string Role
);
