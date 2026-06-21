using MediatR;

namespace Application.Usecases.Horses.DeleteHorse;

public sealed record DeleteHorseCommand(
    int HorseId
) : IRequest<bool>;