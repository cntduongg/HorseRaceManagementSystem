using Application.Common.Interfaces;
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
            .FirstOrDefaultAsync(x => x.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race not found.");

        var warnings = new List<string>();

        var finalStandings = await _context.RaceResults
            .AsNoTracking()
            .Where(x => x.RaceId == request.RaceId)
            .OrderBy(x => x.FinalPosition ?? int.MaxValue)
            .ThenBy(x => x.EntryId)
            .Select(x => new FinalStandingReviewItem(
                x.EntryId,
                x.Entry.HorseId,
                x.Entry.Horse.Name,
                x.Entry.JockeyId,
                x.Entry.Jockey.FullName,
                x.Entry.HorseOwnerId,
                x.Entry.HorseOwner.FullName,
                x.FinalPosition,
                x.IsRaceDQ,
                x.ViolationNote
            ))
            .ToListAsync(cancellationToken);

        if (finalStandings.Count == 0)
        {
            warnings.Add("Race chưa có final standings.");
        }

        if (!finalStandings.Any(x => x.FinalPosition == 1 && !x.IsRaceDQ))
        {
            warnings.Add("Chưa có ngựa thắng hợp lệ ở vị trí 1.");
        }

        var duplicatedPositions = finalStandings
            .Where(x => x.FinalPosition.HasValue && !x.IsRaceDQ)
            .GroupBy(x => x.FinalPosition!.Value)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToList();

        if (duplicatedPositions.Count > 0)
        {
            warnings.Add($"Có vị trí bị trùng: {string.Join(", ", duplicatedPositions)}.");
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
            finalStandings.Count > 0 &&
            winningEntryId is not null &&
            duplicatedPositions.Count == 0;

        if (!canPublish)
        {
            warnings.Add("Race chưa đủ điều kiện publish.");
        }

        return new ReviewRacePublicationResponse(
            race.RaceId,
            race.Name,
            race.Status,
            canPublish,
            warnings,
            finalStandings,
            preview);
    }
}