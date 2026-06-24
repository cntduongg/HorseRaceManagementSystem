using Application.Common;

namespace Application.Usecases.Horses.UpdateHorse;

/// <summary>
/// Edit a horse profile. <see cref="RequesterId"/> is the authenticated owner
/// (set by the Api layer from the JWT), used to enforce ownership.
/// </summary>
public sealed record UpdateHorseCommand(
    int HorseId,
    int RequesterId,
    string Name,
    string? Breed,
    int? BirthYear,
    string? Color,
    string? ImageUrl
) : ICommand<bool>;
