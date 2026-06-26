using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.CreateViolation;

// Flow 6 — Referee báo cáo vi phạm (chỉ 1 referee, không blind).
// FE chỉ gửi raceId + entryId + violationType + description; referee lấy từ JWT;
// LegNumber mặc định = leg hiện hành; Penalty do Admin quyết khi Approve (mặc định Warning); Status = Pending.
public sealed class CreateViolationCommandHandler
    : IRequestHandler<CreateViolationCommand, int>
{
    private static readonly string[] ConfirmedLegStatuses = { "Confirmed", "Resolved" };

    private readonly IApplicationDbContext _context;

    public CreateViolationCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateViolationCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");
        if (request.EntryId <= 0)
            throw new InvalidOperationException("EntryId is invalid.");
        if (request.ReportedByRefereeId <= 0)
            throw new InvalidOperationException("Không xác định được trọng tài báo cáo.");
        if (string.IsNullOrWhiteSpace(request.ViolationType))
            throw new InvalidOperationException("ViolationType is required.");

        // Entry phải thuộc race.
        var entry = await _context.Entries
            .FirstOrDefaultAsync(e => e.EntryId == request.EntryId, cancellationToken)
            ?? throw new InvalidOperationException("Entry không tồn tại.");
        if (entry.RaceId != request.RaceId)
            throw new InvalidOperationException("Entry không thuộc cuộc đua đã chọn.");

        // LegNumber: nếu không gửi → leg hiện hành (leg mở đầu tiên, hoặc leg cuối).
        var legs = await _context.Legs
            .Where(l => l.RaceId == request.RaceId)
            .OrderBy(l => l.LegNumber)
            .ToListAsync(cancellationToken);
        if (legs.Count == 0)
            throw new InvalidOperationException("Cuộc đua chưa bắt đầu — chưa thể báo cáo vi phạm.");

        var legNumber = request.LegNumber;
        if (legNumber <= 0)
        {
            var openLeg = legs.FirstOrDefault(l => !ConfirmedLegStatuses.Contains(l.Status));
            legNumber = (openLeg ?? legs.Last()).LegNumber;
        }
        else if (legs.All(l => l.LegNumber != legNumber))
        {
            throw new InvalidOperationException("Leg không tồn tại trong cuộc đua.");
        }

        var penalty = string.IsNullOrWhiteSpace(request.Penalty) ? "Warning" : request.Penalty.Trim();
        if (penalty is not ("Warning" or "Demote" or "DQ"))
            throw new InvalidOperationException("Penalty must be Warning, Demote or DQ.");

        var violation = new Violation
        {
            RaceId = request.RaceId,
            LegNumber = legNumber,
            EntryId = request.EntryId,
            ReportedByRefereeId = request.ReportedByRefereeId,
            ViolationType = request.ViolationType.Trim(),
            Description = request.Description?.Trim(),
            Penalty = penalty,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Violations.Add(violation);
        await _context.SaveChangesAsync(cancellationToken);

        return violation.ViolationId;
    }
}
