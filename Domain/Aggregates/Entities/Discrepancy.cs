namespace Domain.Aggregates.Entities;

// Nhật ký sai lệch do hệ thống/Referee ghi nhận để Admin xem xét (Flow phụ trợ).
// Cột phẳng, không FK cứng để giữ độc lập với vòng đời Race/User.
public class Discrepancy
{
    public int DiscrepancyId { get; set; }

    public int? RaceId { get; set; }
    public int? ReportedById { get; set; }

    public string Type { get; set; } = "Other";
    // PredictionMismatch | ResultMismatch | PointCalculation | Other

    public string Status { get; set; } = "Pending";
    // Pending | Resolved | Dismissed

    public string? Description { get; set; }

    public int? PredictedPosition { get; set; }
    public int? OfficialPosition { get; set; }

    public string? Resolution { get; set; }
    public string? ResolutionAction { get; set; } // Dismissed | AdjustPoints
    public int? AdjustedPointsAwarded { get; set; }

    public int? ResolvedByAdminId { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public DateTime CreatedAt { get; set; }
}
