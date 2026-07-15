namespace Application.Usecases.Admin.GetPendingHorses;

public sealed record GetPendingHorsesResponse(
    int HorseId,
    string Name,
    string? Breed,
    string? Color,
    int? BirthYear,
    string Status,
    int OwnerId,
    string OwnerName,
    DateTime CreatedAt,
    string? ImageUrl
);