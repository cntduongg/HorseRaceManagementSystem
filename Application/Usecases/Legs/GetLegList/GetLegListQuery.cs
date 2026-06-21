using MediatR;

namespace Application.Usecases.Legs.GetLegList;

public sealed record GetLegListQuery()
    : IRequest<List<LegListItemResponse>>;