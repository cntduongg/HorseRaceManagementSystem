using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Admin.PointsManagement;

// GET /api/admin/points/balances?search=&page=&pageSize=
public sealed record GetPointBalancesQuery(string? Search, int Page, int PageSize)
    : IRequest<PointBalancesResult>;

public sealed record PointBalanceItem(
    int UserId,
    string UserName,
    string UserEmail,
    string Role,
    decimal Balance,
    bool IsFrozen);

public sealed record PointBalancesResult(IReadOnlyList<PointBalanceItem> Items, int Total);

public sealed class GetPointBalancesQueryHandler
    : IRequestHandler<GetPointBalancesQuery, PointBalancesResult>
{
    private readonly IApplicationDbContext _context;

    public GetPointBalancesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<PointBalancesResult> Handle(
        GetPointBalancesQuery request,
        CancellationToken cancellationToken)
    {
        var page = request.Page <= 0 ? 1 : request.Page;
        var pageSize = request.PageSize is <= 0 or > 200 ? 50 : request.PageSize;

        var query =
            from w in _context.PointWallets.AsNoTracking()
            join u in _context.Users.AsNoTracking() on w.SpectatorId equals u.UserId
            select new { w.SpectatorId, u.FullName, u.Email, w.Balance, w.IsFrozen };

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var s = request.Search.Trim().ToLower();
            query = query.Where(x =>
                x.FullName.ToLower().Contains(s) || x.Email.ToLower().Contains(s));
        }

        var total = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(x => x.Balance)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new PointBalanceItem(
                x.SpectatorId, x.FullName, x.Email, "SPECTATOR", x.Balance, x.IsFrozen))
            .ToListAsync(cancellationToken);

        return new PointBalancesResult(items, total);
    }
}
