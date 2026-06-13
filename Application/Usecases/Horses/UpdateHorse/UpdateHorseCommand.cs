using MediatR;

namespace Application.Usecases.Horses.UpdateHorse;

public sealed record UpdateHorseCommand(
    int HorseId,
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string? ImageUrl
) : IRequest<bool>;