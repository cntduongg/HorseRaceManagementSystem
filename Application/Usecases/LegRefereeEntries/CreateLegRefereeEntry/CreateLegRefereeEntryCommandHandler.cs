using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
namespace Application.Usecases.LegRefereeEntries.CreateLegRefereeEntry;

public sealed class CreateLegRefereeEntryCommandHandler
    : IRequestHandler<CreateLegRefereeEntryCommand, long>
{
    private readonly IApplicationDbContext _context;

    public CreateLegRefereeEntryCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<long> Handle(
    CreateLegRefereeEntryCommand request,
    CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResultStatus))
            throw new InvalidOperationException("ResultStatus is required.");

        if (request.RefereeUserId <= 0)
            throw new InvalidOperationException("RefereeUserId is invalid.");

        if (request.EntryId <= 0)
            throw new InvalidOperationException("EntryId is invalid.");

        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == request.RaceId,
                cancellationToken);

        if (race is null)
            throw new InvalidOperationException("Race does not exist.");

        var legExists = await _context.Legs
            .AnyAsync(
                x => x.RaceId == request.RaceId &&
                     x.LegNumber == request.LegNumber,
                cancellationToken);

        if (!legExists)
            throw new InvalidOperationException("Leg does not exist.");

        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                x => x.EntryId == request.EntryId,
                cancellationToken);

        if (entry is null)
            throw new InvalidOperationException("Entry does not exist.");

        if (entry.RaceId != request.RaceId)
            throw new InvalidOperationException("Entry does not belong to this race.");

        var referee = await _context.Users
            .FirstOrDefaultAsync(
                x => x.UserId == request.RefereeUserId,
                cancellationToken);

        if (referee is null)
            throw new InvalidOperationException("Referee does not exist.");

        if (request.RefereeUserId != race.Referee1Id &&
            request.RefereeUserId != race.Referee2Id)
        {
            throw new InvalidOperationException("Referee is not assigned to this race.");
        }

        var duplicate = await _context.LegRefereeEntries
            .AnyAsync(x =>
                x.RaceId == request.RaceId &&
                x.LegNumber == request.LegNumber &&
                x.EntryId == request.EntryId &&
                x.RefereeUserId == request.RefereeUserId,
                cancellationToken);

        if (duplicate)
            throw new InvalidOperationException(
                "This referee has already submitted the result.");

        var entity = new LegRefereeEntry
        {
            RaceId = request.RaceId,
            LegNumber = request.LegNumber,
            EntryId = request.EntryId,
            RefereeUserId = request.RefereeUserId,
            FinishPosition = request.FinishPosition,
            ResultStatus = request.ResultStatus,
            SubmittedAt = DateTime.UtcNow
        };

        _context.LegRefereeEntries.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return entity.LegRefereeEntryId;
    }
}