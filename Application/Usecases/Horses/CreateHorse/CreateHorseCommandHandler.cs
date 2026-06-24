using Application.Common;
using Domain.Aggregates.Entities;
using Domain.Constants;
using MediatR;
using ShareKernel.UnitOfWork;

namespace Application.Usecases.Horses.CreateHorse;

/// <summary>
/// Flow 1: Horse Owner submits a horse profile. The horse starts in Pending status
/// and must be approved by an admin before it can join Race Entries.
/// </summary>
public sealed class CreateHorseCommandHandler
    : IRequestHandler<CreateHorseCommand, int>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateHorseCommandHandler(
        IHorseRepository horseRepository,
        IUnitOfWork unitOfWork)
    {
        _horseRepository = horseRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> Handle(
        CreateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Horse name is required.");
        }

        if (request.OwnerId <= 0)
        {
            throw new InvalidOperationException("OwnerId is invalid.");
        }

        var horse = new Horse
        {
            OwnerId = request.OwnerId,
            Name = request.Name.Trim(),
            Breed = request.Breed?.Trim(),
            BirthYear = request.BirthYear,
            Color = request.Color?.Trim(),
            ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim(),
            Status = HorseStatus.Pending,
            CreatedAt = DateTime.UtcNow,
        };

        await _horseRepository.AddAsync(horse, cancellationToken);

        // Save explicitly so EF Core populates the DB-generated HorseId before returning.
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return horse.HorseId;
    }
}
