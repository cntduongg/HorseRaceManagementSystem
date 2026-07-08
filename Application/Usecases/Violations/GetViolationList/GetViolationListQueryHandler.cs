using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.GetViolationList;

public sealed class GetViolationListQueryHandler
    : IRequestHandler<GetViolationListQuery, List<ViolationListItemResponse>>
{
    private readonly IApplicationDbContext _context;

    public GetViolationListQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ViolationListItemResponse>> Handle(
        GetViolationListQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Violations
            .AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new ViolationListItemResponse(
                x.ViolationId,
                x.RaceId,
                x.Entry.Race.Name,
                x.LegNumber,
                x.EntryId,
                x.Entry.Jockey.FullName,
                x.Entry.Horse.Name,
                x.ViolationType,
                x.Description,
                x.Penalty,
                x.Status,
                x.ReportedByRefereeId,
                x.ReviewedByAdminId,
                x.ReviewedAt,
                x.AdminNote,
                x.CreatedAt
            ))
            .ToListAsync(cancellationToken);
    }
}