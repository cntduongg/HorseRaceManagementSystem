using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.GetPredictionDetail;

public sealed class GetPredictionDetailQueryHandler
    : IRequestHandler<GetPredictionDetailQuery, PredictionDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetPredictionDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PredictionDetailResponse?> Handle(
        GetPredictionDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Predictions
            .Where(x => x.PredictionId == request.PredictionId)
            .Select(x => new PredictionDetailResponse(
                x.PredictionId,
                x.RaceId,
                x.LegNumber,
                x.SpectatorId,
                x.FirstEntryId,
                x.SecondEntryId,
                x.ThirdEntryId,
                x.BetAmount,
                x.OddsLocked1,
                x.OddsLocked2,
                x.OddsLocked3,
                x.Status
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}