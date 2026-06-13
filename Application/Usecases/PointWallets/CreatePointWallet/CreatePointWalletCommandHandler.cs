using MediatR;

namespace Application.Usecases.PointWallets.CreatePointWallet;

public sealed class CreatePointWalletCommandHandler
    : IRequestHandler<CreatePointWalletCommand, int>
{
    public Task<int> Handle(
        CreatePointWalletCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Validate spectator exists

        // TODO: Save to database

        var walletId = 1;

        return Task.FromResult(walletId);
    }
}