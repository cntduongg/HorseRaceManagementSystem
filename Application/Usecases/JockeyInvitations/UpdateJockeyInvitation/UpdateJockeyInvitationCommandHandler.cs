using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;

public sealed class UpdateJockeyInvitationCommandHandler
    : IRequestHandler<UpdateJockeyInvitationCommand, bool>
{
    private static readonly string[] ValidStatuses =
        { "Pending", "Accepted", "Declined", "Confirmed", "Cancelled", "Expired" };

    private static readonly string[] JockeyActions = { "Accepted", "Declined" };
    private static readonly string[] OwnerActions = { "Confirmed", "Cancelled" };

    private readonly IApplicationDbContext _context;

    public UpdateJockeyInvitationCommandHandler(IApplicationDbContext context)
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

        var status = request.Status.Trim();

        if (!ValidStatuses.Contains(status))
            throw new InvalidOperationException("Invalid invitation status.");

        var invitation = await _context.JockeyInvitations
            .FirstOrDefaultAsync(x => x.InvitationId == request.InvitationId, cancellationToken);

        if (invitation is null)
            throw new KeyNotFoundException($"Invitation {request.InvitationId} not found.");

        // ----------------------------------------------------
        // ROLE CHECK (IMPORTANT FIX)
        // ----------------------------------------------------
        if (request.CurrentUserId > 0)
        {
            if (JockeyActions.Contains(status) &&
                request.CurrentUserId != invitation.JockeyId)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ nài được mời mới có thể Accept/Decline.");
            }

            if (OwnerActions.Contains(status) &&
                request.CurrentUserId != invitation.HorseOwnerId)
            {
                throw new UnauthorizedAccessException(
                    "Chỉ chủ ngựa mới có thể Confirm/Cancel.");
            }
        }

        if (invitation.Status is "Cancelled" or "Expired")
            throw new InvalidOperationException("Invitation already cancelled/expired.");

        if (invitation.Status == status)
            throw new InvalidOperationException($"Already in status '{status}'.");

        // ----------------------------------------------------
        // STATE VALIDATION
        // ----------------------------------------------------
        switch (invitation.Status)
        {
            case "Pending":
                if (status is not ("Accepted" or "Declined" or "Cancelled"))
                    throw new InvalidOperationException(
                        "Pending -> Accepted/Declined/Cancelled only.");
                break;

            case "Accepted":
                if (status is not ("Confirmed" or "Cancelled"))
                    throw new InvalidOperationException(
                        "Accepted -> Confirmed/Cancelled only.");
                break;

            case "Declined":
                throw new InvalidOperationException("Declined cannot be updated.");

            case "Confirmed":
                throw new InvalidOperationException("Already confirmed.");
        }

        var now = DateTime.UtcNow;

        // ----------------------------------------------------
        // UPDATE BASIC INFO
        // ----------------------------------------------------
        invitation.ResponseReason = string.IsNullOrWhiteSpace(request.ResponseReason)
            ? null
            : request.ResponseReason.Trim();

        // ----------------------------------------------------
        // STATE TRANSITION
        // ----------------------------------------------------
        switch (status)
        {
            case "Accepted":
                invitation.RespondedAt = now;
                break;

            case "Declined":
                invitation.RespondedAt = now;
                break;

            case "Confirmed":

                // ------------------------------------------------
                // 1 jockey only 1 horse per race
                // ------------------------------------------------
                var jockeyConflict = await _context.JockeyInvitations.AnyAsync(
                    x => x.RaceId == invitation.RaceId
                         && x.JockeyId == invitation.JockeyId
                         && x.HorseId != invitation.HorseId
                         && x.Status == "Confirmed",
                    cancellationToken);

                if (jockeyConflict)
                    throw new InvalidOperationException(
                        "Jockey already confirmed another horse in this race.");

                invitation.ConfirmedAt = now;

                // ------------------------------------------------
                // Cancel other invitations (same horse + race)
                // ------------------------------------------------
                var others = await _context.JockeyInvitations
                    .Where(x =>
                        x.RaceId == invitation.RaceId &&
                        x.HorseId == invitation.HorseId &&
                        x.InvitationId != invitation.InvitationId &&
                        (x.Status == "Pending" || x.Status == "Accepted"))
                    .ToListAsync(cancellationToken);

                foreach (var o in others)
                {
                    o.Status = "Cancelled";
                    o.CancelledAt = now;
                    o.ResponseReason = "Auto-cancelled after confirmation.";
                }
                break;

            case "Cancelled":
                invitation.CancelledAt = now;
                break;
        }

        // ----------------------------------------------------
        // FINAL STATUS UPDATE
        // ----------------------------------------------------
        invitation.Status = status;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}