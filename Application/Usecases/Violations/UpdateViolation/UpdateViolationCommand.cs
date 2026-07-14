using MediatR;

namespace Application.Usecases.Violations.UpdateViolation;

/// <param name="ActorAdminId">Admin acting on the record — set from JWT by the controller; never trust body.</param>
public sealed record UpdateViolationCommand(
	int ViolationId,
	int RaceId,
	int LegNumber,
	int EntryId,
	int ReportedByRefereeId,
	string ViolationType,
	string? Description,
	string Penalty,
	string Status,
	string? AdminNote,
	int ActorAdminId
) : IRequest<bool>;
