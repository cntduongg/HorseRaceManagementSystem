using Application.Common;
using MediatR;
using RefreshTokenEntity = Domain.Aggregates.Entities.RefreshToken;

namespace Application.Usecases.Auth.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtTokenService _jwtTokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokenRepository,
        IJwtTokenService jwtTokenService)
    {
        _refreshTokenRepository = refreshTokenRepository;
        _jwtTokenService = jwtTokenService;
    }

    public async Task<RefreshTokenResponse> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var existingToken = await _refreshTokenRepository
            .GetActiveByTokenAsync(request.RefreshToken, cancellationToken);

        if (existingToken is null)
        {
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");
        }

        // Rotate: revoke the old token
        _refreshTokenRepository.Revoke(existingToken);

        // Issue new tokens
        var newAccessToken = _jwtTokenService.GenerateAccessToken(existingToken.User);
        var newRefreshTokenValue = _jwtTokenService.GenerateRefreshToken();

        var newRefreshToken = new RefreshTokenEntity
        {
            Id = Guid.NewGuid(),
            UserId = existingToken.UserId,
            Token = newRefreshTokenValue,
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtTokenService.RefreshTokenExpirationDays),
            CreatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(newRefreshToken, cancellationToken);

        return new RefreshTokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshTokenValue,
            ExpiresIn: _jwtTokenService.AccessTokenExpirationSeconds
        );
    }
}
