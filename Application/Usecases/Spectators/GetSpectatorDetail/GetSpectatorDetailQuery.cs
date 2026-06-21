using MediatR;

namespace Application.Usecases.Spectators.GetSpectatorDetail;

public sealed record GetSpectatorDetailQuery(
    int UserId
) : IRequest<SpectatorDetailResponse?>;