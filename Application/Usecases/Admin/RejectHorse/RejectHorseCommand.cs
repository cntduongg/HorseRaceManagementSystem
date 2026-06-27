using Application.Common;

namespace Application.Usecases.Admin.RejectHorse;

public sealed record RejectHorseCommand(
    int HorseId,
    int AdminId,
    string? Reason
) : ICommand<RejectHorseResponse>;