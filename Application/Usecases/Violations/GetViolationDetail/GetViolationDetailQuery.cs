using MediatR;

namespace Application.Usecases.Violations.GetViolationDetail;

// ViewerRefereeId: null → ADMIN (mọi báo cáo); có giá trị → chỉ báo cáo do chính referee đó gửi.
// Cùng lý do với GetViolationListQuery: siết list mà bỏ ngỏ detail thì chỉ cần đoán id là đọc được
// báo cáo của trọng tài khác. Không thuộc mình → handler trả null → controller trả 404 (không phải
// 403) để không lộ id nào có tồn tại, giống pattern T-25.
public sealed record GetViolationDetailQuery(
    int ViolationId,
    int? ViewerRefereeId
) : IRequest<ViolationDetailResponse?>;
