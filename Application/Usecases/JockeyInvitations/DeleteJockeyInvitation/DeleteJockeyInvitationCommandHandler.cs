using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.DeleteJockeyInvitation;

public sealed class DeleteJockeyInvitationCommandHandler
    : IRequestHandler<DeleteJockeyInvitationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public DeleteJockeyInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        DeleteJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await _context.JockeyInvitations.FirstOrDefaultAsync(
            x => x.InvitationId == request.InvitationId,
            cancellationToken);

        if (invitation is null)
            return false;

        // business rule: cannot delete if already confirmed
        if (invitation.Status == "Confirmed")
            throw new InvalidOperationException("Cannot delete confirmed invitation.");

        _context.JockeyInvitations.Remove(invitation);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}