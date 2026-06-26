using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

// GET /api/admin/points/transactions?search=&type=&page=&pageSize=
public sealed record GetPointTransactionsQuery(
    string? Search, string? Type, int Page, int PageSize)
    : IRequest<PointTransactionsResult>;

public sealed record PointTransactionItem(
    int TransactionId,
    int UserId,
    string UserName,
    string UserEmail,
    string Type,
    decimal Amount,
    decimal BalanceAfter,
    string? Reason,
    DateTime CreatedAt);

public sealed record PointTransactionsResult(IReadOnlyList<PointTransactionItem> Items, int Total);

public sealed class GetPointTransactionsQueryHandler
    : IRequestHandler<GetPointTransactionsQuery, PointTransactionsResult>
{
    private readonly IApplicationDbContext _context;

    public GetPointTransactionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PointTransactionsResult> Handle(
        GetPointTransactionsQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize is <= 0 or > 200 ? 50 : request.PageSize;

        var query =
            from t in _context.WalletTransactions.AsNoTracking()
            join u in _context.Users.AsNoTracking() on t.SpectatorId equals u.UserId
            select new
            {
                t.WalletTransactionId,
                t.SpectatorId,
                u.FullName,
                u.Email,
                t.Type,
                t.Amount,
                t.BalanceAfter,
                t.Reason,
                t.CreatedAt
            };

        if (!string.IsNullOrWhiteSpace(request.Type) && request.Type != "All")
            query = query.Where(x => x.Type == request.Type);

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(s) || x.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PointTransactionItem(
                x.WalletTransactionId, x.SpectatorId, x.FullName, x.Email,
                x.Type, x.Amount, x.BalanceAfter, x.Reason, x.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PointTransactionsResult(items, total);
    }
}
