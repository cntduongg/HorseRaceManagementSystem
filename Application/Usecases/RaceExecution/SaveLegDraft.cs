using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// PUT /api/races/{raceId}/legs/{legIndex}/draft
// Lưu nháp kết quả leg của referee đang đăng nhập (upsert vào bảng LegRefereeDraft —
// ghi đè mỗi lần lưu). Nháp KHÔNG blind và KHÔNG chốt; bản submit chính thức nằm ở
// LegRefereeEntry (append-only). Referee quay lại đọc nháp qua referee-view.
public sealed record SaveLegDraftCommand(
    int RaceId,
    int LegIndex,
    int CurrentUserId,
    IReadOnlyList<SubmitPositionItem> Entries) : ICommand<SaveLegDraftResponse>;

public sealed record SaveLegDraftResponse(bool Saved);

public sealed class SaveLegDraftCommandHandler
    : IRequestHandler<SaveLegDraftCommand, SaveLegDraftResponse>
{
    private readonly IApplicationDbContext _context;

    public SaveLegDraftCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SaveLegDraftResponse> Handle(
        SaveLegDraftCommand request,
        CancellationToken cancellationToken)
    {
        var legNumber = request.LegIndex + 1;

        var race = await _context.Races
            .AsNoTracking()
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        var isAssignedReferee =
            request.CurrentUserId == race.Referee1Id ||
            request.CurrentUserId == race.Referee2Id;
        if (!isAssignedReferee)
            throw new UnauthorizedAccessException("Chỉ trọng tài được phân công mới lưu nháp.");

        var legExists = await _context.Legs.AnyAsync(
            l => l.RaceId == request.RaceId && l.LegNumber == legNumber, cancellationToken);
        if (!legExists)
            throw new KeyNotFoundException("Leg not found.");

        var items = request.Entries ?? new List<SubmitPositionItem>();

        // Validate không trùng thứ hạng dương (nháp vẫn cho phép thiếu).
        var positiveRanks = items
            .Where(x => x.Position > 0)
            .Select(x => x.Position)
            .ToList();
        if (positiveRanks.Count != positiveRanks.Distinct().Count())
            throw new InvalidOperationException("Thứ hạng bị trùng — mỗi vị trí chỉ gán cho 1 entry.");

        // EntryId trong nháp phải là entry đã duyệt của cuộc đua (tránh FK sai).
        var approvedEntryIds = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .Select(e => e.EntryId)
            .ToListAsync(cancellationToken);
        var approvedSet = approvedEntryIds.ToHashSet();
        if (items.Any(x => !approvedSet.Contains(x.EntryId)))
            throw new InvalidOperationException("Nháp chứa entry không thuộc danh sách đã duyệt của cuộc đua.");

        // Upsert: xóa nháp cũ của referee này ở leg này rồi ghi lại toàn bộ.
        var existing = await _context.LegRefereeDrafts
            .Where(d => d.RaceId == request.RaceId &&
                        d.LegNumber == legNumber &&
                        d.RefereeUserId == request.CurrentUserId)
            .ToListAsync(cancellationToken);
        if (existing.Count > 0)
            _context.LegRefereeDrafts.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var item in items)
        {
            _context.LegRefereeDrafts.Add(new LegRefereeDraft
            {
                RaceId = request.RaceId,
                LegNumber = legNumber,
                RefereeUserId = request.CurrentUserId,
                EntryId = item.EntryId,
                Position = item.Position,
                UpdatedAt = now
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new SaveLegDraftResponse(true);
    }
}
