using Application.Common;

namespace Application.Usecases.Admin.RejectUser;

public sealed record RejectUserCommand(
    int UserId,
    int AdminId,
    string Reason
) : ICommand<RejectUserResponse>;