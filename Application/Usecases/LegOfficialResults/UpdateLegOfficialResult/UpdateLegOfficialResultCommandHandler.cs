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

        var entity = await _context.LegOfficialResults.FirstOrDefaultAsync(x =>
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