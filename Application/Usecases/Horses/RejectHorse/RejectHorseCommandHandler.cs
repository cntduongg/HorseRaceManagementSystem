using Application.Common;
using Domain.Constants;
using MediatR;

namespace Application.Usecases.Horses.RejectHorse;

public sealed class RejectHorseCommandHandler
    : IRequestHandler<RejectHorseCommand, RejectHorseResponse>
{
    private readonly IHorseRepository _horseRepository;

    public RejectHorseCommandHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<RejectHorseResponse> Handle(
        RejectHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            throw new InvalidOperationException("A rejection reason is required.");
        }

        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            throw new KeyNotFoundException($"Horse with ID {request.HorseId} was not found.");
        }

        if (horse.Status != HorseStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Only Pending horses can be rejected (current: {horse.Status}).");
        }

        horse.Status = HorseStatus.Rejected;
        horse.RejectionReason = request.Reason.Trim();
        horse.ApprovedBy = request.AdminId;
        horse.ApprovedAt = DateTime.UtcNow;
        horse.UpdatedAt = DateTime.UtcNow;

        return new RejectHorseResponse(horse.HorseId, horse.Status, horse.RejectionReason);
    }
}
