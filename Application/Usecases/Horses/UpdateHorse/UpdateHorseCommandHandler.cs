using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Horses.UpdateHorse;

public sealed class UpdateHorseCommandHandler
    : IRequestHandler<UpdateHorseCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseId <= 0)
            return false;

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Horse name is required.");

        var horse = await _context.Horses
            .FirstOrDefaultAsync(x => x.HorseId == request.HorseId, cancellationToken);

        if (horse is null)
            return false;

        horse.Name = request.Name.Trim();
        horse.Breed = request.Breed;
        horse.BirthYear = request.BirthYear;
        horse.Color = request.Color;
        horse.ImageUrl = request.ImageUrl;
        horse.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}