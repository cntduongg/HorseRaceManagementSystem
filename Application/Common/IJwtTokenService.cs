using Domain.Aggregates.Entities;

namespace Application.Common;

public interface IJwtTokenService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    int AccessTokenExpirationSeconds { get; }
    int RefreshTokenExpirationDays { get; }
}
