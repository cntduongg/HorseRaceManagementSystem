using Application.Common;
using Domain.Constants;
using MediatR;

namespace Application.Usecases.Horses.ApproveHorse;

public sealed class ApproveHorseCommandHandler
    : IRequestHandler<ApproveHorseCommand, HorseActionResponse>
{
    private readonly IHorseRepository _horseRepository;

    public ApproveHorseCommandHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<HorseActionResponse> Handle(
        ApproveHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            throw new KeyNotFoundException($"Horse with ID {request.HorseId} was not found.");
        }

        if (horse.Status != HorseStatus.Pending)
        {
            throw new InvalidOperationException(
                $"Only Pending horses can be approved (current: {horse.Status}).");
        }

        horse.Status = HorseStatus.Approved;
        horse.ApprovedBy = request.AdminId;
        horse.ApprovedAt = DateTime.UtcNow;
        horse.RejectionReason = null;
        horse.UpdatedAt = DateTime.UtcNow;

        return new HorseActionResponse(horse.HorseId, horse.Status, horse.RejectionReason);
    }
}
