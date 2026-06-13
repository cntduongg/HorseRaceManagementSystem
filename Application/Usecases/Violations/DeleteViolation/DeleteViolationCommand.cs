using MediatR;

namespace Application.Usecases.Violations.DeleteViolation;

public sealed record DeleteViolationCommand(
    int ViolationId
) : IRequest<bool>;