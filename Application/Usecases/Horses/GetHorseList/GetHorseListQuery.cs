using MediatR;

namespace Application.Usecases.Horses.GetHorseList;

public sealed record GetHorseListQuery()
    : IRequest<List<HorseListItemResponse>>;