using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Roles.UpdateRole;

public sealed class UpdateRoleCommandHandler
    : IRequestHandler<UpdateRoleCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateRoleCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateRoleCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Code))
            throw new InvalidOperationException("Code is required.");

        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Name is required.");

        var role = await _context.Roles
            .FirstOrDefaultAsync(
                x => x.RoleId == request.RoleId,
                cancellationToken);

        if (role is null)
            return false;

        role.Code = request.Code.Trim();
        role.Name = request.Name.Trim();

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}