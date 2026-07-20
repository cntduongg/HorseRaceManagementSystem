using Application.Usecases.Admin.GetAdminViolations;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Tests;

// GET /api/admin/violations — FE gửi search/sort/sortDirection; trước đây BE bỏ qua nên ô tìm kiếm
// của Admin không có tác dụng. Các test này khoá hành vi filter + sort.
public class GetAdminViolationsTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options);
    }

    // Seed 2 vi phạm trên 2 race/nài/ngựa khác nhau để search phân biệt được.
    private static async Task SeedAsync(ApplicationDbContext db)
    {
        var role = new Role { Code = "JOCKEY", Name = "Jockey" };
        db.Roles.Add(role);
        await db.SaveChangesAsync();

        var tournament = new Tournament
        {
            Name = "T1",
            StartDate = DateOnly.FromDateTime(DateTime.UtcNow),
            EndDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(1)),
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        async Task<Violation> AddAsync(
            string raceName, string jockeyName, string horseName,
            string violationType, string? description, DateTime createdAt)
        {
            var race = new Race
            {
                TournamentId = tournament.TournamentId,
                Name = raceName,
                ScheduledStartTime = DateTime.UtcNow,
                ScheduledEndTime = DateTime.UtcNow.AddHours(1),
                NumberOfLegs = 1,
                MaxHorses = 8,
                RoundType = "Regular",
                Status = "PendingResult",
                CreatedAt = DateTime.UtcNow
            };
            db.Races.Add(race);

            var jockey = new User
            {
                FullName = jockeyName,
                Email = $"{Guid.NewGuid():N}@t.com",
                PasswordHash = "x",
                RoleId = role.RoleId,
                Status = "Active",
                CreatedAt = DateTime.UtcNow
            };
            var horse = new Horse
            {
                Name = horseName,
                Breed = "Arabian",
                Color = "Bay",
                BirthYear = 2020,
                OwnerId = 3000,
                Status = "Approved",
                CreatedAt = DateTime.UtcNow
            };
            db.Users.Add(jockey);
            db.Horses.Add(horse);
            await db.SaveChangesAsync();

            var entry = new Entry
            {
                RaceId = race.RaceId,
                HorseId = horse.HorseId,
                JockeyId = jockey.UserId,
                HorseOwnerId = 3000,
                Status = "Approved",
                SubmittedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            db.Entries.Add(entry);
            await db.SaveChangesAsync();

            var violation = new Violation
            {
                RaceId = race.RaceId,
                LegNumber = 1,
                EntryId = entry.EntryId,
                ReportedByRefereeId = 101,
                ViolationType = violationType,
                Description = description,
                Penalty = "Warning",
                Status = "Pending",
                CreatedAt = createdAt
            };
            db.Violations.Add(violation);
            await db.SaveChangesAsync();
            return violation;
        }

        await AddAsync("Golden Cup", "Alice Nguyen", "Thunderbolt",
            "Interference", "Cut across a rival", new DateTime(2026, 7, 1, 0, 0, 0, DateTimeKind.Utc));
        await AddAsync("Silver Dash", "Bob Tran", "Lightning",
            "Whip Abuse", "Excessive whip use", new DateTime(2026, 7, 5, 0, 0, 0, DateTimeKind.Utc));
    }

    private static GetAdminViolationsQueryHandler CreateSut(ApplicationDbContext db) => new(db);

    [Fact]
    public async Task Search_MatchesViolationType()
    {
        var db = CreateDb();
        await SeedAsync(db);

        var result = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Search: "whip"), CancellationToken.None);

        Assert.Equal(1, result.Total);
        Assert.Equal("Whip Abuse", Assert.Single(result.Items).ViolationType);
    }

    [Fact]
    public async Task Search_MatchesRaceName_CaseInsensitive()
    {
        var db = CreateDb();
        await SeedAsync(db);

        var result = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Search: "GOLDEN"), CancellationToken.None);

        Assert.Equal("Golden Cup", Assert.Single(result.Items).RaceName);
    }

    [Fact]
    public async Task Search_MatchesJockeyOrHorseName()
    {
        var db = CreateDb();
        await SeedAsync(db);

        var byJockey = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Search: "alice"), CancellationToken.None);
        Assert.Contains("Alice Nguyen", Assert.Single(byJockey.Items).ViolatorName);

        var byHorse = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Search: "lightning"), CancellationToken.None);
        Assert.Contains("Lightning", Assert.Single(byHorse.Items).ViolatorName);
    }

    [Fact]
    public async Task Search_NoMatch_ReturnsEmpty_ButKeepsGlobalCounts()
    {
        var db = CreateDb();
        await SeedAsync(db);

        var result = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Search: "zzzz-nope"), CancellationToken.None);

        Assert.Equal(0, result.Total);
        Assert.Empty(result.Items);
        // Badge đếm tổng vẫn là toàn cục (không phụ thuộc search).
        Assert.Equal(2, result.PendingCount);
    }

    [Fact]
    public async Task Sort_CreatedAtAscending_IsRespected()
    {
        var db = CreateDb();
        await SeedAsync(db);

        var asc = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Sort: "createdAt", SortDirection: "asc"),
            CancellationToken.None);
        Assert.Equal("Golden Cup", asc.Items[0].RaceName);

        var desc = await CreateSut(db).Handle(
            new GetAdminViolationsQuery(null, 1, 15, Sort: "createdAt", SortDirection: "desc"),
            CancellationToken.None);
        Assert.Equal("Silver Dash", desc.Items[0].RaceName);
    }

    [Fact]
    public async Task StatusFilter_StillWorks_AlongsideSearch()
    {
        var db = CreateDb();
        await SeedAsync(db);

        // Cả 2 đều Pending → lọc Resolved phải rỗng.
        var resolved = await CreateSut(db).Handle(
            new GetAdminViolationsQuery("Resolved", 1, 15), CancellationToken.None);
        Assert.Equal(0, resolved.Total);

        var pending = await CreateSut(db).Handle(
            new GetAdminViolationsQuery("Pending", 1, 15), CancellationToken.None);
        Assert.Equal(2, pending.Total);
        Assert.All(pending.Items, i => Assert.Equal("Pending", i.Status));
    }
}
