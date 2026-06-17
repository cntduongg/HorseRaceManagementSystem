using Application.Common;

namespace Application.Usecases.Admin.RejectUser;

public sealed record RejectUserCommand(int UserId, string? Reason) : ICommand<RejectUserResponse>;
