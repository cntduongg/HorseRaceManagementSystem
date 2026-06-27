using Application.Common;
using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using Domain.Aggregates.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.ApproveHorse;

public class ApproveHorseCommandHandler
    : IRequestHandler<ApproveHorseCommand, ApproveHorseResponse>
{
    private readonly IApplicationDbContext _context;
    private readonly IReviewHistoryRepository _reviewHistoryRepository;

    public ApproveHorseCommandHandler(
        IApplicationDbContext context,
        IReviewHistoryRepository reviewHistoryRepository)
    {
        _context = context;
        _reviewHistoryRepository = reviewHistoryRepository;
    }

    public async Task<ApproveHorseResponse> Handle(
        ApproveHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _context.Horses
            .FirstOrDefaultAsync(
                x => x.HorseId == request.HorseId,
                cancellationToken);

        if (horse is null)
            throw new KeyNotFoundException("Horse not found");

        horse.Status = HorseStatus.Approved;
        horse.ApprovedAt = DateTime.UtcNow;
        horse.ApprovedBy = request.AdminId;
        horse.UpdatedAt = DateTime.UtcNow;

        await _reviewHistoryRepository.AddAsync(
            new ReviewHistory
            {
                EntityType = ReviewEntity.Horse,
                EntityId = horse.HorseId,
                Action = ReviewAction.Approved,
                Reason = request.Reason,
                AdminId = request.AdminId
            },
            cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        return new ApproveHorseResponse(
            horse.HorseId,
            horse.Status,
            horse.ApprovedAt.Value);
    }
}