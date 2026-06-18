using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Roles.DeleteRole;

public sealed class DeleteRoleCommandHandler
    : IRequestHandler<DeleteRoleCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteRoleCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteRoleCommand request,
        CancellationToken cancellationToken)
    {
        var role = await _context.Roles
            .FirstOrDefaultAsync(
                x => x.RoleId == request.RoleId,
                cancellationToken);

        if (role is null)
            return false;

        _context.Roles.Remove(role);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}