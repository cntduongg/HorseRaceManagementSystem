using Application.Common;
using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.RaceModeration;

// ⚠️ TÙY CHỌN — KHÔNG thuộc 8 Main Flow. Race không có cổng "duyệt" trong domain
// (vòng đời chuẩn: Scheduled → open/close-registration → start → publish → Finished).
// 3 endpoint dưới đây phục vụ helper FE có sẵn (approveRace/rejectRace/finishRace),
// map vào state machine một cách an toàn nhất:
//   - Approve : chỉ xác nhận Race đang Scheduled (acknowledgement, KHÔNG đổi trạng thái).
//   - Reject  : Cancel Race (→ Cancelled) kèm lý do — dùng khi hủy lịch đua.
//   - Finish  : PendingResult → Finished (đóng thủ công). LƯU Ý: KHÔNG chạy settlement —
//               muốn cộng Prize/quyết toán cược phải dùng Publish (Flow 8).

public sealed record RaceModerationResponse(int RaceId, string Status, string Message);

// ── Approve ──────────────────────────────────────────────────────────
public sealed record ApproveRaceCommand(int RaceId, int AdminId)
    : ICommand<RaceModerationResponse>;

public sealed class ApproveRaceCommandHandler
    : IRequestHandler<ApproveRaceCommand, RaceModerationResponse>
{
    private readonly IApplicationDbContext _context;
    public ApproveRaceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<RaceModerationResponse> Handle(ApproveRaceCommand request, CancellationToken ct)
    {
        var race = await _context.Races.FirstOrDefaultAsync(r => r.RaceId == request.RaceId, ct)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RaceScheduled)
            throw new InvalidOperationException(
                $"Only a Scheduled race can be approved (current: {race.Status}).");

        race.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return new RaceModerationResponse(race.RaceId, race.Status, "The race has been confirmed (Scheduled).");
    }
}

// ── Reject (Cancel) ──────────────────────────────────────────────────
public sealed record RejectRaceCommand(int RaceId, int AdminId, string? Reason)
    : ICommand<RaceModerationResponse>;

public sealed class RejectRaceCommandHandler
    : IRequestHandler<RejectRaceCommand, RaceModerationResponse>
{
    private readonly IApplicationDbContext _context;
    public RejectRaceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<RaceModerationResponse> Handle(RejectRaceCommand request, CancellationToken ct)
    {
        var race = await _context.Races.FirstOrDefaultAsync(r => r.RaceId == request.RaceId, ct)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status is RaceExecutionConstants.RaceFinished or RaceExecutionConstants.RaceCancelled)
            throw new InvalidOperationException(
                $"Cannot cancel a race in status {race.Status}.");

        race.Status = RaceExecutionConstants.RaceCancelled;
        race.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return new RaceModerationResponse(race.RaceId, race.Status, "The race has been cancelled.");
    }
}

// ── Finish ───────────────────────────────────────────────────────────
public sealed record FinishRaceCommand(int RaceId, int AdminId)
    : ICommand<RaceModerationResponse>;

public sealed class FinishRaceCommandHandler
    : IRequestHandler<FinishRaceCommand, RaceModerationResponse>
{
    private readonly IApplicationDbContext _context;
    public FinishRaceCommandHandler(IApplicationDbContext context) => _context = context;

    public async Task<RaceModerationResponse> Handle(FinishRaceCommand request, CancellationToken ct)
    {
        var race = await _context.Races.FirstOrDefaultAsync(r => r.RaceId == request.RaceId, ct)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RacePendingResult)
            throw new InvalidOperationException(
                $"Only a PendingResult race can be Finished (current: {race.Status}). " +
                "To award Prize points/settle bets, use Publish instead of Finish.");

        race.Status = RaceExecutionConstants.RaceFinished;
        race.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync(ct);

        return new RaceModerationResponse(race.RaceId, race.Status,
            "The race is closed (Finished). Note: settlement has not run — use Publish if you need to award Prize/settle bets.");
    }
}
