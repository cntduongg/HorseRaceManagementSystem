namespace Application.Usecases.RaceExecution;

/// <summary>
/// Quy tắc odds dùng chung cho CẢ hệ thống — Admin lẫn Spectator đọc cùng một nguồn (Flow 3 + 7).
///
/// Hai con số, hai vai trò khác nhau:
/// <list type="bullet">
/// <item><b>ODDS ĐỀ XUẤT</b> (<c>Entry.Odds</c>) — máy tính từ lịch sử thắng của ngựa lúc đóng
/// đăng ký (<c>CloseRegistrationCommandHandler.OddsFor</c>). Đây là ước lượng "giá công bằng",
/// chỉ Admin nhìn thấy.</item>
/// <item><b>ODDS CÔNG BỐ</b> (<c>Entry.PublishedOdds</c>) — giá thật spectator đặt cược, mặc
/// định = đề xuất trừ <see cref="HouseMarginRate"/> để nhà cái luôn có biên. Admin sửa được
/// trong modal trước khi publish.</item>
/// </list>
///
/// ⚠️ Trước đây spectator thấy odds ĐỘNG theo pool: bảng hiện 4.00x nhưng lúc ghi lệnh BE cộng
/// chính số tiền đang đặt vào pool rồi mới chốt giá nên khóa ở 2.00x — người chơi đọc thành gian
/// lận. Nay odds là **tĩnh**: số spectator thấy chính là số bị khóa vào Prediction.
/// </summary>
public static class RaceOddsPolicy
{
    /// <summary>Biên nhà cái trừ vào odds đề xuất để ra odds công bố (10%).</summary>
    public const decimal HouseMarginRate = 0.10m;

    /// <summary>
    /// Sàn odds công bố. Phải &gt; 1.00: odds ≤ 1 nghĩa là đoán đúng vẫn mất tiền — vô lý với
    /// người chơi. Đề xuất 1.10 sau khi trừ 10% còn 0.99 nên cái sàn này là bắt buộc, không phải
    /// phòng thủ thừa.
    /// </summary>
    public const decimal MinOdds = 1.01m;

    /// <summary>Trần odds — khớp trần của thang đề xuất (<c>OddsFor</c>).</summary>
    public const decimal MaxOdds = 25.00m;

    /// <summary>Odds công bố mặc định = đề xuất × (1 − biên nhà cái), làm tròn 2 chữ số.</summary>
    public static decimal PublishedFromSuggested(decimal suggestedOdds)
        => Normalize(suggestedOdds * (1m - HouseMarginRate));

    /// <summary>Clamp về [<see cref="MinOdds"/>, <see cref="MaxOdds"/>] + làm tròn 2 chữ số.</summary>
    public static decimal Normalize(decimal odds)
        => Math.Round(Math.Clamp(odds, MinOdds, MaxOdds), 2, MidpointRounding.AwayFromZero);

    /// <summary>
    /// Kiểm tra odds Admin nhập tay. Trả null nếu hợp lệ, khác null là message lỗi (tiếng Anh).
    /// </summary>
    public static string? Validate(decimal odds, string fieldName)
    {
        if (odds < MinOdds || odds > MaxOdds)
            return $"{fieldName} must be between {MinOdds:0.00} and {MaxOdds:0.00} (got {odds:0.00}).";

        return null;
    }
}
