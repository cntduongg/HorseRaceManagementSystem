using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.Usecases.Tournaments.ProcessDueTournamentStatuses;

/// <summary>
/// Quét Tournament chưa kết thúc và đẩy Status theo ngày (xem <see cref="TournamentStatus.ResolveByDate"/>).
///
/// Trước đây Tournament.Status **chỉ đổi khi Admin vào sửa tay** qua <c>PUT /api/tournaments/{id}</c>:
/// giải đã bắt đầu, thậm chí kết thúc từ lâu, vẫn nằm ở "Draft" và hiện là "Upcoming" trên FE.
/// Race đã có worker tự start/cancel, còn Tournament thì không có gì cả.
///
/// Không cần quét dày như Race (Race tính theo giờ, Tournament tính theo ngày) — mặc định 1 giờ/lần.
/// Idempotent: chạy lại bao nhiêu lần cũng ra cùng kết quả, không có gì để "chạy lại nhầm".
/// </summary>
public sealed record ProcessDueTournamentStatusesCommand
    : ICommand<ProcessDueTournamentStatusesResponse>;

public sealed record TournamentStatusTransition(
    int TournamentId,
    string From,
    string To);

public sealed record ProcessDueTournamentStatusesResponse(
    int Examined,
    int Updated,
    IReadOnlyList<TournamentStatusTransition> Transitions);

public sealed class ProcessDueTournamentStatusesCommandHandler
    : IRequestHandler<ProcessDueTournamentStatusesCommand, ProcessDueTournamentStatusesResponse>
{
    private const int BatchSize = 200;

    private readonly IApplicationDbContext _context;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ProcessDueTournamentStatusesCommandHandler> _logger;

    public ProcessDueTournamentStatusesCommandHandler(
        IApplicationDbContext context,
        TimeProvider timeProvider,
        ILogger<ProcessDueTournamentStatusesCommandHandler> logger)
    {
        _context = context;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<ProcessDueTournamentStatusesResponse> Handle(
        ProcessDueTournamentStatusesCommand request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow().UtcDateTime;
        var today = DateOnly.FromDateTime(now);

        // Lọc sẵn ở SQL: chỉ giải chưa terminal VÀ đã tới/qua mốc ngày nào đó.
        // Cancelled/Finished không lọt vào đây — đó là ràng buộc quan trọng nhất của cơ chế này.
        var candidates = await _context.Tournaments
            .Where(t =>
                (t.Status == TournamentStatus.Draft ||
                 t.Status == TournamentStatus.Open ||
                 t.Status == TournamentStatus.Ongoing) &&
                (t.StartDate <= today || t.EndDate < today))
            .OrderBy(t => t.StartDate)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        var transitions = new List<TournamentStatusTransition>();

        foreach (var tournament in candidates)
        {
            var target = TournamentStatus.ResolveByDate(
                tournament.Status,
                tournament.StartDate,
                tournament.EndDate,
                today);

            if (target is null)
                continue;

            transitions.Add(new TournamentStatusTransition(
                tournament.TournamentId,
                tournament.Status,
                target));

            tournament.Status = target;
            tournament.UpdatedAt = now;
        }

        if (transitions.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);

            foreach (var t in transitions)
            {
                _logger.LogInformation(
                    "Tournament {TournamentId} status auto-updated: {From} → {To}.",
                    t.TournamentId, t.From, t.To);
            }
        }

        return new ProcessDueTournamentStatusesResponse(
            candidates.Count,
            transitions.Count,
            transitions);
    }
}
