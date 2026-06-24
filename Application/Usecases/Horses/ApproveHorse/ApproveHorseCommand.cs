using Application.Common;

namespace Application.Usecases.Horses.ApproveHorse;

/// <summary>
/// Flow 1: an admin approves a Pending horse. <see cref="AdminId"/> is recorded as the reviewer.
/// </summary>
public sealed record ApproveHorseCommand(
    int HorseId,
    int AdminId
) : ICommand<HorseActionResponse>;
