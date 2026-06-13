using MediatR;

namespace Application.Usecases.LegOfficialResults.GetLegOfficialResultList;

public sealed record GetLegOfficialResultListQuery()
    : IRequest<List<LegOfficialResultListItemResponse>>;