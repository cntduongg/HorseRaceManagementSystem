using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races;

// Kiểm tra khung giờ [start, end] của một Race có ĐÈ LỊCH (overlap) với Race khác không.
// Dùng chung cho CreateRace + UpdateRace để tránh lặp logic ở hai nơi.
//
// Hai khoảng [s1, e1] và [s2, e2] gọi là CHỒNG LẤN khi:  s1 < e2  &&  s2 < e1
// (chạm nhau đúng ở mép — ví dụ race A kết thúc 8:30, race B bắt đầu 8:30 — KHÔNG tính là chồng lấn).
//
// Chặn 2 trường hợp:
//   1. Hai race trong CÙNG một giải đấu diễn ra chồng giờ.
//   2. Hai race dùng CHUNG trọng tài chồng giờ (kể cả khác giải đấu).
internal static class RaceScheduleGuard
{
    public static async Task EnsureNoOverlapAsync(
        IApplicationDbContext context,
        int tournamentId,
        int? excludeRaceId,
        DateTime startUtc,
        DateTime endUtc,
        int referee1Id,
        int referee2Id,
        CancellationToken cancellationToken)
    {
        // Chỉ soi những race có khả năng đụng: cùng giải HOẶC dùng chung ít nhất 1 trọng tài.
        // Bỏ qua race đã Cancelled và chính nó (khi update).
        var candidates = await context.Races
            .Where(r =>
                r.Status != "Cancelled" &&
                (excludeRaceId == null || r.RaceId != excludeRaceId) &&
                (r.TournamentId == tournamentId ||
                 r.Referee1Id == referee1Id || r.Referee2Id == referee1Id ||
                 r.Referee1Id == referee2Id || r.Referee2Id == referee2Id))
            .Select(r => new
            {
                r.Name,
                r.TournamentId,
                r.ScheduledStartTime,
                r.ScheduledEndTime
            })
            .ToListAsync(cancellationToken);

        foreach (var other in candidates)
        {
            var overlaps = startUtc < other.ScheduledEndTime && other.ScheduledStartTime < endUtc;
            if (!overlaps)
                continue;

            if (other.TournamentId == tournamentId)
                throw new InvalidOperationException(
                    $"Khung giờ này chồng lấn với cuộc đua \"{other.Name}\" trong cùng giải đấu. " +
                    $"Hai cuộc đua trong một giải không được diễn ra cùng lúc.");

            // Khác giải mà vẫn lọt vào candidates ⇒ do dùng chung trọng tài.
            throw new InvalidOperationException(
                $"Trọng tài được phân công đang bận điều hành cuộc đua \"{other.Name}\" " +
                $"trong khung giờ này (ở một giải đấu khác).");
        }
    }
}
