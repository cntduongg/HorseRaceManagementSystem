using Application.Common;

namespace Application.Usecases.Admin.ApproveUser;

public sealed record ApproveUserCommand(int UserId) : ICommand<ApproveUserResponse>;
