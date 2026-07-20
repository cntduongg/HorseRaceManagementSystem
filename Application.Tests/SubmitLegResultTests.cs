using Application.Common;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Application.Tests;

// Tái hiện luồng Blind Double-Entry: 2 trọng tài submit từng leg → khớp → Confirmed →
// hết leg → race PendingResult (điều kiện để Admin publish kết quả cho Spectator xem).
public class SubmitLegResultTests
{
    private const int Referee1 = 101;
    private const int Referee2 = 102;

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    // Seed race đã đóng đăng ký rồi start qua coordinator → có Legs + race InProgress.
    private static async Task<Race> SeedStartedRaceAsync(ApplicationDbContext db, int numberOfLegs = 2)
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);

        var tournament = new Tournament
        {
            Name = "T1",
            StartDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(now.UtcDateTime.AddDays(1)),
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var scheduledStart = now.UtcDateTime.AddMinutes(-1);
        var race = new Race
        {
            TournamentId = tournament.TournamentId,
            Name = "Blind Race",
            ScheduledStartTime = scheduledStart,
            ScheduledEndTime = scheduledStart.AddHours(1),
            NumberOfLegs = numberOfLegs,
            MaxHorses = 8,
            RoundType = "Regular",
            Status = RaceExecutionConstants.RaceScheduled,
            Referee1Id = Referee1,
            Referee2Id = Referee2,
            CreatedAt = DateTime.UtcNow,
            OddsComputedAt = scheduledStart.AddHours(-1),
            RegistrationCloseAt = scheduledStart.AddHours(-1)
        };
        db.Races.Add(race);
        await db.SaveChangesAsync();

        for (var i = 0; i < 3; i++)
        {
            db.Entries.Add(new Entry
            {
                RaceId = race.RaceId,
                HorseId = 1000 + i,
                JockeyId = 2000 + i,
                HorseOwnerId = 3000,
                Status = RaceExecutionConstants.EntryApproved,
                SubmittedAt = DateTime.UtcNow.AddMinutes(-i),
                CreatedAt = DateTime.UtcNow,
                Odds = 2.5m,
                GateNumber = i + 1
            });
        }
        await db.SaveChangesAsync();

        var coordinator = new RaceLifecycleCoordinator(db, new FakeTimeProvider(now), new RaceLiveChangeTracker());
        await coordinator.StartRaceAsync(race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: true);

        return race;
    }

    private static SubmitLegResultCommandHandler CreateHandler(ApplicationDbContext db)
        => new(db, new RaceLiveChangeTracker());

    // Thứ hạng 1..n theo thứ tự entryId — cả hai trọng tài dùng chung để chắc chắn KHỚP.
    private static async Task<List<SubmitPositionItem>> BuildPositionsAsync(ApplicationDbContext db, int raceId)
    {
        var entryIds = await db.Entries
            .Where(e => e.RaceId == raceId && e.Status == RaceExecutionConstants.EntryApproved)
            .OrderBy(e => e.EntryId)
            .Select(e => e.EntryId)
            .ToListAsync();

        return entryIds.Select((id, i) => new SubmitPositionItem(id, i + 1)).ToList();
    }

    [Fact]
    public async Task BothReferees_SubmitMatchingResults_LegIsConfirmed()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 2);
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        // Trọng tài 1 submit leg 1 (legIndex 0) → chờ trọng tài 2.
        var first = await handler.Handle(
            new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None);

        Assert.Equal(RaceExecutionConstants.LegAwaitingSecondReferee, first.Status);
        Assert.False(first.IsRaceComplete);

        // Trọng tài 2 submit CÙNG kết quả → phải khớp và chốt leg.
        var second = await handler.Handle(
            new SubmitLegResultCommand(race.RaceId, 0, Referee2, positions), CancellationToken.None);

        Assert.Equal("Matched", second.Status);

        var leg1 = await db.Legs.FirstAsync(l => l.RaceId == race.RaceId && l.LegNumber == 1);
        Assert.Equal(RaceExecutionConstants.LegConfirmed, leg1.Status);
        Assert.Equal(RaceExecutionConstants.AutoMatched, leg1.ConfirmationType);
        Assert.Equal("Completed", leg1.ExecutionStatus);

        // Kết quả chính thức phải xuất hiện → Spectator mới xem được vị trí.
        var officials = await db.LegOfficialResults
            .Where(o => o.RaceId == race.RaceId && o.LegNumber == 1).ToListAsync();
        Assert.Equal(positions.Count, officials.Count);
    }

    [Fact]
    public async Task AllLegsConfirmed_RaceBecomesPendingResult_SoAdminCanPublish()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 2);
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        // Leg 1
        await handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None);
        var leg1Done = await handler.Handle(
            new SubmitLegResultCommand(race.RaceId, 0, Referee2, positions), CancellationToken.None);
        Assert.Equal("Matched", leg1Done.Status);
        Assert.False(leg1Done.IsRaceComplete);
        Assert.Equal(1, leg1Done.NextLegIndex); // còn leg 2 (index 1)

        // Leg 2 (legIndex 1)
        await handler.Handle(new SubmitLegResultCommand(race.RaceId, 1, Referee1, positions), CancellationToken.None);
        var leg2Done = await handler.Handle(
            new SubmitLegResultCommand(race.RaceId, 1, Referee2, positions), CancellationToken.None);

        Assert.Equal("Matched", leg2Done.Status);
        Assert.True(leg2Done.IsRaceComplete);

        var updated = await db.Races.FirstAsync(r => r.RaceId == race.RaceId);
        Assert.Equal(RaceExecutionConstants.RacePendingResult, updated.Status);
    }

    [Fact]
    public async Task RefereesDisagree_LegConflicted_AndRacePaused()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 2);
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        // Trọng tài 2 đảo vị trí 1 và 2 → lệch.
        var swapped = positions
            .Select(p => p.EntryId == positions[0].EntryId ? p with { Position = 2 }
                       : p.EntryId == positions[1].EntryId ? p with { Position = 1 }
                       : p)
            .ToList();

        await handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None);
        var conflict = await handler.Handle(
            new SubmitLegResultCommand(race.RaceId, 0, Referee2, swapped), CancellationToken.None);

        Assert.Equal("Conflicted", conflict.Status);
        var leg1 = await db.Legs.FirstAsync(l => l.RaceId == race.RaceId && l.LegNumber == 1);
        Assert.Equal(RaceExecutionConstants.LegConflicted, leg1.Status);
        Assert.Equal(RaceExecutionConstants.RacePaused,
            (await db.Races.FirstAsync(r => r.RaceId == race.RaceId)).Status);
    }

    [Fact]
    public async Task ConfirmedLeg_AwardsPointsScaledToFieldSize()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 1); // 3 entry được seed
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        await handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None);
        await handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee2, positions), CancellationToken.None);

        var officials = await db.LegOfficialResults
            .Where(o => o.RaceId == race.RaceId && o.LegNumber == 1)
            .OrderBy(o => o.FinishPosition)
            .ToListAsync();

        // 3 ngựa → 1st=3, 2nd=2, 3rd=1 (trước đây thang cứng cho 6/5/4).
        Assert.Equal(new[] { 3, 2, 1 }, officials.Select(o => o.LegPoints).ToArray());
    }

    [Fact]
    public async Task Position_OutOfFieldRange_IsRejected()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 1);
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        // 3 ngựa nhưng gửi hạng 99 → BE phải chặn (trước đây lọt).
        var bad = positions.Select((p, i) => i == 2 ? p with { Position = 99 } : p).ToList();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, bad), CancellationToken.None));

        Assert.Contains("out of range", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Position_WithGaps_IsRejected()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 1);
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        // Hạng 1,2,3 → đổi thành 1,3,3 sẽ trùng; dùng 1,3 + DNF để tạo lỗ hổng hạng 2.
        var gapped = new List<SubmitPositionItem>
        {
            positions[0] with { Position = 1 },
            positions[1] with { Position = 3 },
            positions[2] with { Position = -1 },
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, gapped), CancellationToken.None));

        Assert.Contains("consecutively", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task SameReferee_CannotSubmitTwice()
    {
        var db = CreateDb();
        var race = await SeedStartedRaceAsync(db, numberOfLegs: 2);
        var handler = CreateHandler(db);
        var positions = await BuildPositionsAsync(db, race.RaceId);

        await handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None));

        Assert.Contains("already submitted", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
