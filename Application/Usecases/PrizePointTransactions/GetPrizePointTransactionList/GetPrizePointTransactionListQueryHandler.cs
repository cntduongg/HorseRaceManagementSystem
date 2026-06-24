using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.PrizePointTransactions.GetPrizePointTransactionList;

public sealed class GetPrizePointTransactionListQueryHandler
    : IRequestHandler<GetPrizePointTransactionListQuery,
        List<PrizePointTransactionListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetPrizePointTransactionListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<PrizePointTransactionListItemResponse>> Handle(
        GetPrizePointTransactionListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.PrizePointTransactions
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new PrizePointTransactionListItemResponse(
                x.PrizePointTransactionId,
                x.UserId,
                x.SourceType,
                x.Points,
                x.TransactionType.ToString()
            ))
            .ToListAsync(cancellationToken);
    }
}