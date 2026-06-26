using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Auth.GetProfile;

// GET /api/auth/profile — hồ sơ user hiện tại (UserId resolve từ JWT ở controller).
public sealed record GetProfileQuery(int UserId) : IRequest<ProfileResponse>;

public sealed record ProfileResponse(
    int UserId,
    string Email,
    string FullName,
    string? PhoneNumber,
    string? AvatarUrl,
    string Role,
    string Status,
    bool IsActive,
    string? LicenseNumber,
    decimal? Weight,
    string? Bio,
    bool IsProfileComplete,
    DateTime CreatedAt);

public sealed class GetProfileQueryHandler
    : IRequestHandler<GetProfileQuery, ProfileResponse>
{
    private readonly IApplicationDbContext _context;

    public GetProfileQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileResponse> Handle(
        GetProfileQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        return new ProfileResponse(
            user.UserId,
            user.Email,
            user.FullName,
            user.PhoneNumber,
            user.AvatarUrl,
            user.Role?.Code ?? string.Empty,
            user.Status,
            user.IsActive,
            user.LicenseNumber,
            user.Weight,
            user.Bio,
            user.IsProfileComplete,
            user.CreatedAt);
    }
}
