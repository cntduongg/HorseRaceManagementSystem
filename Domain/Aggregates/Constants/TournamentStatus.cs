namespace Domain.Aggregates.Constants;

/// <summary>
/// Trạng thái vòng đời của Tournament. Trước đây 5 giá trị này chỉ nằm trong **comment** ở
/// <c>Tournament.cs</c> nên <c>UpdateTournament</c> nhận string tùy ý (gõ sai là hỏng dữ liệu
/// âm thầm) — nay gom về đây giống <see cref="RaceStatus"/>/<see cref="HorseStatus"/>/<see cref="EntryStatus"/>.
/// </summary>
public static class TournamentStatus
{
    public const string Draft = "Draft";          // FE hiển thị "Upcoming"
    public const string Open = "Open";            // FE hiển thị "Registration Open"
    public const string Ongoing = "Ongoing";      // FE hiển thị "In Progress"
    public const string Finished = "Finished";
    public const string Cancelled = "Cancelled";

    public static readonly string[] All = [Draft, Open, Ongoing, Finished, Cancelled];

    public static bool IsValid(string? status)
        => status is not null && All.Contains(status, StringComparer.Ordinal);

    /// <summary>
    /// Trạng thái kết thúc — <b>không bao giờ</b> được tự động đổi.
    /// <c>Cancelled</c>: giải đã hủy, ngày rơi vào khoảng Start/End cũng không được sống lại.
    /// <c>Finished</c>: Admin có quyền kết thúc sớm trước EndDate, tự đẩy ngược về Ongoing là sai.
    /// </summary>
    public static bool IsTerminal(string? status)
        => status is Finished or Cancelled;

    /// <summary>
    /// Tính trạng thái đúng theo ngày. Trả về <c>null</c> nghĩa là <b>giữ nguyên</b>.
    ///
    /// Quy tắc (chỉ đi TIẾN, không bao giờ lùi):
    ///   • <c>Cancelled</c>/<c>Finished</c> hoặc giá trị lạ → không đụng tới.
    ///   • <c>today &gt; EndDate</c>  → <c>Finished</c> (kể cả đang <c>Draft</c>/<c>Open</c>: giải cũ chưa ai đổi tay).
    ///   • <c>today &gt;= StartDate</c> → <c>Ongoing</c>.
    ///   • Chưa tới StartDate → giữ nguyên <c>Draft</c>/<c>Open</c> (Admin tự quyết khi nào mở đăng ký).
    ///
    /// <c>Open</c> được xử lý y hệt <c>Draft</c>: cả hai đều là trạng thái "chưa chạy", để nguyên
    /// thì đúng cái bug cần sửa vẫn còn — chỉ khác điểm xuất phát.
    ///
    /// ⚠️ <paramref name="today"/> tính theo <b>UTC</b> — khớp với phần còn lại của codebase và với
    /// hàm <c>derivedStatus()</c> FE đang dùng (<c>new Date().toISOString()</c> cũng là UTC).
    /// Hệ quả: ở VN (UTC+7) giải chuyển trạng thái lúc 07:00 giờ địa phương.
    /// </summary>
    public static string? ResolveByDate(
        string? currentStatus,
        DateOnly startDate,
        DateOnly endDate,
        DateOnly today)
    {
        if (currentStatus is not (Draft or Open or Ongoing))
            return null;

        string? target =
            today > endDate ? Finished :
            today >= startDate ? Ongoing :
            null;

        return target is null || string.Equals(target, currentStatus, StringComparison.Ordinal)
            ? null
            : target;
    }
}
