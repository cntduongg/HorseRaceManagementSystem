using MediatR;

namespace Application.Usecases.Entries.UpdateEntry;

public sealed class UpdateEntryCommandHandler
    : IRequestHandler<UpdateEntryCommand, bool>
{
    public Task<bool> Handle(
        UpdateEntryCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}