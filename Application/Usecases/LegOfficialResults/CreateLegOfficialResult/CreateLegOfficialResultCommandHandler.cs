using Application.Common.Interfaces;
using Domain.Aggregates.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.LegOfficialResults.CreateLegOfficialResult;

public sealed class CreateLegOfficialResultCommandHandler
    : IRequestHandler<CreateLegOfficialResultCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public CreateLegOfficialResultCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        CreateLegOfficialResultCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.ResultStatus))
            throw new InvalidOperationException("ResultStatus is required.");

        if (string.IsNullOrWhiteSpace(request.ConfirmationType))
            throw new InvalidOperationException("ConfirmationType is required.");

        var exists = await _context.LegOfficialResults.AnyAsync(x =>
            x.RaceId == request.RaceId &&
            x.LegNumber == request.LegNumber &&
            x.EntryId == request.EntryId,
            cancellationToken);

        if (exists)
            throw new InvalidOperationException("LegOfficialResult already exists.");

        var entity = new LegOfficialResult
        {
            RaceId = request.RaceId,
            LegNumber = request.LegNumber,
            EntryId = request.EntryId,
            FinishPosition = request.FinishPosition,
            ResultStatus = request.ResultStatus.Trim(),
            LegPoints = request.LegPoints,
            ConfirmationType = request.ConfirmationType.Trim(),
            ConfirmedAt = DateTime.UtcNow,
            ConfirmedByAdminId = request.ConfirmedByAdminId,
            OverrideReason = request.OverrideReason
        };

        _context.LegOfficialResults.Add(entity);
        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}