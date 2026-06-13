using MediatR;

namespace Application.Usecases.Tournaments.GetTournamentDetail;

public sealed record GetTournamentDetailQuery(
	int TournamentId
) : IRequest<TournamentDetailResponse?>;