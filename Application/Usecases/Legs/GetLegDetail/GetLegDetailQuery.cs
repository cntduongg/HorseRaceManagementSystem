using MediatR;

namespace Application.Usecases.Legs.GetLegDetail;

public sealed record GetLegDetailQuery(
    int RaceId,
    int LegNumber
) : IRequest<LegDetailResponse?>;