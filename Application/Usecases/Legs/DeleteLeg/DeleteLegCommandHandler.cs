using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Legs.DeleteLeg;

public sealed class DeleteLegCommandHandler
    : IRequestHandler<DeleteLegCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteLegCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteLegCommand request,
        CancellationToken cancellationToken)
    {
        var leg = await _context.Legs.FirstOrDefaultAsync(
            x => x.RaceId == request.RaceId &&
                 x.LegNumber == request.LegNumber,
            cancellationToken);

        if (leg is null)
            return false;

        _context.Legs.Remove(leg);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}