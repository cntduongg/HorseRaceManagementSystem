using Application.Common;
using MediatR;

namespace Application.Usecases.Auth.GetProfile;

public sealed class GetProfileQueryHandler
    : IRequestHandler<GetProfileQuery, ProfileResponse?>
{
    private readonly IUserRepository _userRepository;

    public GetProfileQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<ProfileResponse?> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new ProfileResponse(
            user.UserId,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.Role.Code,
            user.Status,
            user.IsActive,
            user.LicenseNumber,
            user.Weight,
            user.Bio,
            user.IsProfileComplete);
    }
}
