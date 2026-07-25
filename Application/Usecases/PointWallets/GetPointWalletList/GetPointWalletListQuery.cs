using MediatR;

namespace Application.Usecases.PointWallets.GetPointWalletList;

// ViewerSpectatorId:
//   null       → ADMIN, thấy toàn bộ ví.
//   có giá trị → chỉ thấy ví của chính user đó.
//
// Trước đây query này trả TOÀN BỘ ví cho mọi user đã đăng nhập rồi để FE tự lọc client-side
// (`api/spectator.js` → `getMyWallet`), tức bất kỳ khán giả nào cũng đọc được số dư của người
// khác. Lọc bắt buộc phải nằm ở server — lọc ở FE chỉ là trang trí.
public sealed record GetPointWalletListQuery(int? ViewerSpectatorId)
    : IRequest<List<PointWalletListItemResponse>>;
