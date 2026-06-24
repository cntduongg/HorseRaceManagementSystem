using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

namespace Application.Usecases.Entries.CreateEntry;

public sealed class CreateEntryCommandHandler
    : IRequestHandler<CreateEntryCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateEntryCommandHandler(IApplicationDbContext context)
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

        if (request.JockeyId <= 0)
            throw new InvalidOperationException("JockeyId is required.");

        if (request.HorseOwnerId <= 0)
            throw new InvalidOperationException("HorseOwnerId is required.");

        var entry = new Entry
        {
            RaceId = request.RaceId,
            HorseId = request.HorseId,
            JockeyId = request.JockeyId,
            HorseOwnerId = request.HorseOwnerId,
            Status = "Pending",
            SubmittedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _context.Entries.Add(entry);

        await _context.SaveChangesAsync(cancellationToken);

        return entry.EntryId;
    }
}