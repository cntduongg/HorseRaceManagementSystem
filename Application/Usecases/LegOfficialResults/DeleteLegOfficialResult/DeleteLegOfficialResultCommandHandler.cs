using MediatR;

namespace Application.Usecases.LegOfficialResults.DeleteLegOfficialResult;

public sealed class DeleteLegOfficialResultCommandHandler
    : IRequestHandler<DeleteLegOfficialResultCommand, bool>
{
    public Task<bool> Handle(
        DeleteLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}