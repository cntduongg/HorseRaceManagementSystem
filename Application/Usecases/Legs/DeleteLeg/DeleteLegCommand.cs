using MediatR;

namespace Application.Usecases.Legs.DeleteLeg;

public sealed record DeleteLegCommand(
    int RaceId,
    int LegNumber
) : IRequest<bool>;