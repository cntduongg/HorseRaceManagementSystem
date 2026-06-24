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

        var prediction = await _context.Predictions
            .FirstOrDefaultAsync(x => x.PredictionId == request.PredictionId, cancellationToken);

        if (prediction is null)
            return false;

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