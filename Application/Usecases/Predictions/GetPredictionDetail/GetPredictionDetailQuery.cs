using MediatR;

namespace Application.Usecases.Predictions.GetPredictionDetail;

// ViewerSpectatorId: null → ADMIN; có giá trị → chỉ đọc được cược của chính mình.
// Response có `FirstEntryId` + `OddsLocked1` — tức cửa đặt và giá đã khóa, đúng thứ không
// được để người khác (nhất là REFEREE) đọc trong lúc race chưa chạy.
public sealed record GetPredictionDetailQuery(
    int PredictionId,
    int? ViewerSpectatorId
) : IRequest<PredictionDetailResponse?>;