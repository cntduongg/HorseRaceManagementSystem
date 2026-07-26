using Application.Common.Interfaces;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Violations.GetViolationDetail;

public sealed class GetViolationDetailQueryHandler
    : IRequestHandler<GetViolationDetailQuery, ViolationDetailResponse?>
{
    private readonly IApplicationDbContext _context;

    public GetViolationDetailQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ViolationDetailResponse?> Handle(
        GetViolationDetailQuery request,
        CancellationToken cancellationToken)
    {
        return await _context.Violations
            .Where(x => x.ViolationId == request.ViolationId)
            .Where(x => request.ViewerRefereeId == null ||
                        x.ReportedByRefereeId == request.ViewerRefereeId)
            .Select(x => new ViolationDetailResponse(
                x.ViolationId,
                x.RaceId,
                x.LegNumber,
                x.EntryId,
                x.ReportedByRefereeId,
                x.ViolationType,
                x.Description,
                x.Penalty,
                x.Status,
                x.ReviewedByAdminId,
                x.ReviewedAt,
                x.AdminNote,
                x.CreatedAt
            ))
            .FirstOrDefaultAsync(cancellationToken);
    }
}