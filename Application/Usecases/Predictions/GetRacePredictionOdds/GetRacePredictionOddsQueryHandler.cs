using Application.Common.Interfaces;
using Application.Usecases.Predictions.Common;
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
                $"Chỉ hiển thị odds để betting khi race đang Scheduled. Current status: {race.Status}");
        }

        if (race.OddsComputedAt is null)
        {
            throw new InvalidOperationException(
                "Odds chưa được tạo. Admin cần close registration trước.");
        }

        var dynamicOdds = await PredictionOddsCalculator.CalculateRaceOddsAsync(
            _context,
            request.RaceId,
            cancellationToken);

        var oddsByEntryId = dynamicOdds.ToDictionary(x => x.EntryId);

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
                e.GateNumber
            })
            .ToListAsync(cancellationToken);

        var resultEntries = entries
            .Where(e => oddsByEntryId.ContainsKey(e.EntryId))
            .Select(e =>
            {
                var odds = oddsByEntryId[e.EntryId];

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
                    odds.BaseOdds,
                    odds.CurrentOdds,
                    odds.EntryPool,
                    odds.TotalPool);
            })
            .ToList();

        if (resultEntries.Count == 0)
        {
            throw new InvalidOperationException("Race chưa có entry Approved với odds hợp lệ.");
        }

        return new RacePredictionOddsResponse(
            race.RaceId,
            race.Name,
            race.Status,
            race.ScheduledStartTime,
            race.OddsComputedAt,
            IsBettingOpen: true,
            resultEntries);
    }
}