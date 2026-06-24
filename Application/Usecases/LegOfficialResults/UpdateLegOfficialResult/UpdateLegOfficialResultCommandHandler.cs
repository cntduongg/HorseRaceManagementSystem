using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegOfficialResults.UpdateLegOfficialResult;

public sealed class UpdateLegOfficialResultCommandHandler
    : IRequestHandler<UpdateLegOfficialResultCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateLegOfficialResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResultStatus))
            throw new InvalidOperationException("ResultStatus is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmationType))
            throw new InvalidOperationException("ConfirmationType is required.");

        var validStatuses = new[] { "Finished", "DNF", "DQ" };
        if (!validStatuses.Contains(request.ResultStatus.Trim()))
            throw new InvalidOperationException("Invalid ResultStatus.");

        var validConfirmationTypes = new[] { "AutoMatched", "AdminOverride" };
        if (!validConfirmationTypes.Contains(request.ConfirmationType.Trim()))
            throw new InvalidOperationException("Invalid ConfirmationType.");

        if (request.FinishPosition.HasValue && request.FinishPosition <= 0)
            throw new InvalidOperationException("FinishPosition must be greater than 0.");

        if (request.LegPoints < 0)
            throw new InvalidOperationException("LegPoints cannot be negative.");

        if (request.ConfirmationType.Trim() == "AdminOverride"
            && string.IsNullOrWhiteSpace(request.OverrideReason))
        {
            throw new InvalidOperationException(
                "OverrideReason is required for AdminOverride.");
        }

        if (request.ConfirmedByAdminId.HasValue)
        {
            var adminExists = await _context.Users.AnyAsync(
                x => x.UserId == request.ConfirmedByAdminId.Value,
                cancellationToken);

            if (!adminExists)
                throw new InvalidOperationException("ConfirmedByAdmin does not exist.");
        }

        var entity = await _context.LegOfficialResults
            .FirstOrDefaultAsync(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber &&
                x.EntryId == request.EntryId,
                cancellationToken);

        if (entity is null)
            return false;

        entity.FinishPosition = request.FinishPosition;
        entity.ResultStatus = request.ResultStatus.Trim();
        entity.LegPoints = request.LegPoints;
        entity.ConfirmationType = request.ConfirmationType.Trim();
        entity.ConfirmedByAdminId = request.ConfirmedByAdminId;
        entity.OverrideReason = request.OverrideReason;
        entity.ConfirmedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}