using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.WalletTransactions.CreateWalletTransaction;

public sealed class CreateWalletTransactionCommandHandler
    : IRequestHandler<CreateWalletTransactionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateWalletTransactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateWalletTransactionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.WalletId <= 0)
            throw new InvalidOperationException("WalletId invalid.");

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Type is required.");

        var walletExists = await _context.PointWallets
            .AnyAsync(x => x.WalletId == request.WalletId, cancellationToken);

        if (!walletExists)
            throw new InvalidOperationException("Wallet not found.");

        var entity = new WalletTransaction
        {
            WalletId = request.WalletId,
            SpectatorId = request.SpectatorId,
            PredictionId = request.PredictionId,
            SettlementRunId = request.SettlementRunId,
            AdminId = request.AdminId,
            Type = request.Type.Trim(),
            Amount = request.Amount,
            BalanceAfter = request.BalanceAfter,
            Reason = request.Reason,
            RollbackOfTransactionId = request.RollbackOfTransactionId,
            CreatedAt = DateTime.UtcNow
        };

        _context.WalletTransactions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.WalletTransactionId;
    }
}