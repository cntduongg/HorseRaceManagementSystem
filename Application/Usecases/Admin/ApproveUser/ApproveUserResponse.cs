namespace Application.Usecases.Admin.ApproveUser;

public sealed record ApproveUserResponse(int UserId, string Email, string FullName, string Status);
