using System.Text.RegularExpressions;

namespace Api.Cors;

/// <summary>
/// Khớp header <c>Origin</c> của request với danh sách cấu hình <c>Cors:AllowedOrigins</c>.
///
/// Lý do tồn tại: Vercel sinh **một hostname mới cho mỗi lần deploy**
/// (<c>horse-race-fe-git-&lt;branch&gt;-&lt;scope&gt;.vercel.app</c>,
/// <c>horse-race-fe-&lt;hash&gt;-&lt;scope&gt;.vercel.app</c>…), trong khi
/// <c>WithOrigins()</c> chỉ so khớp **tuyệt đối**. Hệ quả: bản production alias thì chạy,
/// còn mọi bản preview đều bị chặn CORS — mà lỗi lại im lặng (server trả 204 không kèm
/// header <c>Access-Control-Allow-Origin</c>, browser mới là chỗ báo lỗi).
///
/// Nên hỗ trợ ký tự đại diện <c>*</c>, ví dụ <c>https://horse-race-fe-*.vercel.app</c>.
/// <c>*</c> chỉ thay cho **một nhãn tên miền**: không nuốt dấu <c>.</c> hay <c>/</c>, nên
/// <c>https://*.vercel.app</c> KHÔNG khớp <c>https://evil.com/x.vercel.app</c> hay
/// <c>https://a.b.vercel.app</c>. Vẫn giữ được <c>AllowCredentials()</c> vì
/// <c>SetIsOriginAllowed</c> echo lại đúng origin của request (không phải <c>*</c>).
/// </summary>
public sealed class AllowedOriginMatcher
{
    private static readonly TimeSpan MatchTimeout = TimeSpan.FromMilliseconds(100);

    private readonly HashSet<string> _exactOrigins = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Regex> _patterns = [];
    private readonly List<string> _configuredEntries = [];

    public AllowedOriginMatcher(IEnumerable<string> configuredOrigins)
    {
        foreach (var raw in configuredOrigins)
        {
            // Origin trong header không bao giờ có dấu '/' cuối — cắt để cấu hình gõ thừa vẫn khớp.
            var origin = raw?.Trim().TrimEnd('/');
            if (string.IsNullOrEmpty(origin))
                continue;

            _configuredEntries.Add(origin);

            if (origin.Contains('*'))
                _patterns.Add(BuildPattern(origin));
            else
                _exactOrigins.Add(origin);
        }
    }

    /// <summary>Danh sách đã chuẩn hóa — dùng để log lúc khởi động.</summary>
    public IReadOnlyList<string> ConfiguredEntries => _configuredEntries;

    public bool HasAny => _configuredEntries.Count > 0;

    public bool IsAllowed(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return false;

        var normalized = origin.TrimEnd('/');

        if (_exactOrigins.Contains(normalized))
            return true;

        foreach (var pattern in _patterns)
        {
            if (pattern.IsMatch(normalized))
                return true;
        }

        return false;
    }

    private static Regex BuildPattern(string origin)
    {
        // Escape toàn bộ rồi mới mở lại riêng '*' → phần còn lại luôn là literal.
        var escaped = Regex.Escape(origin).Replace("\\*", "[^./]*");

        return new Regex(
            $"^{escaped}$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
            MatchTimeout);
    }
}
