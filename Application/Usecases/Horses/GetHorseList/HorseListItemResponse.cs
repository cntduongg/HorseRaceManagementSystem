namespace Application.Usecases.Horses.GetHorseList;

public sealed record HorseListItemResponse(
    int HorseId,
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string? ImageUrl,
    string Status,
    string? RejectionReason
);
