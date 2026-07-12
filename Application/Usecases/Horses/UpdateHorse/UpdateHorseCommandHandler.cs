using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.UpdateHorse;

public sealed class UpdateHorseCommandHandler
    : IRequestHandler<UpdateHorseCommand, UpdateHorseResult>
{
    private readonly IApplicationDbContext _context;

    public UpdateHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<UpdateHorseResult> Handle(
        UpdateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
            return new UpdateHorseResult(false, UpdateHorseError.NotFound);

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Horse name is required.");

        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            return new UpdateHorseResult(false, UpdateHorseError.NotFound);

        if (horse.OwnerId != request.OwnerId)
            return new UpdateHorseResult(false, UpdateHorseError.Forbidden);

        if (horse.Status == HorseStatus.Approved)
        {
            return new UpdateHorseResult(
                false,
                UpdateHorseError.InvalidStatus,
                "The horse is already approved and cannot be edited directly.");
        }

        horse.Name = request.Name.Trim();
        horse.Breed = request.Breed;
        horse.BirthYear = request.BirthYear;
        horse.Color = request.Color;
        horse.ImageUrl = request.ImageUrl;
        horse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateHorseResult(true, UpdateHorseError.None);
    }
}