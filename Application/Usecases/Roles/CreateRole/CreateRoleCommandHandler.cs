using MediatR;

namespace Application.Usecases.Roles.CreateRole;

public sealed class CreateRoleCommandHandler
    : IRequestHandler<CreateRoleCommand, int>
{
    public Task<int> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");

        // TODO: Save to database

        var roleId = 1;
        return Task.FromResult(roleId);
    }
}