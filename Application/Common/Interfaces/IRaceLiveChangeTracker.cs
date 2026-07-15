namespace Application.Common.Interfaces;

/// <summary>
/// Handler đánh dấu Race vừa đổi trạng thái; <see cref="RaceLiveBroadcastBehavior{TRequest,TResponse}"/>
/// đẩy snapshot SAU khi đã commit. Scoped theo request.
/// </summary>
public interface IRaceLiveChangeTracker
{
    /// <summary>Đánh dấu Race cần đẩy snapshot mới. Gọi trong handler, an toàn khi gọi nhiều lần.</summary>
    void MarkChanged(int raceId);

    /// <summary>
    /// Lấy & xóa danh sách đã đánh dấu. Phải drain TRƯỚC khi đẩy — xem comment chống đệ quy
    /// trong RaceLiveBroadcastBehavior.
    /// </summary>
    IReadOnlyCollection<int> Drain();
}
