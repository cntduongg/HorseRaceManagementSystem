using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryDetail;

public sealed class GetLegRefereeEntryDetailQueryHandler
    : IRequestHandler<GetLegRefereeEntryDetailQuery, LegRefereeEntryDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetLegRefereeEntryDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<LegRefereeEntryDetailResponse?> Handle(
        GetLegRefereeEntryDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LegRefereeEntries
            .Where(x => x.LegRefereeEntryId == request.LegRefereeEntryId)
            .Select(x => new LegRefereeEntryDetailResponse(
                x.LegRefereeEntryId,
                x.RaceId,
                x.LegNumber,
                x.EntryId,
                x.RefereeUserId,
                x.FinishPosition,
                x.ResultStatus,
                x.SubmittedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}