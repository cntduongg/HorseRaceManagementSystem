using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// PUT /api/races/{raceId}/legs/{legIndex}/draft
// Lưu nháp kết quả leg. Hiện chưa có bảng draft riêng → chỉ validate phía server và trả về OK
// (FE giữ nháp trong state). Tách riêng để sau này dễ persist nếu cần.
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

        // Validate không trùng thứ hạng dương (nháp vẫn cho phép thiếu).
        var positiveRanks = (request.Entries ?? new List<SubmitPositionItem>())
            .Where(x => x.Position > 0)
            .Select(x => x.Position)
            .ToList();
        if (positiveRanks.Count != positiveRanks.Distinct().Count())
            throw new InvalidOperationException("Thứ hạng bị trùng — mỗi vị trí chỉ gán cho 1 entry.");

        return new SaveLegDraftResponse(true);
    }
}
