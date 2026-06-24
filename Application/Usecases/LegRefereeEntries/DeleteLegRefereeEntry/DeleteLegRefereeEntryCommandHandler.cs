using MediatR;

namespace Application.Usecases.LegRefereeEntries.DeleteLegRefereeEntry;

public sealed class DeleteLegRefereeEntryCommandHandler
    : IRequestHandler<DeleteLegRefereeEntryCommand, bool>
{
    public Task<bool> Handle(
        DeleteLegRefereeEntryCommand request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "LegRefereeEntry is append-only and cannot be deleted.");
    }
}