using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/open-registration — Admin mở cổng đăng ký (đánh dấu thời điểm mở).
public sealed record OpenRegistrationCommand(int RaceId) : ICommand<OpenRegistrationResponse>;

public sealed record OpenRegistrationResponse(int RaceId, DateTime RegistrationOpenAt);

public sealed class OpenRegistrationCommandHandler
    : IRequestHandler<OpenRegistrationCommand, OpenRegistrationResponse>
{
    private readonly IApplicationDbContext _context;

    public OpenRegistrationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<OpenRegistrationResponse> Handle(
        OpenRegistrationCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RaceScheduled)
            throw new InvalidOperationException("Registration can only be opened while the race is Scheduled.");
        if (race.OddsComputedAt != null)
            throw new InvalidOperationException("Registration is closed and cannot be reopened.");

        var now = DateTime.UtcNow;
        race.RegistrationOpenAt = now;
        race.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);
        return new OpenRegistrationResponse(race.RaceId, now);
    }
}
