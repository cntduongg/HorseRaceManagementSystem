using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races.CloseRegistration;

public sealed class CloseRaceRegistrationCommandHandler
    : IRequestHandler<CloseRaceRegistrationCommand, bool>
{
    private readonly IApplicationDbContext _context;
    private readonly IRegistrationService _registrationService;

    public CloseRaceRegistrationCommandHandler(
        IApplicationDbContext context,
        IRegistrationService registrationService)
    {
        _context = context;
        _registrationService = registrationService;
    }

    public async Task<bool> Handle(
        CloseRaceRegistrationCommand request,
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
                "Only scheduled races can close registration.");

        await _registrationService.CloseRegistrationAsync(
            request.RaceId,
            cancellationToken);

        return true;
    }
}