using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.RejectHorse;

public class RejectHorseCommandHandler
    : IRequestHandler<RejectHorseCommand, RejectHorseResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public RejectHorseCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<RejectHorseResponse> Handle(
        RejectHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new ArgumentException("Reason is required.");

        var horse = await _context.Horses
            .FirstOrDefaultAsync(
                x => x.HorseId == request.HorseId,
                cancellationToken);

        if (horse is null)
            throw new KeyNotFoundException("Horse not found");

        horse.Status = HorseStatus.Rejected;
        horse.RejectionReason = request.Reason;
        horse.UpdatedAt = DateTime.UtcNow;

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.Horse,
                EntityId = horse.HorseId,
                Action = ReviewAction.Rejected,
                Reason = request.Reason,
                AdminId = request.AdminId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new RejectHorseResponse(
            horse.HorseId,
            horse.Status,
            horse.RejectionReason);
    }
}