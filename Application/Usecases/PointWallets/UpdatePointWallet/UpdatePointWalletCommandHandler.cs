using MediatR;

namespace Application.Usecases.PointWallets.UpdatePointWallet;

public sealed class UpdatePointWalletCommandHandler
    : IRequestHandler<UpdatePointWalletCommand, bool>
{
    public Task<bool> Handle(
        UpdatePointWalletCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Update database

        return Task.FromResult(true);
    }
}