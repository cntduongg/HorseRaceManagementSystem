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
        var query = _context.Predictions
            .AsNoTracking()
            .Where(x => x.PredictionId == request.PredictionId);

        // Không phải ADMIN → chỉ cược của chính mình (trả null → 404, không lộ tồn tại).
        if (request.ViewerSpectatorId is int spectatorId)
        {
            query = query.Where(x => x.SpectatorId == spectatorId);
        }

        return await query
            .Select(x => new PredictionDetailResponse(
                x.PredictionId,
                x.RaceId,
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