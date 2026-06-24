using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Legs.GetLegDetail;

public sealed class GetLegDetailQueryHandler
    : IRequestHandler<GetLegDetailQuery, LegDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetLegDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LegDetailResponse?> Handle(
        GetLegDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Legs
            .Where(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber)
            .Select(x => new LegDetailResponse(
                x.RaceId,
                x.LegNumber,
                x.Status,
                x.ConfirmationType,
                x.ConfirmedAt,
                x.ConflictReportedAt,
                x.AdminOverrideReason,
                x.StartedAt,
                x.FinishedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}