using MediatR;

namespace Application.Usecases.PointWallets.GetPointWalletList;

public sealed class GetPointWalletListQueryHandler
    : IRequestHandler<
        GetPointWalletListQuery,
        List<PointWalletListItemResponse>>
{
    public Task<List<PointWalletListItemResponse>> Handle(
        GetPointWalletListQuery request,
        CancellationToken cancellationToken)
    {
        // TODO: Load from database

        var wallets = new List<PointWalletListItemResponse>
        {
            new(1, 1, 100, false),
            new(2, 2, 250, false)
        };

        return Task.FromResult(wallets);
    }
}