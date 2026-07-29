using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Users.UpdateUser;

public sealed class UpdateUserCommandHandler
    : IRequestHandler<UpdateUserCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateUserCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("FullName is required.");

        if (request.RoleId <= 0)
            throw new InvalidOperationException("RoleId is invalid.");

        var roleCode = await _context.Roles
            .Where(x => x.RoleId == request.RoleId)
            .Select(x => x.Code)
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("RoleId is invalid.");

        var user = await _context.Users
            .FirstOrDefaultAsync(
                x => x.UserId == request.UserId,
                cancellationToken);

        if (user is null)
            return false;

        var licenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber)
            ? null
            : request.LicenseNumber.Trim();
        var bio = string.IsNullOrWhiteSpace(request.Bio)
            ? null
            : request.Bio.Trim();

        if (string.Equals(roleCode, "JOCKEY", StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(licenseNumber))
        {
            var profileLicenseExists = await _context.JockeyProfiles
                .AnyAsync(x =>
                    x.UserId != request.UserId &&
                    x.LicenseNumber == licenseNumber,
                    cancellationToken);

            if (profileLicenseExists)
                throw new InvalidOperationException("LicenseNumber already exists.");

            var userLicenseExists = await _context.Users
                .AnyAsync(x =>
                    x.UserId != request.UserId &&
                    x.LicenseNumber == licenseNumber,
                    cancellationToken);

            if (userLicenseExists)
                throw new InvalidOperationException("LicenseNumber already exists.");
        }

        user.Email = request.Email.Trim();
        user.FullName = request.FullName.Trim();
        user.PhoneNumber = request.PhoneNumber;
        user.AvatarUrl = request.AvatarUrl;
        user.RoleId = request.RoleId;
        user.IsActive = request.IsActive;
        user.LockedUntil = request.LockedUntil;
        user.LicenseNumber = licenseNumber;
        user.Weight = request.Weight;
        user.Bio = bio;
        user.IsProfileComplete = request.IsProfileComplete;
        user.UpdatedAt = DateTime.UtcNow;

        // JockeyProfile là nguồn dữ liệu chính của License/Weight/Bio trong Flow 2.
        // Admin sửa từ màn Users cũng phải cập nhật (hoặc tạo bù) profile, nếu không
        // trang hồ sơ và chức năng tìm/mời nài sẽ tiếp tục trả dữ liệu cũ hoặc ẩn nài.
        if (string.Equals(roleCode, "JOCKEY", StringComparison.OrdinalIgnoreCase))
        {
            var profile = await _context.JockeyProfiles
                .FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);

            if (profile is null)
            {
                _context.JockeyProfiles.Add(new JockeyProfile
                {
                    UserId = request.UserId,
                    LicenseNumber = licenseNumber,
                    Weight = request.Weight,
                    Bio = bio,
                    CreatedAt = DateTime.UtcNow
                });
            }
            else
            {
                profile.LicenseNumber = licenseNumber;
                profile.Weight = request.Weight;
                profile.Bio = bio;
                profile.UpdatedAt = DateTime.UtcNow;
            }
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}
