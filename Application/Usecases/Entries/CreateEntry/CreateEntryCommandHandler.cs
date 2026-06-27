using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Entries.CreateEntry;

// Flow 2 — Owner nộp Entry (Horse + Jockey đã xác nhận).
// Jockey LẤY TỪ invitation Confirmed (không tin body); horse Approved & thuộc owner;
// race Scheduled; chỉ cho phép khi registration còn mở;
// không trùng horse/jockey trong cùng race.
public sealed class CreateEntryCommandHandler
    : IRequestHandler<CreateEntryCommand, int>
{
    private static readonly string[] ActiveEntryStatuses =
    {
        "Pending",
        "Approved"
    };

    private readonly IApplicationDbContext _context;

    public CreateEntryCommandHandler(
        IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is required.");

        if (request.HorseId <= 0)
            throw new InvalidOperationException("HorseId is required.");

        if (request.HorseOwnerId <= 0)
            throw new InvalidOperationException("HorseOwnerId is required.");

        //---------------------------------------------------------
        // Horse
        //---------------------------------------------------------

        var horse = await _context.Horses
            .FirstOrDefaultAsync(
                h => h.HorseId == request.HorseId,
                cancellationToken)
            ?? throw new InvalidOperationException("Ngựa không tồn tại.");

        if (horse.OwnerId != request.HorseOwnerId)
            throw new InvalidOperationException(
                "Bạn không sở hữu con ngựa này.");

        if (horse.Status != "Approved")
            throw new InvalidOperationException(
                "Chỉ ngựa đã duyệt mới được nộp Entry.");

        //---------------------------------------------------------
        // Race
        //---------------------------------------------------------

        var race = await _context.Races
            .FirstOrDefaultAsync(
                r => r.RaceId == request.RaceId,
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Cuộc đua không tồn tại.");

        if (race.Status != "Scheduled")
            throw new InvalidOperationException(
                "Chỉ nộp Entry khi cuộc đua đang Scheduled.");

        var now = DateTime.UtcNow;

        // Registration chưa mở
        if (race.RegistrationOpenAt == null)
            throw new InvalidOperationException(
                "Registration chưa được mở.");

        // Registration đã đóng
        if (race.RegistrationCloseAt != null &&
            race.RegistrationCloseAt <= now)
            throw new InvalidOperationException(
                "Registration đã đóng.");

        //---------------------------------------------------------
        // Confirmed invitation
        //---------------------------------------------------------

        var confirmed = await _context.JockeyInvitations
            .FirstOrDefaultAsync(
                x =>
                    x.HorseOwnerId == request.HorseOwnerId &&
                    x.HorseId == request.HorseId &&
                    x.RaceId == request.RaceId &&
                    x.Status == "Confirmed",
                cancellationToken)
            ?? throw new InvalidOperationException(
                "Chưa có nài được xác nhận cho cặp Ngựa + Cuộc đua này.");

        var jockeyId = confirmed.JockeyId;

        //---------------------------------------------------------
        // Duplicate horse
        //---------------------------------------------------------

        var horseTaken = await _context.Entries.AnyAsync(
            e =>
                e.RaceId == request.RaceId &&
                e.HorseId == request.HorseId &&
                ActiveEntryStatuses.Contains(e.Status),
            cancellationToken);

        if (horseTaken)
            throw new InvalidOperationException(
                "Ngựa này đã có Entry trong cuộc đua.");

        //---------------------------------------------------------
        // Duplicate jockey
        //---------------------------------------------------------

        var jockeyTaken = await _context.Entries.AnyAsync(
            e =>
                e.RaceId == request.RaceId &&
                e.JockeyId == jockeyId &&
                ActiveEntryStatuses.Contains(e.Status),
            cancellationToken);

        if (jockeyTaken)
            throw new InvalidOperationException(
                "Nài này đã có Entry trong cuộc đua.");

        //---------------------------------------------------------
        // Create
        //---------------------------------------------------------

        var entry = new Entry
        {
            RaceId = request.RaceId,
            HorseId = request.HorseId,
            JockeyId = jockeyId,
            HorseOwnerId = request.HorseOwnerId,
            Status = "Pending",
            SubmittedAt = now,
            CreatedAt = now
        };

        _context.Entries.Add(entry);

        await _context.SaveChangesAsync(cancellationToken);

        return entry.EntryId;
    }
}