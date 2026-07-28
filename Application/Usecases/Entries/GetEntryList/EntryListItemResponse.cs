namespace Application.Usecases.Entries.GetEntryList;

public sealed record EntryListItemResponse(
    int EntryId,
    int RaceId,
    int HorseId,
    string? HorseName,
    string? HorseImageUrl,
    int JockeyId,
    string? JockeyName,
    string? JockeyAvatarUrl,
    int HorseOwnerId,
    string? HorseOwnerName,
    int? GateNumber,
    string Status,
    string? RejectionReason,
    DateTime SubmittedAt,
    DateTime? ApprovedAt,
    // Odds duy nhất của Entry — máy tính từ lịch sử thắng lúc đóng đăng ký rồi đứng yên.
    // Đây cũng CHÍNH LÀ giá spectator cược và bị khóa vào Prediction. null = chưa tính.
    decimal? Odds,
    string TournamentName
);
