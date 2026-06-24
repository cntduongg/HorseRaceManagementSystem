using Application.Common;

namespace Application.Usecases.Admin.RevokeHorse;

public sealed record RevokeHorseCommand(
    int HorseId
) : ICommand<RevokeHorseResponse>;