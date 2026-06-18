using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionDetail;

public sealed class GetPrizePointTransactionDetailQueryHandler
    : IRequestHandler<GetPrizePointTransactionDetailQuery,
        PrizePointTransactionDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetPrizePointTransactionDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PrizePointTransactionDetailResponse?> Handle(
        GetPrizePointTransactionDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.PrizePointTransactions
            .AsNoTracking()
            .Where(x => x.PrizePointTransactionId == request.PrizePointTransactionId)
            .Select(x => new PrizePointTransactionDetailResponse(
                x.PrizePointTransactionId,
                x.TournamentId,
                x.RaceId,
                x.EntryId,
                x.UserId,
                x.SourceType,
                x.FinalPosition,
                x.Points,
                x.TransactionType.ToString(),
                x.RollbackOfId
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}