using MediatR;

namespace Application.Usecases.Horses.CreateHorse;

public sealed record CreateHorseCommand(
    int OwnerId,
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string? ImageUrl
) : IRequest<int>;