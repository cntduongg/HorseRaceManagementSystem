using Application.Common;

namespace Application.Usecases.Admin.GetPendingUsers;

public sealed record GetPendingUsersQuery : IQuery<List<PendingUserResponse>>;
