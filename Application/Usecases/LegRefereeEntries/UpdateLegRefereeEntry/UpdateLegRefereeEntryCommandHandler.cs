using MediatR;

namespace Application.Usecases.LegRefereeEntries.UpdateLegRefereeEntry;

public sealed class UpdateLegRefereeEntryCommandHandler
    : IRequestHandler<UpdateLegRefereeEntryCommand, bool>
{
    public Task<bool> Handle(
        UpdateLegRefereeEntryCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}