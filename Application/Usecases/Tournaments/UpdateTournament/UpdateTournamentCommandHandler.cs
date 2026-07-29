using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Tournaments.UpdateTournament;

public sealed class UpdateTournamentCommandHandler
    : IRequestHandler<UpdateTournamentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateTournamentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Tournament name is required.");

        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("StartDate cannot be later than EndDate.");

        // Trước đây gán thẳng request.Status vào entity, không kiểm gì: gõ sai ("Onging", "ongoing",
        // "Active"…) là ghi luôn vào DB, FE không map được nên rơi về nhãn mặc định, và worker
        // auto-status cũng bỏ qua vì không nhận ra giá trị đó.
        var status = request.Status?.Trim();
        if (!TournamentStatus.IsValid(status))
            throw new InvalidOperationException(
                $"Status must be one of: {string.Join(", ", TournamentStatus.All)}.");

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(
                x => x.TournamentId == request.TournamentId,
                cancellationToken);

        if (tournament is null)
            return false;

        // Đóng giải (Finished/Cancelled) trong lúc race con còn sống là sai lệch dữ liệu, không chỉ
        // sai hiển thị: vòng đời Race hoàn toàn ĐỘC LẬP với Tournament.Status — start, nhận cược,
        // khóa odds, publish đều chỉ đọc Race.Status/Entry.Status, không đứa nào đọc Tournament.Status.
        // Nên set giải về "Cancelled" khi bên trong có race InProgress thì race đó vẫn chạy tiếp, vẫn
        // nhận cược, vẫn trả thưởng bình thường, trong khi UI báo giải đã hủy.
        // Ở đây chặn ngay cổng vào (không cascade — hủy race là hành động riêng, có ràng buộc riêng).
        // Chỉ kiểm khi status THỰC SỰ đổi sang terminal: sửa tên/mô tả của giải đã Finished/Cancelled
        // (gửi lại nguyên status cũ) vẫn phải chạy được.
        var isClosingTournament =
            TournamentStatus.IsTerminal(status) &&
            !string.Equals(status, tournament.Status, StringComparison.Ordinal);

        if (isClosingTournament)
        {
            // Race "còn sống" = mọi status chưa terminal: Scheduled | InProgress | Paused | PendingResult.
            var liveRaces = await _context.Races
                .Where(r => r.TournamentId == request.TournamentId &&
                            r.Status != RaceStatus.Finished &&
                            r.Status != RaceStatus.Cancelled)
                .OrderBy(r => r.ScheduledStartTime)
                .Select(r => new { r.RaceId, r.Name, r.Status })
                .ToListAsync(cancellationToken);

            if (liveRaces.Count > 0)
            {
                var detail = string.Join(
                    ", ",
                    liveRaces.Take(5).Select(r => $"#{r.RaceId} {r.Name} ({r.Status})"));

                if (liveRaces.Count > 5)
                    detail += $", ... (+{liveRaces.Count - 5} more)";

                throw new InvalidOperationException(
                    $"Tournament cannot be set to {status} while {liveRaces.Count} race(s) are still active: " +
                    $"{detail}. Finish or cancel those races first.");
            }
        }

        tournament.Name = request.Name.Trim();
        tournament.Description = request.Description;
        tournament.Location = request.Location;
        tournament.StartDate = request.StartDate;
        tournament.EndDate = request.EndDate;
        // Admin vẫn tự quyết được (kể cả Finished sớm trước EndDate, hoặc Cancelled) — miễn là đã
        // qua guard bên trên, tức không còn race con nào đang sống. Worker chỉ đẩy TIẾN từ
        // Draft/Open/Ongoing và không bao giờ đụng vào Cancelled/Finished, nên lựa chọn của Admin
        // không bị ghi đè ngược.
        tournament.Status = status!;
        tournament.CancelReason = request.CancelReason;
        tournament.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}