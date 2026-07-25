using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Domain.Aggregates.Constants;
namespace Application.Usecases.Races.PublishOdds;

public sealed class PublishRaceOddsCommandHandler
    : IRequestHandler<PublishRaceOddsCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public PublishRaceOddsCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        PublishRaceOddsCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == request.RaceId,
                cancellationToken);

        if (race is null)
            return false;
        if (race.Status == RaceStatus.Cancelled)
        {
            throw new InvalidOperationException(
                "Cannot publish odds for a cancelled race.");
        }
        if (race.OddsComputedAt == null)
            throw new InvalidOperationException(
                "Odds have not been calculated.");

        if (race.PublishedAt != null)
            throw new InvalidOperationException(
                "Odds have already been published.");
     

        race.PublishedAt = DateTime.UtcNow;
        race.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}