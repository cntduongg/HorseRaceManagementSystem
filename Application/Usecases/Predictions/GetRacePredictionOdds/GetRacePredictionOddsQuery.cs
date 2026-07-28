using MediatR;

namespace Application.Usecases.Predictions.GetRacePredictionOdds;

// BetAmount là tham số TÍNH TOÁN, không ghi DB và không giữ chỗ trong pool: nó chỉ để trả về
// EffectiveOdds = giá sẽ bị khóa nếu spectator đặt đúng số tiền đó vào entry tương ứng.
public sealed record GetRacePredictionOddsQuery(
    int RaceId,
    decimal? BetAmount = null
) : IRequest<RacePredictionOddsResponse>;