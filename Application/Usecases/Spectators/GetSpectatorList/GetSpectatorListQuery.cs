using MediatR;

namespace Application.Usecases.Spectators.GetSpectatorList;

public sealed record GetSpectatorListQuery()
	: IRequest<List<SpectatorListItemResponse>>;