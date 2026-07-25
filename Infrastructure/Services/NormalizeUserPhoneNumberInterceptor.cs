using Domain.Aggregates.Entities;
using Domain.Common.PhoneNumbers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Infrastructure.Services;

public sealed class NormalizeUserPhoneNumberInterceptor
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        NormalizeChangedUsers(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>>
        SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
    {
        NormalizeChangedUsers(eventData.Context);

        return base.SavingChangesAsync(
            eventData,
            result,
            cancellationToken);
    }

    private static void NormalizeChangedUsers(
        DbContext? dbContext)
    {
        if (dbContext is null)
        {
            return;
        }

        var changedUsers = dbContext.ChangeTracker
            .Entries<User>()
            .Where(ShouldNormalizePhoneNumber)
            .ToList();

        foreach (var userEntry in changedUsers)
        {
            var user = userEntry.Entity;

            user.PhoneNumber =
                string.IsNullOrWhiteSpace(user.PhoneNumber)
                    ? null
                    : user.PhoneNumber.Trim();

            user.NormalizedPhoneNumber =
                PhoneNumberNormalizer.Normalize(
                    user.PhoneNumber);
        }
    }

    private static bool ShouldNormalizePhoneNumber(
        EntityEntry<User> entry)
    {
        if (entry.State == EntityState.Added)
        {
            return true;
        }

        if (entry.State != EntityState.Modified)
        {
            return false;
        }

        return entry.Property(user => user.PhoneNumber)
            .IsModified;
    }
}