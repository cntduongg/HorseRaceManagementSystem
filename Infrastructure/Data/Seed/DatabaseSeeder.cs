using Application.Common;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data.Seed;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        CancellationToken cancellationToken = default)
    {
        await SeedRolesAsync(db, cancellationToken);

        var adminRole = await GetRoleAsync(db, "ADMIN", cancellationToken);
        var refereeRole = await GetRoleAsync(db, "REFEREE", cancellationToken);
        var ownerRole = await GetRoleAsync(db, "HORSE_OWNER", cancellationToken);
        var jockeyRole = await GetRoleAsync(db, "JOCKEY", cancellationToken);
        var spectatorRole = await GetRoleAsync(db, "SPECTATOR", cancellationToken);

        await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "admin@hrs.com",
            password: "Admin@123",
            fullName: "HRS Admin",
            roleId: adminRole.RoleId,
            status: "Active",
            isActive: true,
            cancellationToken: cancellationToken);

        await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "ref1@hrs.com",
            password: "Ref@123",
            fullName: "Referee One",
            roleId: refereeRole.RoleId,
            status: "Active",
            isActive: true,
            cancellationToken: cancellationToken);

        await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "ref2@hrs.com",
            password: "Ref@123",
            fullName: "Referee Two",
            roleId: refereeRole.RoleId,
            status: "Active",
            isActive: true,
            cancellationToken: cancellationToken);

        await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "owner@hrs.com",
            password: "Owner@123",
            fullName: "Horse Owner Test",
            roleId: ownerRole.RoleId,
            status: "Active",
            isActive: true,
            cancellationToken: cancellationToken);

        var jockey = await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "jockey@hrs.com",
            password: "Jockey@123",
            fullName: "Jockey Test",
            roleId: jockeyRole.RoleId,
            status: "Active",
            isActive: true,
            licenseNumber: "JOCKEY-TEST-001",
            weight: 55,
            bio: "Seeded jockey account for development testing.",
            cancellationToken: cancellationToken);

        await EnsureJockeyProfileAsync(db, jockey.UserId, cancellationToken);

        var spectator = await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "spectator@hrs.com",
            password: "Spectator@123",
            fullName: "Spectator Test",
            roleId: spectatorRole.RoleId,
            status: "Active",
            isActive: true,
            cancellationToken: cancellationToken);

        await EnsureSpectatorAndWalletAsync(db, spectator.UserId, cancellationToken);

        await AddOrResetUserAsync(
            db,
            passwordHasher,
            email: "pending.referee@hrs.com",
            password: "Pending@123",
            fullName: "Pending Referee Test",
            roleId: refereeRole.RoleId,
            status: "Pending",
            isActive: false,
            cancellationToken: cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedRolesAsync(
        ApplicationDbContext db,
        CancellationToken cancellationToken)
    {
        var roles = new[]
        {
            new Role { Code = "ADMIN", Name = "Administrator" },
            new Role { Code = "REFEREE", Name = "Race Referee" },
            new Role { Code = "HORSE_OWNER", Name = "Horse Owner" },
            new Role { Code = "JOCKEY", Name = "Jockey" },
            new Role { Code = "SPECTATOR", Name = "Spectator" }
        };

        foreach (var role in roles)
        {
            var existing = await db.Roles.FirstOrDefaultAsync(
                x => x.Code == role.Code,
                cancellationToken);

            if (existing is null)
            {
                db.Roles.Add(role);
            }
            else
            {
                existing.Name = role.Name;
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Role> GetRoleAsync(
        ApplicationDbContext db,
        string code,
        CancellationToken cancellationToken)
    {
        return await db.Roles.FirstAsync(
            x => x.Code == code,
            cancellationToken);
    }

    private static async Task<User> AddOrResetUserAsync(
        ApplicationDbContext db,
        IPasswordHasher passwordHasher,
        string email,
        string password,
        string fullName,
        int roleId,
        string status,
        bool isActive,
        CancellationToken cancellationToken,
        string? licenseNumber = null,
        decimal? weight = null,
        string? bio = null)
    {
        var user = await db.Users.FirstOrDefaultAsync(
            x => x.Email == email,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Email = email,
                CreatedAt = DateTime.UtcNow
            };

            db.Users.Add(user);
        }

        user.PasswordHash = passwordHasher.Hash(password);
        user.FullName = fullName;
        user.RoleId = roleId;
        user.Status = status;
        user.IsActive = isActive;
        user.IsProfileComplete = true;
        user.LicenseNumber = licenseNumber;
        user.Weight = weight;
        user.Bio = bio;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return user;
    }

    private static async Task EnsureJockeyProfileAsync(
        ApplicationDbContext db,
        int userId,
        CancellationToken cancellationToken)
    {
        var exists = await db.JockeyProfiles.AnyAsync(
            x => x.UserId == userId,
            cancellationToken);

        if (exists)
        {
            return;
        }

        db.JockeyProfiles.Add(new JockeyProfile
        {
            UserId = userId,
            LicenseNumber = "JOCKEY-TEST-001",
            Weight = 55,
            Bio = "Seeded jockey profile for development testing.",
            TotalRaces = 0,
            TotalWins = 0,
            TotalTop3 = 0,
            CareerPrizePoints = 0,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureSpectatorAndWalletAsync(
        ApplicationDbContext db,
        int userId,
        CancellationToken cancellationToken)
    {
        var spectator = await db.Spectators.FirstOrDefaultAsync(
            x => x.UserId == userId,
            cancellationToken);

        if (spectator is null)
        {
            spectator = new Spectator
            {
                UserId = userId,
                RegisteredAt = DateTime.UtcNow,
                IsActive = true
            };

            db.Spectators.Add(spectator);
            await db.SaveChangesAsync(cancellationToken);
        }

        var walletExists = await db.PointWallets.AnyAsync(
            x => x.SpectatorId == userId,
            cancellationToken);

        if (walletExists)
        {
            return;
        }

        db.PointWallets.Add(new PointWallet
        {
            SpectatorId = userId,
            Balance = 100,
            IsFrozen = false,
            CreatedAt = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
    }
}