using Application.Common.Interfaces;
using Application.Usecases.Auth.GetProfile;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Auth.UpdateProfile;

// PUT /api/auth/profile — user tự sửa hồ sơ của chính mình (UserId resolve từ JWT ở controller).
// Khác PUT /api/users/{id} (ADMIN-only, full-replace cả RoleId/IsActive): endpoint này chỉ cho
// đổi các trường "của mình" (FullName, PhoneNumber) → không thể tự nâng quyền hay tự mở khóa.
public sealed record UpdateProfileCommand(
    int UserId,
    string FullName,
    string? PhoneNumber) : IRequest<ProfileResponse>;

public sealed class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommand, ProfileResponse>
{
    private readonly IApplicationDbContext _context;

    public UpdateProfileCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProfileResponse> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("Full name is required.");

        // Giới hạn khớp schema: User.FullName HasMaxLength(150). PhoneNumber là text không giới hạn
        // ở DB → chặn ở đây cho gọn input.
        var fullName = request.FullName.Trim();
        if (fullName.Length > 150)
            throw new InvalidOperationException("Full name must be at most 150 characters.");

        var phoneNumber = string.IsNullOrWhiteSpace(request.PhoneNumber)
            ? null
            : request.PhoneNumber.Trim();
        if (phoneNumber is { Length: > 30 })
            throw new InvalidOperationException("Phone number must be at most 30 characters.");

        var user = await _context.Users
            .Include(u => u.Role)
            .FirstOrDefaultAsync(u => u.UserId == request.UserId, cancellationToken)
            ?? throw new KeyNotFoundException("User not found.");

        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

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
