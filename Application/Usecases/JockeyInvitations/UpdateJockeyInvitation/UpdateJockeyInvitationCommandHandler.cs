using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.UpdateJockeyInvitation;

// Flow 2 — Phản hồi/Xác nhận lời mời nài.
//  • Accepted/Declined: chỉ chính nài được mời.
//  • Confirmed/Cancelled: chỉ chủ ngựa.
//  • Khi Confirmed → tự Cancel mọi invitation active khác cùng (Horse+Race);
//    chặn 1 nài được confirm cho 2 horse khác nhau trong cùng Race.
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
            return false;

        // ── Phân quyền theo hành động ──
        if (request.CurrentUserId > 0)
        {
            if (JockeyActions.Contains(status) && request.CurrentUserId != invitation.JockeyId)
                throw new UnauthorizedAccessException("Chỉ nài được mời mới có thể Accept/Decline.");
            if (OwnerActions.Contains(status) && request.CurrentUserId != invitation.HorseOwnerId)
                throw new UnauthorizedAccessException("Chỉ chủ ngựa mới có thể Confirm/Cancel.");
        }

        if (invitation.Status is "Cancelled" or "Expired")
            throw new InvalidOperationException("Không thể cập nhật lời mời đã hủy/hết hạn.");
        if (invitation.Status == status)
            throw new InvalidOperationException($"Lời mời đã ở trạng thái '{status}'.");

        // ── Hợp lệ chuyển trạng thái ──
        switch (invitation.Status)
        {
            case "Pending":
                if (status is not ("Accepted" or "Declined" or "Cancelled"))
                    throw new InvalidOperationException(
                        "Pending chỉ chuyển sang Accepted, Declined hoặc Cancelled.");
                break;
            case "Accepted":
                if (status is not ("Confirmed" or "Cancelled"))
                    throw new InvalidOperationException(
                        "Accepted chỉ chuyển sang Confirmed hoặc Cancelled.");
                break;
            case "Declined":
                throw new InvalidOperationException("Lời mời đã Declined không thể cập nhật.");
            case "Confirmed":
                throw new InvalidOperationException("Lời mời đã Confirmed không thể cập nhật.");
        }

        var now = DateTime.UtcNow;
        invitation.ResponseReason = string.IsNullOrWhiteSpace(request.ResponseReason)
            ? null : request.ResponseReason.Trim();

        switch (status)
        {
            case "Accepted":
            case "Declined":
                invitation.RespondedAt = now;
                break;

            case "Confirmed":
                // 1 nài chỉ được confirm cho 1 horse mỗi Race.
                var jockeyConfirmedElsewhere = await _context.JockeyInvitations.AnyAsync(
                    x => x.RaceId == invitation.RaceId &&
                         x.JockeyId == invitation.JockeyId &&
                         x.HorseId != invitation.HorseId &&
                         x.Status == "Confirmed",
                    cancellationToken);
                if (jockeyConfirmedElsewhere)
                    throw new InvalidOperationException(
                        "Nài này đã được xác nhận cho một con ngựa khác trong cùng cuộc đua.");

                invitation.ConfirmedAt = now;

                // Tự hủy các invitation active khác cùng (Horse + Race).
                var others = await _context.JockeyInvitations
                    .Where(x => x.RaceId == invitation.RaceId &&
                                x.HorseId == invitation.HorseId &&
                                x.InvitationId != invitation.InvitationId &&
                                (x.Status == "Pending" || x.Status == "Accepted"))
                    .ToListAsync(cancellationToken);
                foreach (var o in others)
                {
                    o.Status = "Cancelled";
                    o.CancelledAt = now;
                    o.ResponseReason ??= "Tự hủy: đã xác nhận nài khác cho ngựa này.";
                }
                break;

            case "Cancelled":
                invitation.CancelledAt = now;
                break;
        }

        invitation.Status = status;

        await _context.SaveChangesAsync(cancellationToken);
        return true;
    }
}
