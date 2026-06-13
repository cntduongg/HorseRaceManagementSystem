using MediatR;

namespace Application.Usecases.Spectators.DeleteSpectator;

public sealed record DeleteSpectatorCommand(
    int UserId
) : IRequest<bool>;