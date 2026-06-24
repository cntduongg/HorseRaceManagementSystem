namespace Application.Usecases.Admin.ApproveHorse;

public sealed record ApproveHorseResponse(
    int HorseId,
    string Status,
    DateTime ApprovedAt
);