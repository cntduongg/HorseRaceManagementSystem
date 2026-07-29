using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Tournaments.GetTournamentDetail;

public sealed class GetTournamentDetailQueryHandler
    : IRequestHandler<GetTournamentDetailQuery, TournamentDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetTournamentDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TournamentDetailResponse?> Handle(
        GetTournamentDetailQuery request,
        CancellationToken cancellationToken)
    {
        if (request.TournamentId <= 0)
            return null;

        return await _context.Tournaments
            .Where(x => x.TournamentId == request.TournamentId)
            .Select(x => new TournamentDetailResponse(
                x.TournamentId,
                x.Name,
                x.Description,
                x.Location,
                x.StartDate,
                x.EndDate,
                x.Status,
                x.CancelReason
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}