using MediatR;

namespace Application.Usecases.PointWallets.DeletePointWallet;

public sealed class DeletePointWalletCommandHandler
    : IRequestHandler<DeletePointWalletCommand, bool>
{
    public Task<bool> Handle(
        DeletePointWalletCommand request,
        CancellationToken cancellationToken)
    {
        // TODO: Delete from database

        return Task.FromResult(true);
    }
}