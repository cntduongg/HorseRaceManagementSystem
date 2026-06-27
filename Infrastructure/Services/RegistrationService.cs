using Application.Common;
using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public sealed class RegistrationService : IRegistrationService
{
    private readonly IApplicationDbContext _context;

    public RegistrationService(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task CloseRegistrationAsync(
        int raceId,
        CancellationToken cancellationToken = default)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == raceId,
                cancellationToken);

        if (race is null)
            throw new InvalidOperationException("Race not found.");

        var now = DateTime.UtcNow;

        race.RegistrationCloseAt = now;
        race.UpdatedAt = now;

        await RejectPendingEntries(
            raceId,
            now,
            cancellationToken);

        await ComputeOdds(
            race,
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);
    }

    //-------------------------------------------------------

    private async Task RejectPendingEntries(
        int raceId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var entries = await _context.Entries
            .Where(x =>
                x.RaceId == raceId &&
                x.Status == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var entry in entries)
        {
            entry.Status = "Rejected";
            entry.RejectionReason = "Registration closed.";
            entry.UpdatedAt = now;
        }
    }

    //-------------------------------------------------------

    private Task ComputeOdds(
        Domain.Aggregates.Entities.Race race,
        CancellationToken cancellationToken)
    {
        /*
         * Flow 3
         *
         * Sau này sẽ:
         *
         * 1. Tính Odds
         * 2. Freeze Odds
         * 3. Publish Odds
         */

        race.OddsComputedAt = DateTime.UtcNow;

        return Task.CompletedTask;
    }
}