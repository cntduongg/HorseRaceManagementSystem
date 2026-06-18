using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Tournaments.GetTournamentList;

public sealed class GetTournamentListQueryHandler
    : IRequestHandler<GetTournamentListQuery, List<TournamentListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetTournamentListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<TournamentListItemResponse>> Handle(
        GetTournamentListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Tournaments
            .Select(x => new TournamentListItemResponse(
                x.TournamentId,
                x.Name,
                x.StartDate,
                x.EndDate,
                x.Status
            ))
            .ToListAsync(cancellationToken);
    }
}