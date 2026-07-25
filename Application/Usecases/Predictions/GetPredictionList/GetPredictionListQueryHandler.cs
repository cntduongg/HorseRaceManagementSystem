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
        return await _context.Predictions
            .Select(x => new PredictionListItemResponse(
                x.PredictionId,
                x.RaceId,
                x.LegNumber,
                x.SpectatorId,
                x.BetAmount,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}