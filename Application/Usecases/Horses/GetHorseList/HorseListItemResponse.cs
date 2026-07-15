namespace Application.Usecases.Horses.GetHorseList;

public sealed record HorseListItemResponse(
    int HorseId,
    string Name,
    string? Breed,
    string? Color,
    int? BirthYear,
    string Status,
    int OwnerId,
    string OwnerName,
    DateTime CreatedAt,
    string? ImageUrl,
    string? RejectionReason
);