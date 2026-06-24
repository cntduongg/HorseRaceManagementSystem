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
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is required.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is required.");

        if (request.FirstEntryId <= 0 ||
            request.SecondEntryId <= 0 ||
            request.ThirdEntryId <= 0)
            throw new InvalidOperationException("EntryId is required.");

        if (request.BetAmount < 10)
            throw new InvalidOperationException("BetAmount must be at least 10.");

        if (request.OddsLocked1 <= 0 ||
            request.OddsLocked2 <= 0 ||
            request.OddsLocked3 <= 0)
            throw new InvalidOperationException("Odds must be greater than 0.");

        if (request.FirstEntryId == request.SecondEntryId ||
            request.FirstEntryId == request.ThirdEntryId ||
            request.SecondEntryId == request.ThirdEntryId)
        {
            throw new InvalidOperationException("Selected entries must be different.");
        }

        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race not found.");

        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(x => x.UserId == request.SpectatorId, cancellationToken);

        if (spectator is null)
            throw new InvalidOperationException("Spectator not found.");

        if (!spectator.IsActive)
            throw new InvalidOperationException("Spectator is inactive.");

        var entryIds = new[]
        {
            request.FirstEntryId,
            request.SecondEntryId,
            request.ThirdEntryId
        };

        var validEntries = await _context.Entries
            .CountAsync(x =>
                entryIds.Contains(x.EntryId) &&
                x.RaceId == request.RaceId,
                cancellationToken);

        if (validEntries != 3)
            throw new InvalidOperationException("Entries must belong to the selected race.");

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