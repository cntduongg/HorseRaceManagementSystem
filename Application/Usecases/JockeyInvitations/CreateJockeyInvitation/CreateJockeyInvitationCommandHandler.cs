using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.CreateJockeyInvitation;

// Flow 2 — Owner mời nài cho 1 cặp (Race + Horse).
// Ràng buộc: horse Approved & thuộc owner; race Scheduled; nài hợp lệ (role JOCKEY + có License/Weight);
// 1 invitation active / (Jockey+Horse+Race).
public sealed class CreateJockeyInvitationCommandHandler
    : IRequestHandler<CreateJockeyInvitationCommand, int>
{
    private const int JockeyRoleId = 2;
    private static readonly string[] ActiveStatuses = { "Pending", "Accepted", "Confirmed" };

    private readonly IApplicationDbContext _context;

    public CreateJockeyInvitationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateJockeyInvitationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.HorseOwnerId <= 0)
            throw new InvalidOperationException("HorseOwnerId is required.");
        if (request.JockeyId <= 0)
            throw new InvalidOperationException("JockeyId is required.");
        if (request.HorseId <= 0)
            throw new InvalidOperationException("HorseId is required.");
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is required.");
        if (request.HorseOwnerId == request.JockeyId)
            throw new InvalidOperationException("Chủ ngựa và nài phải khác nhau.");

        // Ngựa: tồn tại, đã duyệt, thuộc sở hữu owner.
        var horse = await _context.Horses
            .FirstOrDefaultAsync(h => h.HorseId == request.HorseId, cancellationToken)
            ?? throw new InvalidOperationException("Ngựa không tồn tại.");
        if (horse.OwnerId != request.HorseOwnerId)
            throw new InvalidOperationException("Bạn không sở hữu con ngựa này.");
        if (horse.Status != "Approved")
            throw new InvalidOperationException("Chỉ mời nài cho ngựa đã được duyệt.");

        // Race: tồn tại & đang Scheduled.
        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Cuộc đua không tồn tại.");
        if (race.Status != "Scheduled")
            throw new InvalidOperationException("Chỉ mời nài khi cuộc đua đang Scheduled.");

        // Nài: tồn tại, role JOCKEY, hồ sơ đủ License + Weight.
        var jockey = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.JockeyId, cancellationToken)
            ?? throw new InvalidOperationException("Nài không tồn tại.");
        if (jockey.RoleId != JockeyRoleId)
            throw new InvalidOperationException("Người được mời không phải là nài.");
        if (string.IsNullOrWhiteSpace(jockey.LicenseNumber) || jockey.Weight is null or <= 0)
            throw new InvalidOperationException("Nài chưa hoàn thiện hồ sơ (License + Weight).");

        // Không trùng invitation active cho (jockey + horse + race).
        var duplicate = await _context.JockeyInvitations.AnyAsync(
            x => x.JockeyId == request.JockeyId &&
                 x.HorseId == request.HorseId &&
                 x.RaceId == request.RaceId &&
                 ActiveStatuses.Contains(x.Status),
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("Đã có lời mời đang hoạt động cho nài này ở cặp Race+Horse.");

        var invitation = new JockeyInvitation
        {
            HorseOwnerId = request.HorseOwnerId,
            JockeyId = request.JockeyId,
            HorseId = request.HorseId,
            RaceId = request.RaceId,
            Message = request.Message?.Trim(),
            Status = "Pending",
            SentAt = DateTime.UtcNow
        };

        _context.JockeyInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        return invitation.InvitationId;
    }
}
