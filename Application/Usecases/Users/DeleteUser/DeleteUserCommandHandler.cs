using MediatR;

namespace Application.Usecases.Users.DeleteUser;

public sealed class DeleteUserCommandHandler
    : IRequestHandler<DeleteUserCommand, bool>
{
    public Task<bool> Handle(
        DeleteUserCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}