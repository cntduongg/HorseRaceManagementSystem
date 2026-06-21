using MediatR;

namespace Application.Usecases.Legs.DeleteLeg;

public sealed class DeleteLegCommandHandler
    : IRequestHandler<DeleteLegCommand, bool>
{
    public Task<bool> Handle(
        DeleteLegCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete leg from database

        return Task.FromResult(true);
    }
}