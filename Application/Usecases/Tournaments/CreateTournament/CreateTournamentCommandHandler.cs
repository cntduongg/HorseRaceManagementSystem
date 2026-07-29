using Application.Common.Interfaces;
using Domain.Aggregates.Constants;
using Domain.Aggregates.Entities;
using MediatR;

namespace Application.Usecases.Tournaments.CreateTournament;

public sealed class CreateTournamentCommandHandler
    : IRequestHandler<CreateTournamentCommand, int>
{
    private readonly IApplicationDbContext _context;

    public CreateTournamentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<int> Handle(
        CreateTournamentCommand request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Tournament name is required.");

        if (request.StartDate > request.EndDate)
            throw new InvalidOperationException("StartDate cannot be later than EndDate.");

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        // Mặc định Draft, nhưng nếu tạo giải mà ngày bắt đầu đã tới/qua rồi thì set đúng ngay từ đầu —
        // đợi worker quét lần sau sẽ hiển thị sai (giải đang diễn ra mà nằm ở tab "Upcoming").
        var status = TournamentStatus.ResolveByDate(
                         TournamentStatus.Draft,
                         request.StartDate,
                         request.EndDate,
                         today)
                     ?? TournamentStatus.Draft;

        var tournament = new Tournament
        {
            Name = request.Name.Trim(),
            Description = request.Description,
            Location = request.Location,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = status,
            CreatedAt = now
        };

        _context.Tournaments.Add(tournament);

        await _context.SaveChangesAsync(cancellationToken);

        return tournament.TournamentId;
    }
}