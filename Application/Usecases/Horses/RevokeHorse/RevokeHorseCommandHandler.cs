using Application.Common;
using Domain.Constants;
using MediatR;

namespace Application.Usecases.Horses.RevokeHorse;

public sealed class RevokeHorseCommandHandler
    : IRequestHandler<RevokeHorseCommand, RevokeHorseResponse>
{
    private readonly IHorseRepository _horseRepository;
    private readonly IEntryRepository _entryRepository;

    public RevokeHorseCommandHandler(
        IHorseRepository horseRepository,
        IEntryRepository entryRepository)
    {
        _horseRepository = horseRepository;
        _entryRepository = entryRepository;
    }

    public async Task<RevokeHorseResponse> Handle(
        RevokeHorseCommand request,
        CancellationToken cancellationToken)
    {
        var horse = await _horseRepository.GetByIdAsync(request.HorseId, cancellationToken);
        if (horse is null)
        {
            throw new KeyNotFoundException($"Horse with ID {request.HorseId} was not found.");
        }

        if (horse.Status != HorseStatus.Approved)
        {
            throw new InvalidOperationException(
                $"Only Approved horses can be revoked (current: {horse.Status}).");
        }

        horse.Status = HorseStatus.Revoked;
        horse.RejectionReason = string.IsNullOrWhiteSpace(request.Reason)
            ? "Revoked by admin."
            : request.Reason.Trim();
        horse.UpdatedAt = DateTime.UtcNow;

        // Auto-cancel Pending entries that rely on this horse.
        var pendingEntries = await _entryRepository.GetByHorseAndStatusAsync(
            horse.HorseId, EntryStatus.Pending, cancellationToken);

        foreach (var entry in pendingEntries)
        {
            entry.Status = EntryStatus.Withdrawn;
            entry.RejectionReason = "Auto-cancelled: horse was revoked by admin.";
            entry.UpdatedAt = DateTime.UtcNow;
        }

        return new RevokeHorseResponse(horse.HorseId, horse.Status, pendingEntries.Count);
    }
}
