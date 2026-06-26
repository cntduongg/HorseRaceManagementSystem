using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/resume — Admin resume đua đang Paused → InProgress.
public sealed record ResumeRaceCommand(int RaceId) : ICommand<ResumeRaceResponse>;

public sealed record ResumeRaceResponse(int RaceId, string Status);

public sealed class ResumeRaceCommandHandler
    : IRequestHandler<ResumeRaceCommand, ResumeRaceResponse>
{
    private readonly IApplicationDbContext _context;

    public ResumeRaceCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ResumeRaceResponse> Handle(
        ResumeRaceCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .Include(r => r.Legs)
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RacePaused)
            throw new InvalidOperationException(
                $"Chỉ có thể resume đua đang Paused (hiện tại: {race.Status}).");

        // Còn leg nào Conflicted chưa resolve thì không cho resume.
        var hasUnresolvedConflict = race.Legs
            .Any(l => l.Status == RaceExecutionConstants.LegConflicted);

        if (hasUnresolvedConflict)
            throw new InvalidOperationException(
                "Vẫn còn leg đang Conflicted chưa được resolve.");

        race.Status = RaceExecutionConstants.RaceInProgress;
        race.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ResumeRaceResponse(race.RaceId, race.Status);
    }
}
