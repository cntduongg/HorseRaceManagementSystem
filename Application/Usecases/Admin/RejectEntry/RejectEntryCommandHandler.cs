using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.RejectEntry;

public sealed class RejectEntryCommandHandler
    : IRequestHandler<RejectEntryCommand, RejectEntryResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public RejectEntryCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<RejectEntryResponse> Handle(
        RejectEntryCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required.");

        var entry = await _context.Entries
            .FirstOrDefaultAsync(
                x => x.EntryId == request.EntryId,
                cancellationToken);

        if (entry is null)
            throw new KeyNotFoundException("Entry not found");

        entry.Status = EntryStatus.Rejected;
        entry.RejectionReason = request.Reason;
        entry.UpdatedAt = DateTime.UtcNow;

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.Entry,
                EntityId = entry.EntryId,
                Action = ReviewAction.Rejected,
                Reason = request.Reason,
                AdminId = request.AdminId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new RejectEntryResponse(
            entry.EntryId,
            entry.Status,
            entry.RejectionReason);
    }
}