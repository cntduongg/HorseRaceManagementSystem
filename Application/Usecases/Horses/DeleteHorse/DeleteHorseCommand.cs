using MediatR;

namespace Application.Usecases.Horses.DeleteHorse;

public sealed record DeleteHorseCommand(
    int HorseId,
    int OwnerId
) : IRequest<DeleteHorseResult>;

public enum DeleteHorseError
{
    None,
    NotFound,
    Forbidden
}

public sealed record DeleteHorseResult(
    bool Success,
    DeleteHorseError Error);