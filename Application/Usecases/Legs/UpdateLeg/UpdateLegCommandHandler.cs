using MediatR;

namespace Application.Usecases.Legs.UpdateLeg;

public sealed class UpdateLegCommandHandler
    : IRequestHandler<UpdateLegCommand, bool>
{
    public Task<bool> Handle(
        UpdateLegCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update leg in database

        return Task.FromResult(true);
    }
}