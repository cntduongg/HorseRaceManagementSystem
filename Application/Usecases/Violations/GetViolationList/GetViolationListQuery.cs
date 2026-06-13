using MediatR;

namespace Application.Usecases.Violations.GetViolationList;

public sealed record GetViolationListQuery()
    : IRequest<List<ViolationListItemResponse>>;