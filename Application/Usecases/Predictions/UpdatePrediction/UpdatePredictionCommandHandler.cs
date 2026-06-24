using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.UpdatePrediction;

public sealed class UpdatePredictionCommandHandler
    : IRequestHandler<UpdatePredictionCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdatePredictionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdatePredictionCommand request,
        CancellationToken cancellationToken)
    {
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

        var validStatus = new[]
        {
            "Pending",
            "Won",
            "Lost",
            "Cancelled"
        };

        if (!validStatus.Contains(request.Status))
            throw new InvalidOperationException("Invalid Status.");

        var prediction = await _context.Predictions
            .FirstOrDefaultAsync(
                x => x.PredictionId == request.PredictionId,
                cancellationToken);

        if (prediction is null)
            return false;

        var raceExists = await _context.Races
            .AnyAsync(x => x.RaceId == request.RaceId, cancellationToken);

        if (!raceExists)
            throw new InvalidOperationException("Race not found.");

        var spectator = await _context.Spectators
            .FirstOrDefaultAsync(
                x => x.UserId == request.SpectatorId,
                cancellationToken);

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

        prediction.RaceId = request.RaceId;
        prediction.SpectatorId = request.SpectatorId;
        prediction.FirstEntryId = request.FirstEntryId;
        prediction.SecondEntryId = request.SecondEntryId;
        prediction.ThirdEntryId = request.ThirdEntryId;
        prediction.BetAmount = request.BetAmount;
        prediction.OddsLocked1 = request.OddsLocked1;
        prediction.OddsLocked2 = request.OddsLocked2;
        prediction.OddsLocked3 = request.OddsLocked3;
        prediction.Status = request.Status;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}