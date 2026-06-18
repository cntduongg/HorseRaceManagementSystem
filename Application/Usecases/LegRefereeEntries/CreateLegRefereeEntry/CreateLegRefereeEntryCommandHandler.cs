using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

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