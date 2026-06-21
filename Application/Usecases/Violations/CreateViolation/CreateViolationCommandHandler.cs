using MediatR;

namespace Application.Usecases.Violations.CreateViolation;

public sealed class CreateViolationCommandHandler
    : IRequestHandler<CreateViolationCommand, int>
{
    public Task<int> Handle(
        CreateViolationCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Save to database

        int violationId = 1;

        return Task.FromResult(violationId);
    }
}