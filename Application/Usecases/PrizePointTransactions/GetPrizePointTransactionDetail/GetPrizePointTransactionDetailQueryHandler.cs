using MediatR;

namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionDetail;

public sealed class GetPrizePointTransactionDetailQueryHandler
    : IRequestHandler<GetPrizePointTransactionDetailQuery,
        PrizePointTransactionDetailResponse?>
{
    public Task<PrizePointTransactionDetailResponse?> Handle(
        GetPrizePointTransactionDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new PrizePointTransactionDetailResponse(
            request.PrizePointTransactionId,
            1,
            1,
            1,
            1,
            1,
            "HorseOwner",
            1,
            100,
            "Awarded"
        );

        return Task.FromResult<PrizePointTransactionDetailResponse?>(response);
    }
}