using MediatR;

namespace Application.Usecases.Roles.UpdateRole;

public sealed class UpdateRoleCommandHandler
    : IRequestHandler<UpdateRoleCommand, bool>
{
    public Task<bool> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");

        // TODO: Update database

        return Task.FromResult(true);
    }
}