using MediatR;

namespace Application.Usecases.LegOfficialResults.UpdateLegOfficialResult;

public sealed class UpdateLegOfficialResultCommandHandler
    : IRequestHandler<UpdateLegOfficialResultCommand, bool>
{
    public Task<bool> Handle(
        UpdateLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}