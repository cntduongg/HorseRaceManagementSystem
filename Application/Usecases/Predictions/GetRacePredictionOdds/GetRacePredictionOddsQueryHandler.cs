using Application.Common.Interfaces;
using Application.Usecases.Predictions.Common;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed class GetLegPredictionOddsQueryHandler : IRequestHandler<GetLegPredictionOddsQuery, RacePredictionOddsResponse>
{
    private readonly IApplicationDbContext _context;
    public GetLegPredictionOddsQueryHandler(IApplicationDbContext context) => _context = context;

    public async Task<RacePredictionOddsResponse> Handle(GetLegPredictionOddsQuery request, CancellationToken cancellationToken)
    {
        var race = await _context.Races.AsNoTracking()
            .Where(r => r.RaceId == request.RaceId)
            .Select(r => new { r.RaceId, r.Name, r.Status, r.ScheduledStartTime, r.OddsComputedAt })
            .FirstOrDefaultAsync(cancellationToken) ?? throw new KeyNotFoundException("Race not found.");

        var leg = await _context.Legs.AsNoTracking()
            .FirstOrDefaultAsync(l => l.RaceId == request.RaceId && l.LegNumber == request.LegNumber, cancellationToken)
            ?? throw new KeyNotFoundException("Leg not found.");

        // Chỉ cho phép xem Odds khi Leg chưa diễn ra xong (Completed / Cancelled)
        if (leg.ExecutionStatus == "Completed" || leg.ExecutionStatus == "Cancelled")
            throw new InvalidOperationException("Leg is already finished.");

        // Cửa cược mở khi Leg chưa bắt đầu chạy (Pending/PredictionOpen/AwaitingResult).
        // Leg đang InProgress vẫn xem được Odds nhưng không được đặt cược → FE dùng cờ này để khóa form.
        var isBettingOpen = LegExecutionStatuses.IsBettingOpen(leg.ExecutionStatus);

        // Cùng một phép tính với CreatePrediction → currentOdds hiển thị ở đây CHÍNH LÀ odds
        // sẽ được khóa vào phiếu, không phụ thuộc spectator cược bao nhiêu.
        var dynamicOdds = await PredictionOddsCalculator.CalculateLegOddsAsync(
            _context, request.RaceId, request.LegNumber, cancellationToken);
        var oddsByEntryId = dynamicOdds.ToDictionary(x => x.EntryId);

        var entries = await _context.Entries.AsNoTracking()
            .Where(e => e.RaceId == request.RaceId && e.Status == "Approved" && e.Odds > 0)
            .OrderBy(e => e.GateNumber ?? int.MaxValue).ThenBy(e => e.EntryId)
            .Select(e => new { e.EntryId, e.HorseId, HorseName = e.Horse.Name,HorseStamina = e.Horse.Stamina, HorseHealthStatus = e.Horse.HealthStatus,  e.JockeyId, JockeyName = e.Jockey.FullName, e.GateNumber, e.Odds })
            .ToListAsync(cancellationToken);

        var resultEntries = entries
            .Where(e => oddsByEntryId.ContainsKey(e.EntryId))
            .Select(e =>
            {
                var row = oddsByEntryId[e.EntryId];
                return new RacePredictionOddsEntryResponse(
                    e.EntryId, e.HorseId, e.HorseName, null, e.JockeyId, e.JockeyName, null, 0, "", e.GateNumber,
                    row.BaseOdds, row.CurrentOdds, row.EntryPool, row.TotalPool, e.HorseStamina, e.HorseHealthStatus);
            })
            .ToList();

        return new RacePredictionOddsResponse(race.RaceId, race.Name, race.Status, race.ScheduledStartTime, race.OddsComputedAt, isBettingOpen, resultEntries);
    }
}