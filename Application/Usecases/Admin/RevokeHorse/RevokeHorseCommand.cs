using Application.Common;

namespace Application.Usecases.Admin.RevokeHorse;

public sealed record RevokeHorseCommand(
    int HorseId,
    int AdminId,
    string Reason
) : ICommand<RevokeHorseResponse>;