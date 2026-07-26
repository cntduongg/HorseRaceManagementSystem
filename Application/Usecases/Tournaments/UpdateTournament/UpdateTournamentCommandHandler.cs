using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Tournaments.UpdateTournament;

public sealed class UpdateTournamentCommandHandler
    : IRequestHandler<UpdateTournamentCommand, bool>
{
    private readonly IApplicationDbContext _context;

    public UpdateTournamentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<bool> Handle(
        UpdateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Tournament name is required.");

        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("StartDate cannot be later than EndDate.");

        // Trước đây gán thẳng request.Status vào entity, không kiểm gì: gõ sai ("Onging", "ongoing",
        // "Active"…) là ghi luôn vào DB, FE không map được nên rơi về nhãn mặc định, và worker
        // auto-status cũng bỏ qua vì không nhận ra giá trị đó.
        var status = request.Status?.Trim();
        if (!TournamentStatus.IsValid(status))
            throw new InvalidOperationException(
                $"Status must be one of: {string.Join(", ", TournamentStatus.All)}.");

        var tournament = await _context.Tournaments
            .FirstOrDefaultAsync(
                x => x.TournamentId == request.TournamentId,
                cancellationToken);

        if (tournament is null)
            return false;

        tournament.Name = request.Name.Trim();
        tournament.Description = request.Description;
        tournament.Location = request.Location;
        tournament.StartDate = request.StartDate;
        tournament.EndDate = request.EndDate;
        tournament.LogoUrl = request.LogoUrl;
        // Giữ nguyên quyền quyết định của Admin ở đây (kể cả Finished sớm trước EndDate, hoặc
        // Cancelled). Worker chỉ đẩy TIẾN từ Draft/Open/Ongoing và không bao giờ đụng vào
        // Cancelled/Finished, nên lựa chọn của Admin không bị ghi đè ngược.
        tournament.Status = status!;
        tournament.CancelReason = request.CancelReason;
        tournament.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        return true;
    }
}