using MediatR;

namespace Application.Usecases.Legs.UpdateLeg;

public sealed record UpdateLegCommand(
    int RaceId,
    int LegNumber,
    string Status,
    string? ConfirmationType,
    string? AdminOverrideReason
) : IRequest<bool>;