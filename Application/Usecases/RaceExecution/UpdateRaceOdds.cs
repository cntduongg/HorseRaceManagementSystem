using Application.Common;
using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceExecution;

// PUT /api/races/{raceId}/odds — Admin sửa odds trong modal điều chỉnh (Flow 3+7).
// Gửi entry nào thì sửa entry đó; entry không có trong body giữ nguyên.
public sealed record UpdateRaceOddsCommand(
    int RaceId,
    List<RaceOddsAdjustment> Entries) : ICommand<UpdateRaceOddsResponse>;

// SuggestedOdds = null → giữ nguyên giá máy đề xuất.
// PublishedOdds = null → tính lại theo công thức (đề xuất − 10%).
public sealed record RaceOddsAdjustment(
    int EntryId,
    decimal? SuggestedOdds,
    decimal? PublishedOdds);

public sealed record UpdateRaceOddsResponse(int RaceId, int UpdatedEntries);

public sealed class UpdateRaceOddsCommandHandler
    : IRequestHandler<UpdateRaceOddsCommand, UpdateRaceOddsResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;

    public UpdateRaceOddsCommandHandler(IApplicationDbContext context, TimeProvider timeProvider)
    {
        _context = context;
        _timeProvider = timeProvider;
    }

    public async Task<UpdateRaceOddsResponse> Handle(
        UpdateRaceOddsCommand request,
        CancellationToken cancellationToken)
    {
        if (request.Entries is null || request.Entries.Count == 0)
            throw new InvalidOperationException("No odds were submitted.");

        var duplicated = request.Entries
            .GroupBy(x => x.EntryId)
            .Any(g => g.Count() > 1);
        if (duplicated)
            throw new InvalidOperationException("The same entry was submitted more than once.");

        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new KeyNotFoundException("Race not found.");

        if (race.Status != RaceExecutionConstants.RaceScheduled)
            throw new InvalidOperationException(
                $"Odds can only be adjusted while the race is Scheduled (current: {race.Status}).");

        if (race.OddsComputedAt is null)
            throw new InvalidOperationException(
                "Registration must be closed before odds can be adjusted.");

        // Sau khi khóa cược, giá phải đứng yên: các lệnh đã đặt khóa theo giá này và
        // settlement lúc Publish trả thưởng theo OddsLocked1.
        if (race.BettingLockedAt is not null)
            throw new InvalidOperationException(
                "Betting is already locked for this race — odds can no longer be changed.");

        var entryIds = request.Entries.Select(x => x.EntryId).ToList();

        var entries = await _context.Entries
            .Where(e => e.RaceId == request.RaceId &&
                        e.Status == RaceExecutionConstants.EntryApproved &&
                        entryIds.Contains(e.EntryId))
            .ToListAsync(cancellationToken);

        var missing = entryIds.Except(entries.Select(e => e.EntryId)).ToList();
        if (missing.Count > 0)
            throw new InvalidOperationException(
                $"Entry {string.Join(", ", missing)} is not an approved entry of this race.");

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        foreach (var adjustment in request.Entries)
        {
            var entry = entries.First(e => e.EntryId == adjustment.EntryId);

            if (adjustment.SuggestedOdds is { } suggested)
            {
                var error = RaceOddsPolicy.Validate(suggested, "Suggested odds");
                if (error is not null) throw new InvalidOperationException(error);
                entry.Odds = RaceOddsPolicy.Normalize(suggested);
            }

            // Bỏ trống giá công bố = "tính lại giúp tôi" — dùng đúng công thức mặc định để
            // Admin không phải tự nhân 0.9 bằng tay.
            var published = adjustment.PublishedOdds
                ?? RaceOddsPolicy.PublishedFromSuggested(entry.Odds);

            var publishedError = RaceOddsPolicy.Validate(published, "Published odds");
            if (publishedError is not null) throw new InvalidOperationException(publishedError);

            entry.PublishedOdds = RaceOddsPolicy.Normalize(published);
            entry.UpdatedAt = now;
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UpdateRaceOddsResponse(race.RaceId, entries.Count);
    }
}
