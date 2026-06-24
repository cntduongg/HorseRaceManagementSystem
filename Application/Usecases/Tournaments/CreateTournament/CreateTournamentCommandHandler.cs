using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

namespace Application.Usecases.Tournaments.CreateTournament;

public sealed class CreateTournamentCommandHandler
    : IRequestHandler<CreateTournamentCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateTournamentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Tournament name is required.");

        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("StartDate cannot be later than EndDate.");

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            LogoUrl = request.LogoUrl,
            Status = "Draft",
            CreatedAt = DateTime.UtcNow
        };

        _context.Tournaments.Add(tournament);

        await _context.SaveChangesAsync(cancellationToken);

        return tournament.TournamentId;
    }
}