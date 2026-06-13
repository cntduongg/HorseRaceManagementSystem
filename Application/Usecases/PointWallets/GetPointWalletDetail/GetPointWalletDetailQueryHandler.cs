using MediatR;

namespace Application.Usecases.PointWallets.GetPointWalletDetail;

public sealed class GetPointWalletDetailQueryHandler
    : IRequestHandler<
        GetPointWalletDetailQuery,
        PointWalletDetailResponse?>
{
    public Task<PointWalletDetailResponse?> Handle(
        GetPointWalletDetailQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var response = new PointWalletDetailResponse(
            request.WalletId,
            1,
            100,
            false
        );

        return Task.FromResult<PointWalletDetailResponse?>(response);
    }
}