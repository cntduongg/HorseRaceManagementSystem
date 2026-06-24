namespace Application.Usecases.Horses.ApproveHorse;

/// <summary>Result of an admin moderation action (approve/reject) on a horse.</summary>
public sealed record HorseActionResponse(
    int HorseId,
    string Status,
    string? RejectionReason
);
