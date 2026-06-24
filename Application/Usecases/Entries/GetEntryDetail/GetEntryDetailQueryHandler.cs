using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Entries.GetEntryDetail;

public sealed class GetEntryDetailQueryHandler
    : IRequestHandler<GetEntryDetailQuery, EntryDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetEntryDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EntryDetailResponse?> Handle(
        GetEntryDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Entries
            .Where(x => x.EntryId == request.EntryId)
            .Select(x => new EntryDetailResponse(
                x.EntryId,
                x.RaceId,
                x.HorseId,
                x.JockeyId,
                x.HorseOwnerId,
                x.Status,
                x.GateNumber
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}