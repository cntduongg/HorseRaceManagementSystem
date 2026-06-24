using Application.Common;

namespace Application.Usecases.Admin.ApproveHorse;

public sealed record ApproveHorseCommand(
    int HorseId,
    int AdminId
) : ICommand<ApproveHorseResponse>;