using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Data;

/// <summary>
/// Tạo bù <see cref="JockeyProfile"/> cho các tài khoản JOCKEY chưa có, lấy License/Weight/Bio
/// từ chính bảng <c>Users</c> (Flow 2).
///
/// <para><b>Vì sao cần:</b> form đăng ký nài bắt nhập License Number + Weight và
/// <c>RegisterCommandHandler</c> lưu chúng vào <c>Users</c>, nhưng <b>mọi</b> luồng nghề nghiệp
/// lại đọc từ bảng <c>JockeyProfiles</c>: trang hồ sơ nài (<c>GET /api/jockey-profiles/{userId}</c>),
/// Owner tìm nài (<c>GET /api/jockeys/search</c>, <c>GET /api/jockey-profiles</c>) và điều kiện
/// mời nài. Trước đây đăng ký KHÔNG tạo row profile ⇒ nài nhập đủ thông tin lúc đăng ký vẫn thấy
/// form Professional Identity trống và Owner không tìm ra họ, phải vào profile gõ lại y hệt.
/// Đường đăng ký nay đã bootstrap sẵn, nhưng những tài khoản tạo TRƯỚC đó (và tài khoản Admin
/// tạo qua <c>POST /api/users</c> ở các bản cũ) vẫn mồ côi — backfill này dọn nốt.</para>
///
/// Idempotent: chạy mỗi lần khởi động, chỉ đụng vào user chưa có profile.
/// </summary>
public static class JockeyProfileBackfill
{
    private const string JockeyRoleCode = "JOCKEY";

    public static async Task RunAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var orphans = await db.Users
            .Where(u => u.Role != null && u.Role.Code == JockeyRoleCode)
            .Where(u => !db.JockeyProfiles.Any(p => p.UserId == u.UserId))
            .Select(u => new
            {
                u.UserId,
                u.LicenseNumber,
                u.Weight,
                u.Bio
            })
            .ToListAsync(cancellationToken);

        if (orphans.Count == 0)
            return;

        // License trùng sẽ vỡ ràng buộc nghiệp vụ ở CreateJockeyProfile/UpdateJockeyProfile,
        // nên chỉ chép sang những số chưa ai dùng; phần còn lại vẫn tạo profile (rỗng) để
        // trang hồ sơ mở được, nài tự sửa lại.
        var usedLicenses = await db.JockeyProfiles
            .Where(p => p.LicenseNumber != null && p.LicenseNumber != "")
            .Select(p => p.LicenseNumber!)
            .ToListAsync(cancellationToken);

        var taken = new HashSet<string>(usedLicenses, StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        var withDetails = 0;
        var skippedLicenses = new List<int>();

        foreach (var user in orphans)
        {
            var license = user.LicenseNumber?.Trim();

            if (!string.IsNullOrWhiteSpace(license) && !taken.Add(license))
            {
                skippedLicenses.Add(user.UserId);
                license = null;
            }

            db.JockeyProfiles.Add(new JockeyProfile
            {
                UserId = user.UserId,
                LicenseNumber = string.IsNullOrWhiteSpace(license) ? null : license,
                Weight = user.Weight,
                Bio = user.Bio,
                CreatedAt = now
            });

            if (!string.IsNullOrWhiteSpace(license) && user.Weight is > 0)
                withDetails++;
        }

        await db.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Jockey profile backfill complete: created={Created}, withLicenseAndWeight={WithDetails}, duplicateLicensesSkipped={Skipped}",
            orphans.Count,
            withDetails,
            skippedLicenses.Count);

        if (skippedLicenses.Count > 0)
        {
            logger.LogWarning(
                "Jockey profile backfill left license blank for UserIds {UserIds} — the number is already used by another profile.",
                string.Join(", ", skippedLicenses));
        }
    }
}
