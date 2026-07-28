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
    // Odds ĐỀ XUẤT (Entry.Odds) — giá máy tính từ lịch sử thắng, chỉ Admin đọc.
    decimal? CurrentOdds,
    // Odds CÔNG BỐ (Entry.PublishedOdds) — giá spectator thật sự cược. null = chưa tính.
    decimal? PublishedOdds,
    string TournamentName
);
