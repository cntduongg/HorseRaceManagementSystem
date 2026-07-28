using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// POST /api/races/{raceId}/publish-odds — Admin công bố odds cho Spectator (Flow 7).
// Đây là cổng mở cược: trước khi bấm nút này spectator không thấy giá và không đặt được lệnh.
// Idempotent — publish lại chỉ cập nhật thời điểm, không đụng gì tới cược đã đặt.
public sealed record PublishRaceOddsCommand(int RaceId) : ICommand<PublishRaceOddsResponse>;

public sealed record PublishRaceOddsResponse(
    int RaceId,
    DateTime OddsPublishedAt,
    int PublishedEntries,
    bool WasAlreadyPublished);

public sealed class PublishRaceOddsCommandHandler
    : IRequestHandler<PublishRaceOddsCommand, PublishRaceOddsResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public PublishRaceOddsCommandHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<PublishRaceOddsResponse> Handle(
        PublishRaceOddsCommand request,
        CancellationToken cancellationToken)
    {
        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RaceScheduled)
            throw new InvalidOperationException(
                $"Odds can only be published while the race is Scheduled (current: {race.Status}).");

        if (race.OddsComputedAt is null)
            throw new InvalidOperationException(
                "Registration must be closed before odds can be published.");

        if (race.BettingLockedAt is not null)
            throw new InvalidOperationException(
                "Betting is already locked for this race — odds can no longer be published.");

        var entries = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved)
            .ToListAsync(cancellationToken);

        if (entries.Count < 2)
            throw new InvalidOperationException(
                "At least 2 approved entries are required to publish odds.");

        // Entry nào chưa có giá công bố thì lấp bằng công thức mặc định thay vì chặn Admin —
        // giá 0 lọt ra tới bảng cược nghĩa là spectator cược mà chắc chắn không được trả.
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        foreach (var entry in entries.Where(e => e.PublishedOdds <= 0))
        {
            entry.PublishedOdds = RaceOddsPolicy.PublishedFromSuggested(entry.Odds);
            entry.UpdatedAt = now;
        }

        var wasAlreadyPublished = race.OddsPublishedAt is not null;

        race.OddsPublishedAt = now;
        race.UpdatedAt = now;

        await _context.SaveChangesAsync(cancellationToken);

        return new PublishRaceOddsResponse(
            race.RaceId, now, entries.Count, wasAlreadyPublished);
    }
}
