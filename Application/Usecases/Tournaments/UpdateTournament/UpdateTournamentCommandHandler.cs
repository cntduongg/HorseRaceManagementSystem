using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Tournaments.UpdateTournament;

public sealed class UpdateTournamentCommandHandler
    : IRequestHandler<UpdateTournamentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateTournamentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Tournament name is required.");

        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("StartDate cannot be later than EndDate.");

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(
                x => x.TournamentId == request.TournamentId,
                cancellationToken);

        if (tournament is null)
            return false;

        tournament.Name = request.Name.Trim();
        tournament.Description = request.Description;
        tournament.Location = request.Location;
        tournament.StartDate = request.StartDate;
        tournament.EndDate = request.EndDate;
        tournament.LogoUrl = request.LogoUrl;
        tournament.Status = request.Status;
        tournament.CancelReason = request.CancelReason;
        tournament.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}