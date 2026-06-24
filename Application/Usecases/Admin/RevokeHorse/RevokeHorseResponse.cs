namespace Application.Usecases.Admin.RevokeHorse;

public sealed record RevokeHorseResponse(
    int HorseId,
    string Status,
    int CancelledEntries
);