using Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Application.Usecases.Races;

public static class RaceRefereeValidator
{
    public static async Task EnsureRefereesAsync(
        IApplicationDbContext context,
        int referee1Id,
        int referee2Id,
        CancellationToken cancellationToken)
    {
        var ids = new[] { referee1Id, referee2Id };
        var users = await context.Users
            .AsNoTracking()
            .Include(u => u.Role)
            .Where(u => ids.Contains(u.UserId))
            .ToListAsync(cancellationToken);

        foreach (var id in ids)
        {
            var user = users.FirstOrDefault(u => u.UserId == id);
            if (user is null)
                throw new InvalidOperationException($"Referee user {id} was not found.");

            if (!string.Equals(user.Role?.Code, "REFEREE", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException(
                    $"User {id} is not a referee (role: {user.Role?.Code ?? "unknown"}).");
        }
    }
}
