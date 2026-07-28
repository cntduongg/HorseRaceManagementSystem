using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed class GetRacePredictionOddsQueryHandler
    : IRequestHandler<GetRacePredictionOddsQuery, RacePredictionOddsResponse>
{
    private const string RaceScheduled = "Scheduled";
    private const string EntryApproved = "Approved";

    private readonly IApplicationDbContext _context;

    public GetRacePredictionOddsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<RacePredictionOddsResponse> Handle(
        GetRacePredictionOddsQuery request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .AsNoTracking()
            .Where(r => r.RaceId == request.RaceId)
            .Select(r => new
            {
                r.RaceId,
                r.Name,
                r.Status,
                r.ScheduledStartTime,
                r.OddsComputedAt
            })
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        if (!string.Equals(race.Status?.Trim(), RaceScheduled, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Betting odds are only shown while the race is Scheduled. Current status: {race.Status}");
        }

        if (race.OddsComputedAt is null)
        {
            throw new InvalidOperationException(
                "Odds have not been generated. Admin must close registration first.");
        }

        var entries = await _context.Entries
            .AsNoTracking()
            .Where(e =>
                e.RaceId == request.RaceId &&
                e.Status == EntryApproved &&
                e.Odds > 0)
            .OrderBy(e => e.GateNumber ?? int.MaxValue)
            .ThenBy(e => e.EntryId)
            .Select(e => new
            {
                e.EntryId,
                e.HorseId,
                HorseName = e.Horse.Name,
                HorseImageUrl = e.Horse.ImageUrl,
                e.JockeyId,
                JockeyName = e.Jockey.FullName,
                JockeyAvatarUrl = e.Jockey.AvatarUrl,
                e.HorseOwnerId,
                HorseOwnerName = e.HorseOwner.FullName,
                e.GateNumber,
                e.Odds
            })
            .ToListAsync(cancellationToken);

        if (entries.Count == 0)
        {
            throw new InvalidOperationException("The race has no Approved entries with valid odds.");
        }

        // Pool chỉ để hiển thị "bao nhiêu người đang theo con này" — KHÔNG còn tham gia vào giá.
        var pools = await _context.Predictions
            .AsNoTracking()
            .Where(p => p.RaceId == request.RaceId && p.Status != PredictionStatus.Cancelled)
            .GroupBy(p => p.FirstEntryId)
            .Select(g => new { EntryId = g.Key, Amount = g.Sum(x => x.BetAmount) })
            .ToDictionaryAsync(x => x.EntryId, x => x.Amount, cancellationToken);

        var totalPool = pools.Values.Sum();

        var resultEntries = entries
            .Select(e =>
            {
                pools.TryGetValue(e.EntryId, out var entryPool);

                return new RacePredictionOddsEntryResponse(
                    e.EntryId,
                    e.HorseId,
                    e.HorseName,
                    e.HorseImageUrl,
                    e.JockeyId,
                    e.JockeyName,
                    e.JockeyAvatarUrl,
                    e.HorseOwnerId,
                    e.HorseOwnerName,
                    e.GateNumber,
                    e.Odds,
                    entryPool,
                    totalPool);
            })
            .ToList();

        return new RacePredictionOddsResponse(
            race.RaceId,
            race.Name,
            race.Status,
            race.ScheduledStartTime,
            race.OddsComputedAt,
            resultEntries);
    }
}
