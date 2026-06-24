using Application.Common;

namespace Application.Usecases.Horses.RejectHorse;

/// <summary>
/// Flow 1: an admin rejects a Pending horse with a mandatory <see cref="Reason"/>.
/// </summary>
public sealed record RejectHorseCommand(
    int HorseId,
    int AdminId,
    string Reason
) : ICommand<RejectHorseResponse>;
