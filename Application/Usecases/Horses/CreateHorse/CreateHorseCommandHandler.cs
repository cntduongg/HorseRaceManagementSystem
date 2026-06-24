using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

namespace Application.Usecases.Horses.CreateHorse;

public sealed class CreateHorseCommandHandler
    : IRequestHandler<CreateHorseCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateHorseCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Horse name is required.");

        if (request.OwnerId <= 0)
            throw new InvalidOperationException("OwnerId is invalid.");

        var horse = new Horse
        {
            OwnerId = request.OwnerId,
            Name = request.Name.Trim(),
            Breed = request.Breed,
            BirthYear = request.BirthYear,
            Color = request.Color,
            ImageUrl = request.ImageUrl,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Horses.Add(horse);

        await _context.SaveChangesAsync(cancellationToken);

        return horse.HorseId;
    }
}