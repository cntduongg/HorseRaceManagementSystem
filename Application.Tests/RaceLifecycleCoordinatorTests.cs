using Application.Common;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;

namespace Application.Tests;

public class RaceLifecycleCoordinatorTests
{
    private static (ApplicationDbContext db, RaceLifecycleCoordinator sut, FakeTimeProvider clock) CreateSut(
        DateTimeOffset? now = null)
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;

        var db = new ApplicationDbContext(options);
        var clock = new FakeTimeProvider(now ?? DateTimeOffset.UtcNow);
        // Tracker thật (không có phụ thuộc gì) — chỉ gom raceId vào HashSet trong bộ nhớ.
        // Việc đẩy snapshot do RaceLiveBroadcastBehavior lo, nằm ngoài phạm vi test này.
        var sut = new RaceLifecycleCoordinator(db, clock, new RaceLiveChangeTracker());
        return (db, sut, clock);
    }

    private static async Task<Race> SeedReadyRaceAsync(
        ApplicationDbContext db,
        DateTime scheduledStart,
        bool closeRegistration = false,
        int approvedCount = 2,
        bool assignReferees = true)
    {
        var tournament = new Tournament
        {
            Name = "T1",
            StartDate = DateOnly.FromDateTime(scheduledStart.AddDays(-1)),
            EndDate = DateOnly.FromDateTime(scheduledStart.AddDays(1)),
            Status = "Open",
            CreatedAt = DateTime.UtcNow
        };
        db.Tournaments.Add(tournament);
        await db.SaveChangesAsync();

        var race = new Race
        {
            TournamentId = tournament.TournamentId,
            Name = "Auto Race",
            ScheduledStartTime = scheduledStart,
            ScheduledEndTime = scheduledStart.AddHours(1),
            NumberOfLegs = 2,
            MaxHorses = 8,
            RoundType = "Regular",
            Status = RaceExecutionConstants.RaceScheduled,
            Referee1Id = assignReferees ? 101 : null,
            Referee2Id = assignReferees ? 102 : null,
            CreatedAt = DateTime.UtcNow,
            OddsComputedAt = closeRegistration ? scheduledStart.AddHours(-1) : null,
            RegistrationCloseAt = closeRegistration ? scheduledStart.AddHours(-1) : null
        };
        db.Races.Add(race);
        await db.SaveChangesAsync();

        for (var i = 0; i < approvedCount; i++)
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
                Odds = closeRegistration ? 2.5m : 0m,
                GateNumber = closeRegistration ? i + 1 : null
            });
        }

        await db.SaveChangesAsync();
        return race;
    }

    [Fact]
    public async Task ManualStart_BeforeScheduledTime_StillAllowed_ToMatchFe()
    {
        var now = new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(db, now.UtcDateTime.AddMinutes(30), closeRegistration: true);

        // Endpoint thủ công không enforce schedule — FE vẫn bấm Start trước giờ.
        var result = await sut.StartRaceAsync(
            race.RaceId, enforceSchedule: false, allowAutoClose: true, throwOnFailure: true);

        Assert.Equal(RaceStartOutcome.Started, result.Outcome);
        Assert.Equal(RaceExecutionConstants.RaceInProgress, (await db.Races.FindAsync(race.RaceId))!.Status);
    }

    [Fact]
    public async Task StartRace_BeforeScheduledTime_Throws()
    {
        var now = new DateTimeOffset(2026, 7, 14, 10, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(db, now.UtcDateTime.AddMinutes(30), closeRegistration: true);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.StartRaceAsync(race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: true));

        Assert.Contains("ScheduledStartTime", ex.Message);
        Assert.Equal(RaceExecutionConstants.RaceScheduled, (await db.Races.FindAsync(race.RaceId))!.Status);
    }

    [Fact]
    public async Task StartRace_WhenDue_AutoClosesAndStarts_LocksPredictions()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(db, now.UtcDateTime.AddMinutes(-1), closeRegistration: false);

        db.Predictions.Add(new Prediction
        {
            RaceId = race.RaceId,
            SpectatorId = 1,
            FirstEntryId = 1,
            BetAmount = 20,
            OddsLocked1 = 2,
            Status = PredictionStatus.Pending,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        // Fix FirstEntryId to a real entry
        var entry = await db.Entries.FirstAsync(e => e.RaceId == race.RaceId);
        var prediction = await db.Predictions.FirstAsync();
        prediction.FirstEntryId = entry.EntryId;
        await db.SaveChangesAsync();

        var result = await sut.StartRaceAsync(
            race.RaceId,
            enforceSchedule: true,
            allowAutoClose: true,
            throwOnFailure: true);

        Assert.Equal(RaceStartOutcome.Started, result.Outcome);
        Assert.True(result.RegistrationWasClosed);
        Assert.Equal(RaceExecutionConstants.RaceInProgress, result.Status);

        var updated = await db.Races.Include(r => r.Legs).Include(r => r.Entries)
            .FirstAsync(r => r.RaceId == race.RaceId);
        Assert.Equal(RaceExecutionConstants.RaceInProgress, updated.Status);
        Assert.NotNull(updated.OddsComputedAt);
        Assert.Equal(2, updated.Legs.Count);
        Assert.All(updated.Entries.Where(e => e.Status == "Approved"), e => Assert.NotNull(e.GateNumber));
        Assert.Equal(PredictionStatus.Locked, (await db.Predictions.FirstAsync()).Status);
    }

    [Fact]
    public async Task StartRace_WhenAlreadyClosed_StartsWithoutReclosing()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(db, now.UtcDateTime.AddMinutes(-5), closeRegistration: true);
        var closedAt = race.OddsComputedAt;

        var result = await sut.StartRaceAsync(
            race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: true);

        Assert.Equal(RaceStartOutcome.Started, result.Outcome);
        Assert.False(result.RegistrationWasClosed);
        Assert.Equal(closedAt, (await db.Races.FindAsync(race.RaceId))!.OddsComputedAt);
    }

    [Fact]
    public async Task StartRace_MissingReferees_SkipsAndKeepsScheduled()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(
            db, now.UtcDateTime.AddMinutes(-1), closeRegistration: true, assignReferees: false);

        var result = await sut.StartRaceAsync(
            race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: false);

        Assert.Equal(RaceStartOutcome.Skipped, result.Outcome);
        Assert.Contains("referees", result.SkipReason, StringComparison.OrdinalIgnoreCase);
        var updated = await db.Races.FindAsync(race.RaceId);
        Assert.Equal(RaceExecutionConstants.RaceScheduled, updated!.Status);
        Assert.NotNull(updated.OddsComputedAt); // registration already closed; skip must not roll that back incorrectly from DB pre-seed
    }

    [Fact]
    public async Task StartRace_InsufficientApproved_SkipsWithoutMutating()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(
            db, now.UtcDateTime.AddMinutes(-1), closeRegistration: false, approvedCount: 1);

        var result = await sut.StartRaceAsync(
            race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: false);

        Assert.Equal(RaceStartOutcome.Skipped, result.Outcome);
        var updated = await db.Races.FindAsync(race.RaceId);
        Assert.Equal(RaceExecutionConstants.RaceScheduled, updated!.Status);
        Assert.Null(updated.OddsComputedAt);
    }

    [Fact]
    public async Task ProcessDueRaces_OneFailureDoesNotBlockOthers()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var (db, lifecycle, clock) = CreateSut(now);

        var good = await SeedReadyRaceAsync(db, now.UtcDateTime.AddMinutes(-2), closeRegistration: true);
        var bad = await SeedReadyRaceAsync(
            db, now.UtcDateTime.AddMinutes(-1), closeRegistration: true, approvedCount: 1);

        var handler = new ProcessDueRaceStartsCommandHandler(
            db, lifecycle, clock, NullLogger<ProcessDueRaceStartsCommandHandler>.Instance);

        var response = await handler.Handle(new ProcessDueRaceStartsCommand(), CancellationToken.None);

        Assert.Equal(2, response.Examined);
        Assert.Equal(1, response.Started);
        Assert.True(response.Skipped >= 1);
        Assert.Equal(RaceExecutionConstants.RaceInProgress, (await db.Races.FindAsync(good.RaceId))!.Status);
        Assert.Equal(RaceExecutionConstants.RaceScheduled, (await db.Races.FindAsync(bad.RaceId))!.Status);
    }

    [Fact]
    public async Task StartRace_SecondCall_IsIdempotentAlreadyStarted()
    {
        var now = new DateTimeOffset(2026, 7, 14, 12, 0, 0, TimeSpan.Zero);
        var (db, sut, _) = CreateSut(now);
        var race = await SeedReadyRaceAsync(db, now.UtcDateTime.AddMinutes(-1), closeRegistration: true);

        var first = await sut.StartRaceAsync(
            race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: false);
        var second = await sut.StartRaceAsync(
            race.RaceId, enforceSchedule: true, allowAutoClose: true, throwOnFailure: false);

        Assert.Equal(RaceStartOutcome.Started, first.Outcome);
        Assert.Equal(RaceStartOutcome.AlreadyStarted, second.Outcome);
        Assert.Equal(2, await db.Legs.CountAsync(l => l.RaceId == race.RaceId));
    }

    [Fact]
    public void LegPoints_ScaleWithFieldSize()
    {
        // 8 ngựa → nhất 8 điểm, chót 1 điểm (không còn ai bị 0 oan như thang cứng cũ).
        Assert.Equal(8, RaceExecutionConstants.LegPointsFor(1, RaceExecutionConstants.ResultFinished, 8));
        Assert.Equal(1, RaceExecutionConstants.LegPointsFor(8, RaceExecutionConstants.ResultFinished, 8));

        // 4 ngựa → nhất 4 điểm, chót 1 điểm.
        Assert.Equal(4, RaceExecutionConstants.LegPointsFor(1, RaceExecutionConstants.ResultFinished, 4));
        Assert.Equal(1, RaceExecutionConstants.LegPointsFor(4, RaceExecutionConstants.ResultFinished, 4));

        // DNF/DQ luôn 0; hạng vượt sĩ số coi như dữ liệu hỏng → 0.
        Assert.Equal(0, RaceExecutionConstants.LegPointsFor(null, RaceExecutionConstants.ResultDnf, 8));
        Assert.Equal(0, RaceExecutionConstants.LegPointsFor(null, RaceExecutionConstants.ResultDq, 8));
        Assert.Equal(0, RaceExecutionConstants.LegPointsFor(9, RaceExecutionConstants.ResultFinished, 8));
    }

    [Fact]
    public void PrizePoints_ScaleWithFieldSize_AnchoredToLegacyTop()
    {
        // Neo với thang cũ: race 5 ngựa → nhất vẫn đúng 1000 điểm.
        Assert.Equal(1000, RaceExecutionConstants.PrizePointsFor(1, 5));
        Assert.Equal(200, RaceExecutionConstants.PrizePointsFor(5, 5));
        Assert.Equal(1600, RaceExecutionConstants.PrizePointsFor(1, 8));
        Assert.Equal(0, RaceExecutionConstants.PrizePointsFor(9, 8));
        Assert.Equal(0, RaceExecutionConstants.PrizePointsFor(0, 8));
    }

    [Theory]
    // hợp lệ: đủ 1..N
    [InlineData(new[] { 1, 2, 3 }, 3, true)]
    // hợp lệ: có DNF/DQ, phần xếp hạng vẫn liên tục từ 1
    [InlineData(new[] { 1, 2, -1 }, 3, true)]
    [InlineData(new[] { 1, -2, -2 }, 3, true)]
    // vượt sĩ số
    [InlineData(new[] { 1, 2, 99 }, 3, false)]
    // trùng hạng
    [InlineData(new[] { 1, 1, 2 }, 3, false)]
    // đứt quãng (thiếu hạng 2)
    [InlineData(new[] { 1, 3, -1 }, 3, false)]
    // giá trị âm không hợp lệ
    [InlineData(new[] { 1, 2, -5 }, 3, false)]
    public void ValidatePositions_EnforcesRangeAndContiguity(int[] positions, int fieldSize, bool expectValid)
    {
        var error = RaceExecutionConstants.ValidatePositions(positions, fieldSize);
        Assert.Equal(expectValid, error is null);
    }

    [Fact]
    public void OddsFor_ClampsToExpectedRange()
    {
        Assert.InRange(CloseRegistrationCommandHandler.OddsFor(10, 10, 8), 1.1m, 25m);
        Assert.InRange(CloseRegistrationCommandHandler.OddsFor(0, 0, 8), 1.1m, 25m);
    }
}
