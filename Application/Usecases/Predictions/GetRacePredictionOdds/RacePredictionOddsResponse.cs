namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed record RacePredictionOddsResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    DateTime ScheduledStartTime,
    // Mốc đóng đăng ký = mốc odds được tính. Cửa cược = RaceStatus "Scheduled" + mốc này
    // khác null; không có bước công bố/khóa nào nữa. FE suy trạng thái từ đúng 2 field đó.
    DateTime? OddsComputedAt,
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
    // Odds TĨNH — tính một lần lúc đóng đăng ký rồi không đổi. Đây CHÍNH LÀ giá khóa vào
    // lệnh cược: payout = betAmount × odds, không có giá thứ hai nào khác.
    decimal Odds,
    // Số liệu tham khảo — tổng điểm đã đặt, KHÔNG ảnh hưởng tới giá.
    decimal EntryPool,
    decimal TotalPool);
