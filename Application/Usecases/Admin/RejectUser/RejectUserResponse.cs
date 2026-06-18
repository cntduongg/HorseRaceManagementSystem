namespace Application.Usecases.Admin.RejectUser;

public sealed record RejectUserResponse(int UserId, string Email, string FullName, string Status);
