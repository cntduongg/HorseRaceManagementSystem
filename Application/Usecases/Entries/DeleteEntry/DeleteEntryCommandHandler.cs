using MediatR;

namespace Application.Usecases.Entries.DeleteEntry;

public sealed class DeleteEntryCommandHandler
    : IRequestHandler<DeleteEntryCommand, bool>
{
    public Task<bool> Handle(
        DeleteEntryCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}