using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Tournaments.DeleteTournament;

public sealed class DeleteTournamentCommandHandler
    : IRequestHandler<DeleteTournamentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteTournamentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TournamentId <= 0)
            return false;

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(
                x => x.TournamentId == request.TournamentId,
                cancellationToken);

        if (tournament is null)
            return false;

        _context.Tournaments.Remove(tournament);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}