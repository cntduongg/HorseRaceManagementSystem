namespace Application.Usecases.Horses.GetHorseStatistics;

public sealed record HorseStatisticsResponse(
    int HorseId,
    string Name,
    int Stamina,            // Thể lực hiện tại (0 -> 3)
    string HealthStatus,    // Sức khỏe quy đổi (Healthy, Fair, Weak, Exhausted)
    int TotalRaces,         // Tổng trận đã chạy
    int TotalWins,          // Tổng trận thắng (vị trí 1)
    int TotalTop3,          // Tổng trận về top 3
    decimal WinRate,        // Tỷ lệ thắng %
    int? RecentRacePosition,// Vị trí trận gần nhất
    string? RecentRaceName, // Tên trận gần nhất
    List<int> RecentForm    // Phong độ 5 trận gần nhất (ví dụ: [1, 3, 2, 5, 4])
);