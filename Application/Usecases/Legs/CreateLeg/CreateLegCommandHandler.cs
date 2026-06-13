using MediatR;

namespace Application.Usecases.Legs.CreateLeg;

public sealed class CreateLegCommandHandler
    : IRequestHandler<CreateLegCommand, bool>
{
    public Task<bool> Handle(
        CreateLegCommand request,
        CancellationToken cancellationToken)
    {
        if (request.LegNumber < 1 || request.LegNumber > 10)
        {
            throw new InvalidOperationException(
                "LegNumber must be between 1 and 10.");
        }

        // TODO: Save leg into database

        return Task.FromResult(true);
    }
}