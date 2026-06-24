using Application.Common;
using Domain.Constants;
using MediatR;

namespace Application.Usecases.Horses.UpdateHorse;

public sealed class UpdateHorseCommandHandler
    : IRequestHandler<UpdateHorseCommand, bool>
{
    private readonly IHorseRepository _horseRepository;

    public UpdateHorseCommandHandler(IHorseRepository horseRepository)
    {
        _horseRepository = horseRepository;
    }

    public async Task<bool> Handle(
        UpdateHorseCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new InvalidOperationException("Horse name is required.");
        }

        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            return false;
        }

        if (horse.OwnerId != request.RequesterId)
        {
            throw new UnauthorizedAccessException("You can only edit your own horses.");
        }

        // An Approved or Revoked horse is locked; only Pending/Rejected profiles are editable.
        if (horse.Status is not (HorseStatus.Pending or HorseStatus.Rejected))
        {
            throw new InvalidOperationException(
                $"A horse in '{horse.Status}' status cannot be edited.");
        }

        horse.Name = request.Name.Trim();
        horse.Breed = request.Breed?.Trim();
        horse.BirthYear = request.BirthYear;
        horse.Color = request.Color?.Trim();
        horse.ImageUrl = string.IsNullOrWhiteSpace(request.ImageUrl) ? null : request.ImageUrl.Trim();
        horse.UpdatedAt = DateTime.UtcNow;

        // Editing a rejected horse resubmits it for approval.
        if (horse.Status == HorseStatus.Rejected)
        {
            horse.Status = HorseStatus.Pending;
            horse.RejectionReason = null;
        }

        return true;
    }
}
