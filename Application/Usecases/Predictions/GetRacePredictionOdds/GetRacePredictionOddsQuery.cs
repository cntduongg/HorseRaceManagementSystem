using MediatR;

namespace Application.Usecases.Predictions.GetRacePredictionOdds;

// Bảng giá cược của một race — đúng một giá mỗi ngựa (odds công bố Admin đã duyệt).
// Không còn tham số BetAmount: giá không phụ thuộc số tiền đặt nữa nên không có gì để preview.
public sealed record GetRacePredictionOddsQuery(
    int RaceId
) : IRequest<RacePredictionOddsResponse>;
