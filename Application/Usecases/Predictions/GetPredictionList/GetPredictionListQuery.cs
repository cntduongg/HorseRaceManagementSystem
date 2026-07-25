using MediatR;

namespace Application.Usecases.Predictions.GetPredictionList;

// ViewerSpectatorId: null → ADMIN (mọi lệnh cược); có giá trị → chỉ cược của chính mình.
//
// Trước đây trả TOÀN BỘ prediction cho mọi user đã đăng nhập rồi để FE lọc theo `spectatorId`.
// Ngoài chuyện lộ dữ liệu, nó còn hỏng nghiệp vụ: cửa cược mở khi race `Scheduled`, nên một
// khán giả đọc được toàn bộ lệnh cược đang chờ của người khác **trước khi race chạy** —
// và REFEREE (người nhập kết quả) cũng đọc được ai đặt cửa nào.
// Bảng xếp hạng cược dùng `GET /api/leaderboards/spectators` (chỉ số liệu tổng hợp).
public sealed record GetPredictionListQuery(int? ViewerSpectatorId)
    : IRequest<List<PredictionListItemResponse>>;