using MediatR;
using Microsoft.EntityFrameworkCore;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
namespace Application.Usecases.Admin.GetPendingEntries;

public sealed class GetPendingEntriesQueryHandler
    : IRequestHandler<GetPendingEntriesQuery, List<GetPendingEntriesResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetPendingEntriesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GetPendingEntriesResponse>> Handle(
        GetPendingEntriesQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Entries
            .Where(e =>
                e.Status == EntryStatus.Pending &&
                e.Race.Status == RaceStatus.Scheduled)
            .Include(e => e.Race)
            .Include(e => e.Horse)
            .Include(e => e.Jockey)
            .Select(e => new GetPendingEntriesResponse(
                e.EntryId,
                e.RaceId,
                e.Race.Name,
                e.HorseId,
                e.Horse.Name,
                e.JockeyId,
                e.Jockey.FullName,
                e.SubmittedAt
            ))
            .ToListAsync(cancellationToken);
    }
}