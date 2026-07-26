using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Predictions.GetPredictionList;

public sealed class GetPredictionListQueryHandler
    : IRequestHandler<GetPredictionListQuery, List<PredictionListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetPredictionListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PredictionListItemResponse>> Handle(
        GetPredictionListQuery request,
        CancellationToken cancellationToken)
    {
        var query = _context.Predictions.AsNoTracking();

        // Không phải ADMIN → chỉ cược của chính mình.
        if (request.ViewerSpectatorId is int spectatorId)
        {
            query = query.Where(x => x.SpectatorId == spectatorId);
        }

        return await query
            .Select(x => new PredictionListItemResponse(
                x.PredictionId,
                x.RaceId,
                x.SpectatorId,
                x.BetAmount,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}