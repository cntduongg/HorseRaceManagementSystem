using Application.Common;

namespace Application.Usecases.Horses.DeleteHorse;

/// <summary>
/// Delete a horse. <see cref="RequesterId"/> is the authenticated owner (from the JWT).
/// </summary>
public sealed record DeleteHorseCommand(
    int HorseId,
    int RequesterId
) : ICommand<bool>;
