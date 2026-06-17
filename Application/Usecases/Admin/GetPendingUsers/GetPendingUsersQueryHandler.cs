using Application.Common;
using MediatR;

namespace Application.Usecases.Admin.GetPendingUsers;

public sealed class GetPendingUsersQueryHandler : IRequestHandler<GetPendingUsersQuery, List<PendingUserResponse>>
{
    private readonly IUserRepository _userRepository;

    public GetPendingUsersQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<List<PendingUserResponse>> Handle(
        GetPendingUsersQuery request,
        CancellationToken cancellationToken)
    {
        var users = await _userRepository.GetPendingUsersAsync(cancellationToken);

        return users.Select(u => new PendingUserResponse(
            UserId:        u.UserId,
            Email:         u.Email,
            FullName:      u.FullName,
            PhoneNumber:   u.PhoneNumber,
            RoleCode:      u.Role.Code,
            RoleName:      u.Role.Name,
            LicenseNumber: u.LicenseNumber,
            Weight:        u.Weight,
            Bio:           u.Bio,
            CreatedAt:     u.CreatedAt
        )).ToList();
    }
}
