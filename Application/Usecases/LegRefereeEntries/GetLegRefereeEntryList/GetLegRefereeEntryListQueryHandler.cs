using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegRefereeEntries.GetLegRefereeEntryList;

public sealed class GetLegRefereeEntryListQueryHandler
    : IRequestHandler<GetLegRefereeEntryListQuery, List<LegRefereeEntryListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetLegRefereeEntryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<LegRefereeEntryListItemResponse>> Handle(
        GetLegRefereeEntryListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.LegRefereeEntries
     .Select(x => new LegRefereeEntryListItemResponse(
         x.LegRefereeEntryId,
         x.RaceId,
         x.LegNumber,
         x.EntryId,
         x.ResultStatus
     ))
     .ToListAsync(cancellationToken);
    }
}