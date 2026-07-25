using Application.Common;
using Application.Usecases.RaceExecution;
using Application.Usecases.Violations.CreateViolation;
using Application.Usecases.Violations.RejectViolation;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;

namespace Application.Tests;

// Nối chuỗi Flow 4 → 6 → 8: cả 2 trọng tài submit hết leg → race PendingResult →
// Admin publish để Spectator xem kết quả. Vi phạm CÒN Pending sẽ CHẶN publish (cố ý).
public class PublishBlockedByViolationTests
{
    private const int Referee1 = 101;
    private const int Referee2 = 102;
    private const int AdminId = 1;

    // Repo giả — PublishRaceResult chỉ ghi audit, không ảnh hưởng nghiệp vụ đang test.
    private sealed class FakeReviewHistoryRepository : IReviewHistoryRepository
    {
        public List<ReviewHistory> Saved { get; } = new();

        public Task AddAsync(ReviewHistory history, CancellationToken cancellationToken = default)
        {
            Saved.Add(history);
            return Task.CompletedTask;
        }

        public Task<List<ReviewHistory>> GetAsync(
            ReviewEntity? entity, int? entityId, CancellationToken cancellationToken = default)
            => Task.FromResult(new List<ReviewHistory>());
    }

    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .ConfigureWarnings(w => w.Ignore(
                Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        return new ApplicationDbContext(options);
    }

    // Race 1 leg, 2 trọng tài, 3 entry — chạy tới trạng thái PendingResult.
    private static async Task<Race> SeedRaceAtPendingResultAsync(ApplicationDbContext db)
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
            Name = "Publish Race",
            ScheduledStartTime = scheduledStart,
            ScheduledEndTime = scheduledStart.AddHours(1),
            NumberOfLegs = 1,
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

        // Cả 2 trọng tài submit khớp → leg Confirmed → race PendingResult.
        var entryIds = await db.Entries
            .Where(e => e.RaceId == race.RaceId).OrderBy(e => e.EntryId).Select(e => e.EntryId).ToListAsync();
        var positions = entryIds.Select((id, i) => new SubmitPositionItem(id, i + 1)).ToList();

        var submit = new SubmitLegResultCommandHandler(db, new RaceLiveChangeTracker());
        await submit.Handle(new SubmitLegResultCommand(race.RaceId, 0, Referee1, positions), CancellationToken.None);
        var done = await submit.Handle(
            new SubmitLegResultCommand(race.RaceId, 0, Referee2, positions), CancellationToken.None);

        Assert.Equal("Matched", done.Status);
        Assert.Equal(RaceExecutionConstants.RacePendingResult,
            (await db.Races.FirstAsync(r => r.RaceId == race.RaceId)).Status);

        return race;
    }

    [Fact]
    public async Task NoViolations_AdminCanPublish_ResultsBecomeVisible()
    {
        var db = CreateDb();
        var race = await SeedRaceAtPendingResultAsync(db);

        var publish = new PublishRaceResultCommandHandler(
            db, new FakeReviewHistoryRepository(), new RaceLiveChangeTracker());

        var result = await publish.Handle(
            new PublishRaceResultCommand(race.RaceId, AdminId), CancellationToken.None);

        Assert.Equal(RaceExecutionConstants.RaceFinished, result.Status);
        Assert.Equal(3, result.ResultsCount);

        // RaceResult là thứ Spectator xem được sau khi publish.
        Assert.Equal(3, await db.RaceResults.CountAsync(r => r.RaceId == race.RaceId));
    }

    [Fact]
    public async Task PendingViolation_BlocksPublish_UntilAdminReviewsIt()
    {
        var db = CreateDb();
        var race = await SeedRaceAtPendingResultAsync(db);
        var entryId = await db.Entries.Where(e => e.RaceId == race.RaceId)
            .OrderBy(e => e.EntryId).Select(e => e.EntryId).FirstAsync();

        // Trọng tài báo cáo vi phạm → Status = Pending.
        var createViolation = new CreateViolationCommandHandler(db);
        var violationId = await createViolation.Handle(
            new CreateViolationCommand(
                RaceId: race.RaceId,
                LegNumber: 0,            // 0 → BE tự chọn leg hiện hành
                EntryId: entryId,
                ReportedByRefereeId: Referee1,
                ViolationType: "Interference",
                Description: "Cắt ngang đối thủ",
                Penalty: "Warning",
                Status: "Pending",
                ReviewedByAdminId: null,
                AdminNote: null),
            CancellationToken.None);

        Assert.True(violationId > 0);

        var publish = new PublishRaceResultCommandHandler(
            db, new FakeReviewHistoryRepository(), new RaceLiveChangeTracker());

        // ĐÂY chính là lý do "Admin không publish được": vi phạm chưa duyệt chặn publish.
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            publish.Handle(new PublishRaceResultCommand(race.RaceId, AdminId), CancellationToken.None));

        Assert.Contains("unresolved violation", ex.Message, StringComparison.OrdinalIgnoreCase);

        // Admin xử lý vi phạm (reject) → publish chạy được.
        var reject = new RejectViolationCommandHandler(db, new FakeReviewHistoryRepository());
        await reject.Handle(
            new RejectViolationCommand(violationId, AdminId, "Không đủ bằng chứng"), CancellationToken.None);

        var result = await publish.Handle(
            new PublishRaceResultCommand(race.RaceId, AdminId), CancellationToken.None);

        Assert.Equal(RaceExecutionConstants.RaceFinished, result.Status);
        Assert.Equal(3, await db.RaceResults.CountAsync(r => r.RaceId == race.RaceId));
    }
}
