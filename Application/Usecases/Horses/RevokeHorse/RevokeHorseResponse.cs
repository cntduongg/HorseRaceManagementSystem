namespace Application.Usecases.Horses.RevokeHorse;

public sealed record RevokeHorseResponse(
    int HorseId,
    string Status,
    int CancelledEntriesCount
);
