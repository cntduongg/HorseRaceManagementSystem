using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
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

    private async Task ComputeOdds(
     Race race,
     CancellationToken cancellationToken)
    {
        var approvedEntries = await _context.Entries
            .Where(x =>
                x.RaceId == race.RaceId &&
                x.Status == EntryStatus.Approved)
            .ToListAsync(cancellationToken);

        if (!approvedEntries.Any())
        {
            race.OddsComputedAt = DateTime.UtcNow;
            return;
        }

        //---------------------------------------------------
        // TEMP ALGORITHM
        // Hiện tại tất cả đều có odds = 2.0000
        //---------------------------------------------------

        foreach (var entry in approvedEntries)
        {
            entry.Odds = 2.0000m;
            entry.UpdatedAt = DateTime.UtcNow;
        }

        race.OddsComputedAt = DateTime.UtcNow;
    }
}