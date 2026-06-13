using MediatR;

namespace Application.Usecases.LegRefereeEntries.CreateLegRefereeEntry;

public sealed class CreateLegRefereeEntryCommandHandler
    : IRequestHandler<CreateLegRefereeEntryCommand, long>
{
    public Task<long> Handle(
        CreateLegRefereeEntryCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Save to database

        long legRefereeEntryId = 1;

        return Task.FromResult(legRefereeEntryId);
    }
}