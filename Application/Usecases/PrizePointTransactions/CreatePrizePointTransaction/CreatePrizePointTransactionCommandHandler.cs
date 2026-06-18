using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;

namespace Application.Usecases.PrizePointTransactions.CreatePrizePointTransaction;

public sealed class CreatePrizePointTransactionCommandHandler
    : IRequestHandler<CreatePrizePointTransactionCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreatePrizePointTransactionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreatePrizePointTransactionCommand request,
        CancellationToken cancellationToken)
    {
        if (request.TournamentId <= 0)
            throw new InvalidOperationException("TournamentId is invalid.");

        if (request.RaceId <= 0)
            throw new InvalidOperationException("RaceId is invalid.");

        if (request.EntryId <= 0)
            throw new InvalidOperationException("EntryId is invalid.");

        if (request.UserId <= 0)
            throw new InvalidOperationException("UserId is invalid.");

        if (string.IsNullOrWhiteSpace(request.SourceType))
            throw new InvalidOperationException("SourceType is required.");

        if (request.Points < 0)
            throw new InvalidOperationException("Points must be >= 0.");

        var entity = new PrizePointTransaction
        {
            TournamentId = request.TournamentId,
            RaceId = request.RaceId,
            EntryId = request.EntryId,
            UserId = request.UserId,
            SourceType = request.SourceType.Trim(),
            FinalPosition = request.FinalPosition,
            Points = request.Points,
            TransactionType = request.TransactionType,
            RollbackOfId = request.RollbackOfId,
            CreatedAt = DateTime.UtcNow
        };

        _context.PrizePointTransactions.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return entity.PrizePointTransactionId;
    }
}