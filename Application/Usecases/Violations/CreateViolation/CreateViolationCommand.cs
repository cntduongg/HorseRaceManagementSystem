using MediatR;

namespace Application.Usecases.Violations.CreateViolation;

public sealed record CreateViolationCommand(
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
) : IRequest<int>;