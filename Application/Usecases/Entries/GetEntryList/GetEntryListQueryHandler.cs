using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Entries.GetEntryList;

public sealed class GetEntryListQueryHandler
    : IRequestHandler<GetEntryListQuery, List<EntryListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetEntryListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<EntryListItemResponse>> Handle(
        GetEntryListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Entries
            .Select(x => new EntryListItemResponse(
                x.EntryId,
                x.RaceId,
                x.HorseId,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}