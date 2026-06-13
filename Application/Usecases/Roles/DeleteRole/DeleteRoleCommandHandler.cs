using MediatR;

namespace Application.Usecases.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler
    : IRequestHandler<DeleteRoleCommand, bool>
{
    public Task<bool> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}