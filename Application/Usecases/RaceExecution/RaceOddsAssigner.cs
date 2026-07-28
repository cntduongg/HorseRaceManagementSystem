using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

/// <summary>
/// Nơi DUY NHẤT trong hệ thống ghi <see cref="Entry.Odds"/> và <see cref="Entry.GateNumber"/>.
///
/// Odds là TĨNH: tính đúng một lần lúc Admin đóng đăng ký, dựa trên lịch sử về nhất của ngựa,
/// rồi đứng yên cho tới khi race kết thúc. Không có tầng giá thứ hai, không có biên nhà cái,
/// không ai sửa được — con số spectator nhìn thấy chính là con số khóa vào Prediction và dùng
/// để trả thưởng (payout = BetAmount × OddsLocked1).
/// </summary>
public static class RaceOddsAssigner
{
    /// <summary>
    /// Odds = 1 / winRate (clamp 1.1..25). Laplace prior để mẫu nhỏ không cực đoan:
    /// ngựa mới toanh (0 lần đua) không bị thành 25x, ngựa thắng 1/1 không thành 1.0x.
    /// </summary>
    public static decimal OddsFor(int firsts, int total, int fieldSize)
    {
        var n = Math.Max(fieldSize, 2);
        double winRate = total > 0
            ? (firsts + 1.0) / (total + n)
            : 1.0 / n;

        var odds = (decimal)Math.Round(1.0 / Math.Max(winRate, 0.04), 2);
        return Math.Clamp(odds, 1.1m, 25m);
    }

    /// <summary>
    /// Tính Odds + đánh lại GateNumber 1..N cho các Entry đã duyệt của một race.
    /// </summary>
    /// <param name="approved">
    /// Entry <c>Approved</c> của race, đã được context track và đã sort (SubmittedAt, EntryId).
    /// </param>
    /// <remarks>
    /// ⚠️ Caller PHẢI đang ở trong transaction — hàm gọi <c>SaveChangesAsync</c> giữa chừng.
    ///
    /// Ghi gate làm HAI PHA vì index <c>(RaceId, GateNumber)</c> là unique index có filter,
    /// PostgreSQL không defer được nên nó kiểm tra theo từng statement. Mà
    /// <c>ApproveEntryCommandHandler</c> đã gán gate theo thứ tự DUYỆT, còn ở đây đánh lại
    /// 1..N theo thứ tự NỘP: Admin duyệt lệch thứ tự nộp là ra một hoán vị của {1..N} và
    /// EF sẽ đâm vào 23505 ngay UPDATE đầu tiên. Xóa hết về null trước rồi mới gán lại thì
    /// pha 1 không thể va (filter bỏ qua NULL), đồng thời giải phóng luôn gate mà các row
    /// Cancelled/Rejected/Withdrawn đang chiếm.
    /// </remarks>
    public static async Task AssignAsync(
        IApplicationDbContext context,
        int raceId,
        IReadOnlyList<Entry> approved,
        DateTime now,
        CancellationToken cancellationToken)
    {
        // Pha 1 — trả toàn bộ gate của race về null (kể cả entry đã hủy/bị từ chối).
        var occupied = await context.Entries
            .Where(e => e.RaceId == raceId && e.GateNumber != null)
            .ToListAsync(cancellationToken);

        foreach (var entry in occupied)
            entry.GateNumber = null;

        await context.SaveChangesAsync(cancellationToken);

        // Pha 2 — tính odds theo lịch sử về nhất & gán gate 1..N theo thứ tự nộp.
        var horseIds = approved.Select(e => e.HorseId).Distinct().ToList();
        var history = await context.RaceResults
            .Where(r => horseIds.Contains(r.Entry.HorseId) && r.FinalPosition != null)
            .Select(r => new { r.Entry.HorseId, r.FinalPosition })
            .ToListAsync(cancellationToken);

        var gate = 1;
        foreach (var entry in approved)
        {
            var rows = history.Where(h => h.HorseId == entry.HorseId).ToList();
            var firsts = rows.Count(h => h.FinalPosition == 1);

            entry.Odds = OddsFor(firsts, rows.Count, approved.Count);
            entry.GateNumber = gate++;
            entry.UpdatedAt = now;
        }
    }
}
