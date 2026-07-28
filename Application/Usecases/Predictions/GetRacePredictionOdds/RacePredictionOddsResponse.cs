namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed record RacePredictionOddsResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    DateTime ScheduledStartTime,
    DateTime? OddsComputedAt,
    // Mốc Admin công bố odds — cửa cược chỉ mở từ đây.
    DateTime? OddsPublishedAt,
    // Mốc Admin khóa sổ cược — sau đó không đặt/hủy được nữa.
    DateTime? BettingLockedAt,
    bool IsBettingOpen,
    List<RacePredictionOddsEntryResponse> Entries);

public sealed record RacePredictionOddsEntryResponse(
    int EntryId,
    int HorseId,
    string? HorseName,
    string? HorseImageUrl,
    int JockeyId,
    string? JockeyName,
    string? JockeyAvatarUrl,
    int HorseOwnerId,
    string? HorseOwnerName,
    int? GateNumber,
    // ODDS CÔNG BỐ — con số Admin đã duyệt. Đây CHÍNH LÀ giá khóa vào lệnh cược:
    // payout = betAmount × odds, không có giá thứ hai nào khác.
    // (Trước 2026-07-28 odds động theo pool nên bảng hiện 4.00x mà lệnh khóa 2.00x.)
    decimal Odds,
    // Số liệu tham khảo — tổng điểm đã đặt, KHÔNG ảnh hưởng tới giá.
    decimal EntryPool,
    decimal TotalPool);
