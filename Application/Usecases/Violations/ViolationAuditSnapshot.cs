using Application.Common;
using Domain.Aggregates.Entities;

namespace Application.Usecases.Violations;

/// <summary>Builds ReviewHistory snapshots for Violation mutations.</summary>
internal static class ViolationAuditSnapshot
{
    public static object Capture(
        Violation v,
        IEnumerable<object>? affectedStandings = null)
        => new
        {
            violationId = v.ViolationId,
            raceId = v.RaceId,
            legNumber = v.LegNumber,
            entryId = v.EntryId,
            reportedByRefereeId = v.ReportedByRefereeId,
            violationType = v.ViolationType,
            description = v.Description,
            penalty = v.Penalty,
            status = v.Status,
            reviewedByAdminId = v.ReviewedByAdminId,
            reviewedAt = v.ReviewedAt,
            adminNote = v.AdminNote,
            affectedStandings = affectedStandings
        };

    public static object Standing(
        int raceId,
        int legNumber,
        int entryId,
        string? resultStatus,
        int? finishPosition,
        int legPoints)
        => new
        {
            raceId,
            legNumber,
            entryId,
            resultStatus,
            finishPosition,
            legPoints
        };

    public static object Standing(LegOfficialResult result)
        => Standing(
            result.RaceId,
            result.LegNumber,
            result.EntryId,
            result.ResultStatus,
            result.FinishPosition,
            result.LegPoints);

    public static List<object> Standings(IEnumerable<LegOfficialResult> results)
        => results.Select(Standing).ToList();

    public static string? Serialize(
        Violation v,
        IEnumerable<object>? affectedStandings = null)
        => ReviewHistoryJson.Serialize(Capture(v, affectedStandings));
}
