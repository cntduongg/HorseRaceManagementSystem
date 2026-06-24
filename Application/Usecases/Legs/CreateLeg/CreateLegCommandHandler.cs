using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Legs.CreateLeg;

public sealed class CreateLegCommandHandler
    : IRequestHandler<CreateLegCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public CreateLegCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        CreateLegCommand request,
        CancellationToken cancellationToken)
    {
        if (request.LegNumber < 1 || request.LegNumber > 10)
            throw new InvalidOperationException("LegNumber must be between 1 and 10.");

        var exists = await _context.Legs
            .AnyAsync(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber,
                cancellationToken);

        if (exists)
            throw new InvalidOperationException("Leg already exists for this race.");

        var leg = new Leg
        {
            RaceId = request.RaceId,
            LegNumber = request.LegNumber,
            Status = request.Status ?? "Pending",
            ConfirmationType = request.ConfirmationType
        };

        _context.Legs.Add(leg);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}