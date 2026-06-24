namespace Application.Usecases.Admin.RejectHorse;

public sealed record RejectHorseResponse(
    int HorseId,
    string Status,
    string? Reason
);