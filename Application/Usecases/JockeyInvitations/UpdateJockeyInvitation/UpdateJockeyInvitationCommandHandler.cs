using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;

public sealed class UpdateJockeyInvitationCommandHandler
    : IRequestHandler<UpdateJockeyInvitationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateJockeyInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        var invitation = await _context.JockeyInvitations.FirstOrDefaultAsync(
            x => x.InvitationId == request.InvitationId,
            cancellationToken);

        if (invitation is null)
            return false;

        // prevent invalid state transition
        if (invitation.Status == "Cancelled" || invitation.Status == "Expired")
            throw new InvalidOperationException("Cannot update cancelled/expired invitation.");

        invitation.Status = request.Status;

        if (!string.IsNullOrWhiteSpace(request.ResponseReason))
            invitation.ResponseReason = request.ResponseReason;

        if (request.Status == "Accepted" || request.Status == "Declined")
            invitation.RespondedAt = DateTime.UtcNow;

        if (request.Status == "Confirmed")
            invitation.ConfirmedAt = DateTime.UtcNow;

        if (request.Status == "Cancelled")
            invitation.CancelledAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}