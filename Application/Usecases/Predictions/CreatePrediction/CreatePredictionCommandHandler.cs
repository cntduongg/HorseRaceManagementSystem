using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.CreatePrediction;

public sealed class CreatePredictionCommandHandler
    : IRequestHandler<CreatePredictionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePredictionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.BetAmount < 10)
            throw new InvalidOperationException("BetAmount must be at least 10.");

        if (request.FirstEntryId == request.SecondEntryId ||
            request.FirstEntryId == request.ThirdEntryId ||
            request.SecondEntryId == request.ThirdEntryId)
        {
            throw new InvalidOperationException("Selected entries must be different.");
        }

        // Check Race
        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race not found.");

        // Check Spectator
        var spectatorExists = await _context.Spectators
            .AnyAsync(x => x.UserId == request.SpectatorId, cancellationToken);

        if (!spectatorExists)
            throw new InvalidOperationException("Spectator not found.");

        // Check Entries
        var entryIds = new[] { request.FirstEntryId, request.SecondEntryId, request.ThirdEntryId };

        var validEntries = await _context.Entries
            .CountAsync(x => entryIds.Contains(x.EntryId), cancellationToken);

        if (validEntries != 3)
            throw new InvalidOperationException("One or more entries not found.");

        var prediction = new Prediction
        {
            RaceId = request.RaceId,
            SpectatorId = request.SpectatorId,
            FirstEntryId = request.FirstEntryId,
            SecondEntryId = request.SecondEntryId,
            ThirdEntryId = request.ThirdEntryId,
            BetAmount = request.BetAmount,
            OddsLocked1 = request.OddsLocked1,
            OddsLocked2 = request.OddsLocked2,
            OddsLocked3 = request.OddsLocked3,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Predictions.Add(prediction);
        await _context.SaveChangesAsync(cancellationToken);

        return prediction.PredictionId;
    }
}