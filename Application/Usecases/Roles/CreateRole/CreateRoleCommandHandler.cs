using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

namespace Application.Usecases.Roles.CreateRole;

public sealed class CreateRoleCommandHandler
    : IRequestHandler<CreateRoleCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateRoleCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");

        var role = new Role
        {
            Code = request.Code.Trim(),
            Name = request.Name.Trim()
        };

        _context.Roles.Add(role);

        await _context.SaveChangesAsync(cancellationToken);

        return role.RoleId;
    }
}