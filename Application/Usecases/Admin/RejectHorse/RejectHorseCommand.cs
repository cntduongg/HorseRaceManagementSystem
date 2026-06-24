using Application.Common;

namespace Application.Usecases.Admin.RejectHorse;

public sealed record RejectHorseCommand(
    int HorseId,
    string? Reason
) : ICommand<RejectHorseResponse>;