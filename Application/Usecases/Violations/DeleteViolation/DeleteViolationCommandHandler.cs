using MediatR;

namespace Application.Usecases.Violations.DeleteViolation;

public sealed class DeleteViolationCommandHandler
    : IRequestHandler<DeleteViolationCommand, bool>
{
    public Task<bool> Handle(
        DeleteViolationCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}