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
            throw new InvalidOperationException("WalletId is invalid.");

        if (request.SpectatorId <= 0)
            throw new InvalidOperationException("SpectatorId is invalid.");

        if (string.IsNullOrWhiteSpace(request.Type))
            throw new InvalidOperationException("Type is required.");

        if (request.Amount == 0)
            throw new InvalidOperationException("Amount cannot be zero.");

        if (request.BalanceAfter < 0)
            throw new InvalidOperationException("BalanceAfter cannot be negative.");

        var wallet = await _context.PointWallets
            .FirstOrDefaultAsync(
                x => x.WalletId == request.WalletId,
                cancellationToken);

        if (wallet is null)
            throw new InvalidOperationException("Wallet not found.");

        if (wallet.SpectatorId != request.SpectatorId)
            throw new InvalidOperationException("Wallet does not belong to the spectator.");

        var spectatorExists = await _context.Spectators
            .AnyAsync(
                x => x.UserId == request.SpectatorId,
                cancellationToken);

        if (!spectatorExists)
            throw new InvalidOperationException("Spectator not found.");

        if (request.PredictionId.HasValue)
        {
            var predictionExists = await _context.Predictions
                .AnyAsync(
                    x => x.PredictionId == request.PredictionId.Value,
                    cancellationToken);

            if (!predictionExists)
                throw new InvalidOperationException("Prediction not found.");
        }

        if (request.SettlementRunId.HasValue)
        {
            var settlementExists = await _context.SettlementRuns
                .AnyAsync(
                    x => x.SettlementRunId == request.SettlementRunId.Value,
                    cancellationToken);

            if (!settlementExists)
                throw new InvalidOperationException("SettlementRun not found.");
        }

        if (request.AdminId.HasValue)
        {
            var adminExists = await _context.Users
                .AnyAsync(
                    x => x.UserId == request.AdminId.Value,
                    cancellationToken);

            if (!adminExists)
                throw new InvalidOperationException("Admin not found.");
        }

        if (request.RollbackOfTransactionId.HasValue)
        {
            var rollbackExists = await _context.WalletTransactions
                .AnyAsync(
                    x => x.WalletTransactionId == request.RollbackOfTransactionId.Value,
                    cancellationToken);

            if (!rollbackExists)
                throw new InvalidOperationException("Rollback transaction not found.");
        }

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