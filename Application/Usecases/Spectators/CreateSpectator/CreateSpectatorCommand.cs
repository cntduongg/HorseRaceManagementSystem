using MediatR;

namespace Application.Usecases.Spectators.CreateSpectator;

public sealed record CreateSpectatorCommand(
    int UserId
) : IRequest<int>;