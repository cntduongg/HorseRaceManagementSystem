using MediatR;

namespace Application.Usecases.Legs.CreateLeg;

public sealed record CreateLegCommand(
	int RaceId,
	int LegNumber,
	string Status,
	string? ConfirmationType
) : IRequest<bool>;