using MediatR;

namespace Application.Usecases.LegRefereeEntries.DeleteLegRefereeEntry;

public sealed class DeleteLegRefereeEntryCommandHandler
    : IRequestHandler<DeleteLegRefereeEntryCommand, bool>
{
    public Task<bool> Handle(
        DeleteLegRefereeEntryCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}