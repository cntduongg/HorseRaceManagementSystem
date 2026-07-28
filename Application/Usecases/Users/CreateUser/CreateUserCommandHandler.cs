using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Usecases.Users.CreateUser;

public sealed class CreateUserCommandHandler
    : IRequestHandler<CreateUserCommand, int>
{
    private const string SpectatorRoleCode = "SPECTATOR";
    private const string JockeyRoleCode = "JOCKEY";

    private readonly IApplicationDbContext _context;
    private readonly IPasswordHasher _passwordHasher;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        IPasswordHasher passwordHasher)
    {
        _context = context;
        _passwordHasher = passwordHasher;
    }

    public async Task<int> Handle(
        CreateUserCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            throw new InvalidOperationException("Email is required.");

        if (string.IsNullOrWhiteSpace(request.Password))
            throw new InvalidOperationException("Password is required.");

        if (string.IsNullOrWhiteSpace(request.FullName))
            throw new InvalidOperationException("FullName is required.");

        if (request.RoleId <= 0)
            throw new InvalidOperationException("RoleId is invalid.");

        // Spectator PHẢI đi qua /register: RegisterCommandHandler bootstrap kèm Spectator row +
        // PointWallet 100 điểm + WalletTransaction "Initial". Handler này chỉ Add mỗi User, nên
        // Admin tạo Spectator ở đây sẽ ra tài khoản KHÔNG có ví — đăng nhập được nhưng mọi thao tác
        // Flow 7 (xem số dư, đặt cược) đều vỡ. Tra Role theo Code thay vì hardcode id vì mapping
        // RoleId ↔ Code phụ thuộc thứ tự seed.
        var role = await _context.Roles
            .FirstOrDefaultAsync(r => r.RoleId == request.RoleId, cancellationToken)
            ?? throw new InvalidOperationException("RoleId is invalid.");

        if (string.Equals(role.Code, SpectatorRoleCode, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                "Spectator accounts must be created via self-registration (/register), not by Admin.");

        var exists = await _context.Users
    .AnyAsync(x => x.Email == request.Email, cancellationToken);
        if (!string.IsNullOrWhiteSpace(request.LicenseNumber))
        {
            var licenseExists = await _context.Users
                .AnyAsync(x => x.LicenseNumber == request.LicenseNumber, cancellationToken);

            if (licenseExists)
                throw new InvalidOperationException("LicenseNumber already exists.");
        }
        if (exists)
            throw new InvalidOperationException("Email already exists.");
        var user = new User
        {
            Email = request.Email.Trim(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            FullName = request.FullName.Trim(),
            PhoneNumber = request.PhoneNumber,
            AvatarUrl = request.AvatarUrl,
            RoleId = request.RoleId,
            LicenseNumber = request.LicenseNumber,
            Weight = request.Weight,
            Bio = request.Bio,
            IsActive = true,
            IsProfileComplete = false,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);

        await _context.SaveChangesAsync(cancellationToken);

        // Cùng lý do như Spectator phải có ví: nài không có JockeyProfile là nài "tàng hình".
        // License/Weight ở bảng Users không được luồng nào của Flow 2 đọc — trang hồ sơ nài,
        // Owner tìm nài và điều kiện mời nài đều truy vấn JockeyProfiles.
        if (string.Equals(role.Code, JockeyRoleCode, StringComparison.OrdinalIgnoreCase))
        {
            _context.JockeyProfiles.Add(new JockeyProfile
            {
                UserId = user.UserId,
                LicenseNumber = string.IsNullOrWhiteSpace(request.LicenseNumber)
                    ? null
                    : request.LicenseNumber.Trim(),
                Weight = request.Weight,
                Bio = request.Bio,
                CreatedAt = DateTime.UtcNow
            });

            await _context.SaveChangesAsync(cancellationToken);
        }

        return user.UserId;
    }
}