using MediatR;

namespace Application.Usecases.Violations.GetViolationList;

// ViewerRefereeId: null → ADMIN (mọi báo cáo); có giá trị → chỉ báo cáo do chính referee đó gửi.
//
// Trước đây trả TOÀN BỘ bảng Violations cho bất kỳ ai gọi được endpoint (REFEREE hoặc ADMIN),
// nghĩa là trọng tài A đọc được báo cáo của trọng tài B: race nào, entry nào, lý do, án đề xuất,
// trạng thái duyệt. Flow 6 nói rõ vi phạm KHÔNG blind, nhưng "không blind" chỉ có nghĩa là
// một mình người báo cáo quyết định nội dung — không có nghĩa các trọng tài được đọc chéo của nhau.
// Cùng pattern scope dữ liệu cá nhân đã áp cho PointWallet/WalletTransaction/Prediction (T-25).
public sealed record GetViolationListQuery(int? ViewerRefereeId)
    : IRequest<List<ViolationListItemResponse>>;
