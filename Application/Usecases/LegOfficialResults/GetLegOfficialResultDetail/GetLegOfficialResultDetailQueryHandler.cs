using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultDetail;

public sealed class GetLegOfficialResultDetailQueryHandler
    : IRequestHandler<GetLegOfficialResultDetailQuery, LegOfficialResultDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetLegOfficialResultDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LegOfficialResultDetailResponse?> Handle(
        GetLegOfficialResultDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LegOfficialResults
            .AsNoTracking()
            .Where(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber &&
                x.EntryId == request.EntryId)
            .Select(x => new LegOfficialResultDetailResponse(
                x.RaceId,
                x.LegNumber,
                x.EntryId,
                x.FinishPosition,
                x.ResultStatus,
                x.LegPoints,
                x.ConfirmationType,
                x.ConfirmedAt,
                x.ConfirmedByAdminId,
                x.OverrideReason
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}