namespace Application.Usecases.Admin.GetPendingHorses;

public sealed record GetPendingHorsesResponse(
    int HorseId,
    string Name,
    string? Breed,
    int OwnerId,
    string OwnerName,
    DateTime CreatedAt
);