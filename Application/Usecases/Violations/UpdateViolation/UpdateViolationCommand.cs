using MediatR;

namespace Application.Usecases.Violations.UpdateViolation;

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
	int? ReviewedByAdminId,
	string? AdminNote
) : IRequest<bool>;