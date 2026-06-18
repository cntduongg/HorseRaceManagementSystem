using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.DeleteViolation;

public sealed class DeleteViolationCommandHandler
    : IRequestHandler<DeleteViolationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteViolationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteViolationCommand request,
        CancellationToken cancellationToken)
    {
        var violation = await _context.Violations
            .FirstOrDefaultAsync(x => x.ViolationId == request.ViolationId, cancellationToken);

        if (violation is null)
            return false;

        _context.Violations.Remove(violation);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}