using MediatR;

namespace Application.Usecases.LegRefereeEntries.UpdateLegRefereeEntry;

public sealed class UpdateLegRefereeEntryCommandHandler
    : IRequestHandler<UpdateLegRefereeEntryCommand, bool>
{
    public Task<bool> Handle(
        UpdateLegRefereeEntryCommand request,
        CancellationToken cancellationToken)
    {
        throw new InvalidOperationException(
            "LegRefereeEntry is append-only. Updates are not allowed.");
    }
}