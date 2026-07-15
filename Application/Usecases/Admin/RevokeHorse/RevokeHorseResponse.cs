namespace Application.Usecases.Admin.RevokeHorse;

public sealed record RevokeHorseResponse(
    int HorseId,
    string Status,
    int CancelledPendingEntries,
    List<int> ApprovedEntryIdsRequiringManualReview
);