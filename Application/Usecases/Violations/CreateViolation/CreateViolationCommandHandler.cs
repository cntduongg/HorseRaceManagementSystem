using Application.Common.Interfaces;
using Application.Usecases.RaceExecution;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.CreateViolation;

// Flow 6 — Referee báo cáo vi phạm (chỉ 1 referee, không blind).
// FE chỉ gửi raceId + entryId + violationType + description; referee lấy từ JWT;
// LegNumber mặc định = leg hiện hành; Status = Pending.
// Penalty LUÔN là "None" khi tạo — Admin mới là người ra quyết định xử phạt lúc Approve.
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
            throw new InvalidOperationException("Could not determine the reporting referee.");
        if (string.IsNullOrWhiteSpace(request.ViolationType))
            throw new InvalidOperationException("ViolationType is required.");

        // Race không được Finished/Cancelled — tránh report muộn sau khi đã publish/hủy
        // (approve sau đó không tự re-run publish nên sẽ làm sai kết quả đã công bố).
        var race = await _context.Races
            .FirstOrDefaultAsync(r => r.RaceId == request.RaceId, cancellationToken)
            ?? throw new InvalidOperationException("Race does not exist.");
        if (race.Status is RaceExecutionConstants.RaceFinished or RaceExecutionConstants.RaceCancelled)
            throw new InvalidOperationException(
                $"Violations cannot be reported for a race that is already {race.Status}.");

        // Entry phải thuộc race.
        var entry = await _context.Entries
            .FirstOrDefaultAsync(e => e.EntryId == request.EntryId, cancellationToken)
            ?? throw new InvalidOperationException("Entry does not exist.");
        if (entry.RaceId != request.RaceId)
            throw new InvalidOperationException("The entry does not belong to the selected race.");

        // LegNumber: nếu không gửi → leg hiện hành (leg mở đầu tiên, hoặc leg cuối).
        var legs = await _context.Legs
            .Where(l => l.RaceId == request.RaceId)
            .OrderBy(l => l.LegNumber)
            .ToListAsync(cancellationToken);
        if (legs.Count == 0)
            throw new InvalidOperationException("The race has not started — violations cannot be reported yet.");

        var legNumber = request.LegNumber;
        if (legNumber <= 0)
        {
            var openLeg = legs.FirstOrDefault(l => !ConfirmedLegStatuses.Contains(l.Status));
            legNumber = (openLeg ?? legs.Last()).LegNumber;
        }
        else if (legs.All(l => l.LegNumber != legNumber))
        {
            throw new InvalidOperationException("The leg does not exist in this race.");
        }

        // Án phạt do ADMIN quyết khi Approve — báo cáo của trọng tài KHÔNG mang đề xuất.
        // Trước đây handler nhận request.Penalty (mặc định "Warning") nên một báo cáo còn
        // Pending đã hiện sẵn "Warning" ở bảng Admin, trông y như đã có phán quyết. Nay luôn ghi
        // "None" = chưa quyết; `request.Penalty` bị bỏ qua hoàn toàn, không tin body.
        // ApproveViolation vẫn bắt buộc Admin chọn Warning/Demote/DQ nên không có đường nào để
        // "None" lọt vào một violation đã Approved.
        var violation = new Violation
        {
            RaceId = request.RaceId,
            LegNumber = legNumber,
            EntryId = request.EntryId,
            ReportedByRefereeId = request.ReportedByRefereeId,
            ViolationType = request.ViolationType.Trim(),
            Description = request.Description?.Trim(),
            Penalty = "None",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };

        _context.Violations.Add(violation);
        await _context.SaveChangesAsync(cancellationToken);

        return violation.ViolationId;
    }
}