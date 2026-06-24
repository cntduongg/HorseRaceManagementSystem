using Application.Common;

namespace Application.Usecases.Horses.RevokeHorse;

/// <summary>
/// Flow 1: an admin revokes a previously Approved horse. Any Pending entries that use
/// the horse are auto-cancelled. <see cref="Reason"/> is optional.
/// </summary>
public sealed record RevokeHorseCommand(
    int HorseId,
    int AdminId,
    string? Reason
) : ICommand<RevokeHorseResponse>;
