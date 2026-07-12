using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/close-registration — Admin đóng đăng ký:
//  • auto-reject Entry Pending còn lại
//  • tính & KHÓA Odds cho từng Entry Approved (win rate / avg finish)
//  • gán GateNumber
//  • set RegistrationCloseAt + OddsComputedAt
public sealed record CloseRegistrationCommand(int RaceId) : ICommand<CloseRegistrationResponse>;

public sealed record CloseRegistrationResponse(
    int RaceId,
    int ApprovedEntries,
    int RejectedEntries);

public sealed class CloseRegistrationCommandHandler
    : IRequestHandler<CloseRegistrationCommand, CloseRegistrationResponse>
{
    private readonly IApplicationDbContext _context;

    public CloseRegistrationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<CloseRegistrationResponse> Handle(
        CloseRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var race = await _context.Races
                .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
                ?? throw new KeyNotFoundException("Race not found.");

            if (race.Status != RaceExecutionConstants.RaceScheduled)
                throw new InvalidOperationException(
                    $"Registration can only be closed while the race is Scheduled (current: {race.Status}).");
            if (race.OddsComputedAt != null)
                throw new InvalidOperationException("Registration has already been closed.");

            var entries = await _context.Entries
                .Where(e => e.RaceId == race.RaceId)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;

            // 1) Auto-reject Entry Pending.
            var pending = entries.Where(e => e.Status == "Pending").ToList();
            foreach (var e in pending)
            {
                e.Status = "Rejected";
                e.RejectionReason = "Automatically rejected when registration closed (not approved).";
                e.UpdatedAt = now;
            }

            var approved = entries
                .Where(e => e.Status == RaceExecutionConstants.EntryApproved)
                .OrderBy(e => e.SubmittedAt)
                .ThenBy(e => e.EntryId)
                .ToList();

            if (approved.Count < 2)
                throw new InvalidOperationException(
                    "At least 2 approved entries are required to close registration.");

            // 2) Tính win rate lịch sử cho các ngựa liên quan.
            var horseIds = approved.Select(e => e.HorseId).Distinct().ToList();
            var history = await _context.RaceResults
                .Where(r => horseIds.Contains(r.Entry.HorseId) && r.FinalPosition != null)
                .Select(r => new { r.Entry.HorseId, r.FinalPosition })
                .ToListAsync(cancellationToken);

            // 3) Khóa Odds + gán GateNumber tuần tự.
            var gate = 1;
            foreach (var e in approved)
            {
                var rows = history.Where(h => h.HorseId == e.HorseId).ToList();
                var firsts = rows.Count(h => h.FinalPosition == 1);
                e.Odds = OddsFor(firsts, rows.Count, approved.Count);
                e.GateNumber = gate++;
                e.UpdatedAt = now;
            }

            race.RegistrationCloseAt = now;
            race.OddsComputedAt = now;
            race.UpdatedAt = now;

            await _context.SaveChangesAsync(cancellationToken);
            await tx.CommitAsync(cancellationToken);

            return new CloseRegistrationResponse(race.RaceId, approved.Count, pending.Count);
        }
        catch
        {
            await tx.RollbackAsync(cancellationToken);
            throw;
        }
    }

    // Odds = 1 / winRate (clamp 1.1..25). Chưa có lịch sử → theo cỡ field.
    public static decimal OddsFor(int firsts, int total, int fieldSize)
    {
        double winRate = total > 0
            ? (double)firsts / total
            : 1.0 / Math.Max(fieldSize, 2);

        var odds = (decimal)Math.Round(1.0 / Math.Max(winRate, 0.04), 2);
        return Math.Clamp(odds, 1.1m, 25m);
    }
}
