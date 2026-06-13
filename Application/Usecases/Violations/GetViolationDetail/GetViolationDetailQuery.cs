using MediatR;

namespace Application.Usecases.Violations.GetViolationDetail;

public sealed record GetViolationDetailQuery(
    int ViolationId
) : IRequest<ViolationDetailResponse?>;