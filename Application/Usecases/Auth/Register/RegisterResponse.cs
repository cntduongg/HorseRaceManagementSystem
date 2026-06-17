namespace Application.Usecases.Auth.Register;

public sealed record RegisterResponse(
    int UserId,
    string Email,
    string FullName,
    string Status,
    bool RequiresApproval
);
