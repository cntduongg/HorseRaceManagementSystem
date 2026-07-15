namespace Application.Usecases.Horses.GetHorseDetail;

public sealed record HorseDetailResponse(
    int HorseId,
    int OwnerId,
    string OwnerName,
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string Status,
    string? ImageUrl,
    DateTime CreatedAt,
    string? RejectionReason
);