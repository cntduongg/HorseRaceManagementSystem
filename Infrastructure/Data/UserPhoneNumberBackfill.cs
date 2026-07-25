using Domain.Aggregates.Entities;
using Domain.Common.PhoneNumbers;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Data;

public static class UserPhoneNumberBackfill
{
    public static async Task RunAsync(
        ApplicationDbContext db,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        var users = await db.Users
            .Where(u => u.NormalizedPhoneNumber == null && u.PhoneNumber != null)
            .ToListAsync(cancellationToken);

        if (users.Count == 0)
            return;

        var normalizedToUserIds = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var unparseableUserIds = new List<int>();
        var applied = 0;

        foreach (var user in users)
        {
            string? normalized;
            try
            {
                normalized = PhoneNumberNormalizer.Normalize(user.PhoneNumber);
            }
            catch (InvalidPhoneNumberException)
            {
                unparseableUserIds.Add(user.UserId);
                continue;
            }

            if (normalized is null)
                continue;

            if (!normalizedToUserIds.TryGetValue(normalized, out var list))
            {
                list = new List<int>();
                normalizedToUserIds[normalized] = list;
            }

            list.Add(user.UserId);
        }

        var duplicateUserIds = new List<int>();
        foreach (var (normalized, ids) in normalizedToUserIds)
        {
            if (ids.Count > 1)
            {
                duplicateUserIds.AddRange(ids);
                logger.LogWarning(
                    "Phone backfill skipped duplicate normalized {Normalized} for UserIds: {UserIds}",
                    normalized,
                    string.Join(", ", ids));
                continue;
            }

            var user = users.First(u => u.UserId == ids[0]);
            user.NormalizedPhoneNumber = normalized;
            applied++;
        }

        if (applied > 0)
            await db.SaveChangesAsync(cancellationToken);

        if (unparseableUserIds.Count > 0)
        {
            logger.LogWarning(
                "Phone backfill skipped {Count} user(s) with unparseable numbers: {UserIds}",
                unparseableUserIds.Count,
                string.Join(", ", unparseableUserIds));
        }

        if (duplicateUserIds.Count > 0)
        {
            logger.LogWarning(
                "Phone backfill skipped {Count} user(s) due to duplicate normalized values",
                duplicateUserIds.Count);
        }

        logger.LogInformation(
            "Phone backfill complete: applied={Applied}, unparseable={Unparseable}, duplicateGroups={Duplicate}",
            applied,
            unparseableUserIds.Count,
            duplicateUserIds.Count);
    }
}
