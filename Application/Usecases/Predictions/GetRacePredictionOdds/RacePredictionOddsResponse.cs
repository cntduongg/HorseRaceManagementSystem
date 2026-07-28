namespace Application.Usecases.Predictions.GetRacePredictionOdds;

public sealed record RacePredictionOddsResponse(
    int RaceId,
    string RaceName,
    string RaceStatus,
    DateTime ScheduledStartTime,
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
    decimal BaseOdds,
    decimal CurrentOdds,
    // Giá SẼ BỊ KHÓA nếu đặt `betAmount` vào chính entry này. Khác CurrentOdds vì lúc ghi lệnh,
    // CreatePrediction cộng chính số tiền đang đặt vào pool trước khi tính giá — nên
    // `currentOdds × betAmount` KHÔNG phải payout thật. Est. Payout phải dùng số này.
    // Không truyền betAmount (hoặc ≤ 0) → EffectiveOdds == CurrentOdds.
    decimal EffectiveOdds,
    decimal EntryPool,
    decimal TotalPool);