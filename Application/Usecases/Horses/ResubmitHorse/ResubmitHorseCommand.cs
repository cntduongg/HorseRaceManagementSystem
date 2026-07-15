using MediatR;

namespace Application.Usecases.Horses.ResubmitHorse;

public sealed record ResubmitHorseCommand(
    int HorseId,
    int OwnerId
) : IRequest<ResubmitHorseResult>;

public enum ResubmitHorseError
{
    None,
    NotFound,
    Forbidden,
    InvalidStatus
}

public sealed record ResubmitHorseResult(
    bool Success,
    ResubmitHorseError Error,
    int? HorseId = null,
    string? Status = null);