using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;

public sealed class UpdateJockeyInvitationCommandHandler
    : IRequestHandler<UpdateJockeyInvitationCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateJockeyInvitationCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.InvitationId <= 0)
            throw new InvalidOperationException("InvitationId is required.");

        if (string.IsNullOrWhiteSpace(request.Status))
            throw new InvalidOperationException("Status is required.");

        request = request with { Status = request.Status.Trim() };

        var validStatuses = new[]
        {
            "Pending",
            "Accepted",
            "Declined",
            "Confirmed",
            "Cancelled",
            "Expired"
        };

        if (!validStatuses.Contains(request.Status))
            throw new InvalidOperationException("Invalid invitation status.");

        var invitation = await _context.JockeyInvitations
            .FirstOrDefaultAsync(
                x => x.InvitationId == request.InvitationId,
                cancellationToken);

        if (invitation is null)
            return false;

        // Final states cannot be modified
        if (invitation.Status is "Cancelled" or "Expired")
            throw new InvalidOperationException(
                "Cannot update a cancelled or expired invitation.");

        // Optional: prevent duplicate update
        if (invitation.Status == request.Status)
            throw new InvalidOperationException(
                $"Invitation is already '{request.Status}'.");

        // State transition validation
        switch (invitation.Status)
        {
            case "Pending":
                if (request.Status is not ("Accepted" or "Declined" or "Cancelled"))
                    throw new InvalidOperationException(
                        "Pending invitation can only become Accepted, Declined or Cancelled.");
                break;

            case "Accepted":
                if (request.Status is not ("Confirmed" or "Cancelled"))
                    throw new InvalidOperationException(
                        "Accepted invitation can only become Confirmed or Cancelled.");
                break;

            case "Declined":
                throw new InvalidOperationException(
                    "Declined invitation cannot be updated.");

            case "Confirmed":
                throw new InvalidOperationException(
                    "Confirmed invitation cannot be updated.");
        }

        invitation.Status = request.Status;

        invitation.ResponseReason = string.IsNullOrWhiteSpace(request.ResponseReason)
            ? null
            : request.ResponseReason.Trim();

        switch (request.Status)
        {
            case "Accepted":
            case "Declined":
                invitation.RespondedAt = DateTime.UtcNow;
                break;

            case "Confirmed":
                invitation.ConfirmedAt = DateTime.UtcNow;
                break;

            case "Cancelled":
                invitation.CancelledAt = DateTime.UtcNow;
                break;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}