using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Microsoft.EntityFrameworkCore;
using MediatR;
namespace Application.Usecases.Admin.ApproveHorse;

public class ApproveHorseCommandHandler
    : IRequestHandler<ApproveHorseCommand, ApproveHorseResponse>
{
    private readonly IApplicationDbContext _context;

    public ApproveHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ApproveHorseResponse> Handle(
        ApproveHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            throw new KeyNotFoundException("Horse not found");

        if (horse.Status != HorseStatus.Pending)
            throw new InvalidOperationException("Horse is not pending");

        horse.Status = HorseStatus.Approved;
        horse.ApprovedAt = DateTime.UtcNow;
        horse.ApprovedBy = request.AdminId;
        horse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveHorseResponse(
            horse.HorseId,
            horse.Status,
            horse.ApprovedAt.Value
        );
    }
}