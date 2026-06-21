using MediatR;

namespace Application.Usecases.Spectators.UpdateSpectator;

public sealed record UpdateSpectatorCommand(
	int UserId,
	bool IsActive
) : IRequest<bool>;