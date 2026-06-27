using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.OpenRegistration;

public sealed class OpenRaceRegistrationCommandHandler
    : IRequestHandler<OpenRaceRegistrationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public OpenRaceRegistrationCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        OpenRaceRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == request.RaceId,
                cancellationToken);

        if (race is null)
            return false;

        if (race.Status != "Scheduled")
            throw new InvalidOperationException(
                "Only scheduled races can open registration.");

        var now = DateTime.UtcNow;

        if (race.RegistrationOpenAt != null &&
            race.RegistrationOpenAt <= now &&
            race.RegistrationCloseAt != null &&
            race.RegistrationCloseAt > now)
        {
            throw new InvalidOperationException(
                "Registration is already open.");
        }

        race.RegistrationOpenAt = now;

        if (race.RegistrationCloseAt == null ||
            race.RegistrationCloseAt <= now)
        {
            race.RegistrationCloseAt = race.ScheduledStartTime;
        }

        race.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}