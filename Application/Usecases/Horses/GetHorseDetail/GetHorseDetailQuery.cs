using MediatR;

namespace Application.Usecases.Horses.GetHorseDetail;

public sealed record GetHorseDetailQuery(
    int HorseId
) : IRequest<HorseDetailResponse?>;