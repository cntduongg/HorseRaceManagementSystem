namespace Application.Usecases.Horses.RejectHorse;

public sealed record RejectHorseResponse(
    int HorseId,
    string Status,
    string RejectionReason
);
