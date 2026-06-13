namespace Application.Usecases.Horses.GetHorseList;

public sealed record HorseListItemResponse(
    int HorseId,
    string Name,
    string Status,
    string? Breed
);