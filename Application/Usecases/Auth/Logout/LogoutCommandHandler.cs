using Application.Common;
using MediatR;

namespace Application.Usecases.Auth.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, bool>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;

    public LogoutCommandHandler(IRefreshTokenRepository refreshTokenRepository)
    {
        _refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<bool> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var refreshToken = await _refreshTokenRepository
            .GetActiveByTokenAsync(request.RefreshToken, cancellationToken);

        if (refreshToken is null)
        {
            return false;
        }

        _refreshTokenRepository.Revoke(refreshToken);

        return true;
    }
}
