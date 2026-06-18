using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.RaceResults.CreateRaceResult;

public sealed class CreateRaceResultCommandHandler
    : IRequestHandler<CreateRaceResultCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public CreateRaceResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(CreateRaceResultCommand request, CancellationToken cancellationToken)
    {
        var exists = await _context.RaceResults
            .AnyAsync(x => x.RaceId == request.RaceId && x.EntryId == request.EntryId, cancellationToken);

        if (exists)
            throw new InvalidOperationException("RaceResult already exists.");

        var entity = new RaceResult
        {
            RaceId = request.RaceId,
            EntryId = request.EntryId,
            TotalPoints = request.TotalPoints,
            FinalPosition = request.FinalPosition,
            IsRaceDQ = request.IsRaceDQ,
            LegWinCount = request.LegWinCount,
            LegTop3Count = request.LegTop3Count,
            CreatedAt = DateTime.UtcNow
        };

        _context.RaceResults.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}