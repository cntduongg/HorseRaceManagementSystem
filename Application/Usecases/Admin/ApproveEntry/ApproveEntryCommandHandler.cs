using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.ApproveEntry;

public sealed class ApproveEntryCommandHandler
    : IRequestHandler<ApproveEntryCommand, ApproveEntryResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public ApproveEntryCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<ApproveEntryResponse> Handle(
        ApproveEntryCommand request,
        CancellationToken cancellationToken)
    {
        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                x => x.EntryId == request.EntryId,
                cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException("Entry not found");

        // Chỉ gán GateNumber nếu chưa có
        if (entry.GateNumber is null)
        {
            var usedGates = await _context.Entries
                .Where(e => e.RaceId == entry.RaceId && e.GateNumber != null)
                .Select(e => e.GateNumber!.Value)
                .ToListAsync(cancellationToken);

            int nextGate = 1;
            while (usedGates.Contains(nextGate))
                nextGate++;

            entry.GateNumber = nextGate;
        }

        entry.Status = EntryStatus.Approved;
        entry.ApprovedAt = DateTime.UtcNow;
        entry.ApprovedBy = request.AdminId;
        entry.UpdatedAt = DateTime.UtcNow;

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.Entry,
                EntityId = entry.EntryId,
                Action = ReviewAction.Approved,
                Reason = request.Reason,
                AdminId = request.AdminId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveEntryResponse(
            entry.EntryId,
            entry.Status,
            entry.GateNumber);
    }
}