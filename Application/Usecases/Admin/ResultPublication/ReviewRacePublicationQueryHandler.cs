using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.ResultPublication;

public sealed class ReviewRacePublicationQueryHandler
    : IRequestHandler<ReviewRacePublicationQuery, ReviewRacePublicationResponse>
{
    private readonly IApplicationDbContext _context;

    public ReviewRacePublicationQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ReviewRacePublicationResponse> Handle(
        ReviewRacePublicationQuery request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .AsNoTracking()
            .Include(r => r.Legs)
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        var warnings = new List<string>();

        // Điều kiện publish thật (khớp PublishRaceResultCommandHandler):
        // race phải ở PendingResult và mọi Leg đã Confirmed/Resolved.
        var isPendingResult = race.Status == RaceExecutionConstants.RacePendingResult;
        if (!isPendingResult)
        {
            warnings.Add($"The race is not in PendingResult status (current: {race.Status}).");
        }

        var allLegsDone = race.Legs.Count > 0 && race.Legs.All(l =>
            l.Status is RaceExecutionConstants.LegConfirmed
                or RaceExecutionConstants.LegResolved);
        if (!allLegsDone)
        {
            warnings.Add("There are still unconfirmed legs.");
        }

        // Còn vi phạm chưa duyệt → chưa được publish (khớp guard trong PublishRaceResultCommandHandler).
        var pendingViolations = await _context.Violations
            .CountAsync(v => v.RaceId == request.RaceId && v.Status == "Pending", cancellationToken);
        var hasNoPendingViolations = pendingViolations == 0;
        if (!hasNoPendingViolations)
        {
            warnings.Add($"There are still {pendingViolations} unresolved violation(s) to review.");
        }

        // Bảng điểm "sống": tính trực tiếp từ LegOfficialResults + Violations (Approved, DQ),
        // KHÔNG đọc RaceResults (bảng đó chỉ có dữ liệu SAU khi publish chạy).
        var entries = await _context.Entries
            .AsNoTracking()
            .Include(e => e.Horse)
            .Include(e => e.Jockey)
            .Include(e => e.HorseOwner)
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .ToListAsync(cancellationToken);

        var officials = await _context.LegOfficialResults
            .AsNoTracking()
            .Where(o => o.RaceId == request.RaceId)
            .ToListAsync(cancellationToken);

        // Entry bị Race DQ (vi phạm đã duyệt) + mô tả để hiển thị lý do.
        var dqViolations = await _context.Violations
            .AsNoTracking()
            .Where(v => v.RaceId == request.RaceId && v.Status == "Approved" && v.Penalty == "DQ")
            .Select(v => new { v.EntryId, v.Description })
            .ToListAsync(cancellationToken);

        var dqSet = dqViolations.Select(v => v.EntryId).ToHashSet();
        var dqNotes = dqViolations
            .GroupBy(v => v.EntryId)
            .ToDictionary(
                g => g.Key,
                g => string.Join("; ", g
                    .Select(v => v.Description)
                    .Where(d => !string.IsNullOrWhiteSpace(d))));

        // Xếp hạng bằng đúng công thức của publish thật (helper dùng chung).
        var ranked = RaceRankingCalculator.Rank(entries, officials, dqSet);

        var hasUnresolvedTie = false;
        for (var i = 1; i < ranked.Count; i++)
        {
            var prev = ranked[i - 1];
            var curr = ranked[i];
            if (prev.IsDq != curr.IsDq)
                continue;

            if (prev.TotalPoints == curr.TotalPoints &&
                prev.LegWins == curr.LegWins &&
                prev.Leg2nds == curr.Leg2nds &&
                prev.LastLegPos == curr.LastLegPos)
            {
                hasUnresolvedTie = true;
                break;
            }
        }

        if (hasUnresolvedTie)
        {
            warnings.Add(
                "Some entries are tied on all ranking criteria; final order uses EntryId as a deterministic tie-break.");
        }

        var finalStandings = ranked
            .Select(r => new FinalStandingReviewItem(
                r.Entry.EntryId,
                r.Entry.HorseId,
                r.Entry.Horse?.Name,
                r.Entry.JockeyId,
                r.Entry.Jockey?.FullName,
                r.Entry.HorseOwnerId,
                r.Entry.HorseOwner?.FullName,
                r.FinalPosition,
                r.IsDq,
                r.IsDq && dqNotes.TryGetValue(r.Entry.EntryId, out var note) && note.Length > 0
                    ? note
                    : null))
            .ToList();

        if (finalStandings.Count == 0)
        {
            warnings.Add("The race does not have final standings yet.");
        }

        if (!finalStandings.Any(x => x.FinalPosition == 1 && !x.IsRaceDQ))
        {
            warnings.Add("There is no valid winning horse in position 1.");
        }

        var winningEntryId = finalStandings
            .Where(x => x.FinalPosition == 1 && !x.IsRaceDQ)
            .Select(x => (int?)x.EntryId)
            .FirstOrDefault();

        var activePredictions = await _context.Predictions
            .AsNoTracking()
            .Where(x =>
                x.RaceId == request.RaceId &&
                x.Status != PredictionStatus.Cancelled)
            .Select(x => new
            {
                x.FirstEntryId,
                x.BetAmount,
                x.OddsLocked1
            })
            .ToListAsync(cancellationToken);

        var correctPredictions = winningEntryId is null
            ? 0
            : activePredictions.Count(x => x.FirstEntryId == winningEntryId.Value);

        var estimatedPayout = winningEntryId is null
            ? 0m
            : activePredictions
                .Where(x => x.FirstEntryId == winningEntryId.Value)
                .Sum(x => Math.Round(
                    x.BetAmount * x.OddsLocked1,
                    2,
                    MidpointRounding.AwayFromZero));

        var preview = new PredictionSettlementPreview(
            TotalPredictions: activePredictions.Count,
            CorrectPredictions: correctPredictions,
            WrongPredictions: activePredictions.Count - correctPredictions,
            TotalBetAmount: activePredictions.Sum(x => x.BetAmount),
            EstimatedTotalPayout: estimatedPayout);

        var canPublish =
            isPendingResult &&
            allLegsDone &&
            hasNoPendingViolations &&
            finalStandings.Count > 0 &&
            winningEntryId is not null;

        if (!canPublish)
        {
            warnings.Add("The race is not eligible for publishing yet.");
        }

        return new ReviewRacePublicationResponse(
            race.RaceId,
            race.Name,
            race.Status,
            pendingViolations,
            hasUnresolvedTie,
            canPublish,
            warnings,
            finalStandings,
            preview);
    }
}
