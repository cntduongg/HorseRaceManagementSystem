using MediatR;

namespace Application.Usecases.Horses.GetHorseList;

public sealed record GetHorseListQuery(
    int? OwnerId = null)
    : IRequest<List<HorseListItemResponse>>;