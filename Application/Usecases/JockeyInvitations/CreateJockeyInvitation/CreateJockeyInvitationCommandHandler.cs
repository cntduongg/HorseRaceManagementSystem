using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.JockeyInvitations.CreateJockeyInvitation;

// Flow 2 — Owner mời nài cho 1 cặp (Race + Horse).
// Ràng buộc: horse Approved & thuộc owner; race Scheduled + registration đang mở; nài hợp lệ
// (role JOCKEY + có License/Weight); 1 invitation active / (Jockey+Horse+Race).
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
            throw new InvalidOperationException("Horse owner and jockey must be different.");

        // Ngựa: tồn tại, đã duyệt, thuộc sở hữu owner.
        var horse = await _context.Horses
            .FirstOrDefaultAsync(h => h.HorseId == request.HorseId, cancellationToken)
            ?? throw new InvalidOperationException("Horse does not exist.");
        if (horse.OwnerId != request.HorseOwnerId)
            throw new InvalidOperationException("You do not own this horse.");
        if (horse.Status != "Approved")
            throw new InvalidOperationException("You can only invite a jockey for an approved horse.");

        // Race: tồn tại & đang Scheduled.
        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race does not exist.");
        if (race.Status != "Scheduled")
            throw new InvalidOperationException("You can only invite a jockey while the race is Scheduled.");

        // Cửa đăng ký phải đang mở — cùng điều kiện & cùng message với CreateEntry.
        // Trước đây chỗ này chỉ check Status: race Scheduled mà Admin CHƯA bấm Open Registration
        // vẫn mời nài được, chỉ tới bước nộp Entry mới bị chặn ⇒ đẻ ra một đống invitation
        // Pending/Accepted cho race chưa nên nhận thao tác gì, và lệch hẳn với CreateEntry.
        var now = DateTime.UtcNow;

        // Registration chưa mở
        if (race.RegistrationOpenAt == null)
            throw new InvalidOperationException("Registration has not been opened.");

        // Registration đã đóng
        if (race.RegistrationCloseAt != null && race.RegistrationCloseAt <= now)
            throw new InvalidOperationException("Registration is closed.");

        // Nài: tồn tại, role JOCKEY, hồ sơ đủ License + Weight.
        var jockey = await _context.Users
            .FirstOrDefaultAsync(u => u.UserId == request.JockeyId, cancellationToken)
            ?? throw new InvalidOperationException("Jockey does not exist.");
        if (jockey.RoleId != JockeyRoleId)
            throw new InvalidOperationException("The invited user is not a jockey.");
        if (string.IsNullOrWhiteSpace(jockey.LicenseNumber) || jockey.Weight is null or <= 0)
            throw new InvalidOperationException("The jockey has not completed their profile (License + Weight).");

        //---------------------------------------------------------
        // Jockey already confirmed another horse in this race
        //---------------------------------------------------------

        var confirmedElsewhere = await _context.JockeyInvitations.AnyAsync(
            x =>
                x.RaceId == request.RaceId &&
                x.JockeyId == request.JockeyId &&
                x.Status == "Confirmed",
            cancellationToken);

        if (confirmedElsewhere)
        {
            throw new InvalidOperationException(
                "The jockey has already confirmed riding another horse in this race.");
        }
        // Không trùng invitation active cho (jockey + horse + race).
        var duplicate = await _context.JockeyInvitations.AnyAsync(
            x => x.JockeyId == request.JockeyId &&
                 x.HorseId == request.HorseId &&
                 x.RaceId == request.RaceId &&
                 ActiveStatuses.Contains(x.Status),
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("There is already an active invitation for this jockey for the same Race+Horse pair.");

        var invitation = new JockeyInvitation
        {
            HorseOwnerId = request.HorseOwnerId,
            JockeyId = request.JockeyId,
            HorseId = request.HorseId,
            RaceId = request.RaceId,
            Message = request.Message?.Trim(),
            Status = "Pending",
            SentAt = now
        };

        _context.JockeyInvitations.Add(invitation);
        await _context.SaveChangesAsync(cancellationToken);

        return invitation.InvitationId;
    }
}
