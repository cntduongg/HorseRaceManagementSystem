using Application.Common;

namespace Application.Usecases.Admin.RevokeHorse;

// Reason là tùy chọn (khác Reject — Reject bắt buộc lý do). Flow 1: Admin có thể
// revoke ngựa đã duyệt bất kỳ lúc nào; handler tự điền lý do mặc định nếu bỏ trống.
public sealed record RevokeHorseCommand(
    int HorseId,
    int AdminId,
    string? Reason
) : ICommand<RevokeHorseResponse>;