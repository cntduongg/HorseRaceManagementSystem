using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Legs.UpdateLeg;

public sealed class UpdateLegCommandHandler
    : IRequestHandler<UpdateLegCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateLegCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateLegCommand request,
        CancellationToken cancellationToken)
    {
        var leg = await _context.Legs.FirstOrDefaultAsync(
            x => x.RaceId == request.RaceId &&
                 x.LegNumber == request.LegNumber,
            cancellationToken);

        if (leg is null)
            return false;

        if (!string.IsNullOrWhiteSpace(request.Status))
            leg.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.ConfirmationType))
            leg.ConfirmationType = request.ConfirmationType;

        if (!string.IsNullOrWhiteSpace(request.AdminOverrideReason))
            leg.AdminOverrideReason = request.AdminOverrideReason;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}