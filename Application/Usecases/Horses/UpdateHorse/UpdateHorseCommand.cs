using MediatR;

namespace Application.Usecases.Horses.UpdateHorse;

public sealed record UpdateHorseCommand(
    int HorseId,
    int OwnerId,
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string? ImageUrl
) : IRequest<UpdateHorseResult>;

public enum UpdateHorseError
{
    None,
    NotFound,
    Forbidden,
    InvalidStatus
}

public sealed record UpdateHorseResult(
    bool Success,
    UpdateHorseError Error,
    string? Message = null);