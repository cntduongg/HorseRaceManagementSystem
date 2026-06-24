using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegOfficialResults.CreateLegOfficialResult;

public sealed class CreateLegOfficialResultCommandHandler
    : IRequestHandler<CreateLegOfficialResultCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public CreateLegOfficialResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        CreateLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResultStatus))
            throw new InvalidOperationException("ResultStatus is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmationType))
            throw new InvalidOperationException("ConfirmationType is required.");

        if (request.LegPoints < 0)
            throw new InvalidOperationException("LegPoints cannot be negative.");

        if (request.FinishPosition.HasValue && request.FinishPosition <= 0)
            throw new InvalidOperationException("FinishPosition must be greater than 0.");

        var race = await _context.Races
            .FirstOrDefaultAsync(
                x => x.RaceId == request.RaceId,
                cancellationToken);

        if (race is null)
            throw new InvalidOperationException("Race does not exist.");

        var legExists = await _context.Legs
            .AnyAsync(x =>
                x.RaceId == request.RaceId &&
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

        if (request.ConfirmedByAdminId.HasValue)
        {
            var admin = await _context.Users
                .FirstOrDefaultAsync(
                    x => x.UserId == request.ConfirmedByAdminId.Value,
                    cancellationToken);

            if (admin is null)
                throw new InvalidOperationException("ConfirmedByAdmin does not exist.");
        }

        if (request.ConfirmationType.Equals("AdminOverride", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(request.OverrideReason))
        {
            throw new InvalidOperationException(
                "OverrideReason is required for AdminOverride.");
        }

        var exists = await _context.LegOfficialResults.AnyAsync(x =>
            x.RaceId == request.RaceId &&
            x.LegNumber == request.LegNumber &&
            x.EntryId == request.EntryId,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("LegOfficialResult already exists.");

        var entity = new LegOfficialResult
        {
            RaceId = request.RaceId,
            LegNumber = request.LegNumber,
            EntryId = request.EntryId,
            FinishPosition = request.FinishPosition,
            ResultStatus = request.ResultStatus.Trim(),
            LegPoints = request.LegPoints,
            ConfirmationType = request.ConfirmationType.Trim(),
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedByAdminId = request.ConfirmedByAdminId,
            OverrideReason = request.OverrideReason
        };

        _context.LegOfficialResults.Add(entity);

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}