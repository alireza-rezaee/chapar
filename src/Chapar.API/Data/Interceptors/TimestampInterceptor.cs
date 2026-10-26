using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Chapar.API.Models.IHas;

namespace Chapar.API.Data.Interceptors;

public sealed class TimestampInterceptor
    : SaveChangesInterceptor
{
    private static readonly HashSet<string> CreationPropertyNames =
    [
        nameof(IHasCreatedAt.CreatedAt),
        nameof(IHasAt.At)
    ];

    private static readonly HashSet<string> ModificationPropertyNames =
    [
        nameof(IHasUpdatedAt.UpdatedAt),
    ];

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateTimestamps(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    private static void UpdateTimestamps(DbContext? context)
    {
        if (context is null)
            return;

        DateTimeOffset utcNow = DateTimeOffset.UtcNow;

        foreach (EntityEntry entry in context.ChangeTracker.Entries())
        {
            if (entry.State is not (EntityState.Added or EntityState.Modified))
                continue;

            switch (entry.State)
            {
                case EntityState.Added:
                    HandleAddedEntity(entry, utcNow);
                    break;

                case EntityState.Modified:
                    HandleModifiedEntity(entry, utcNow);
                    break;
            }
        }
    }

    private static void HandleAddedEntity(EntityEntry entry, DateTimeOffset utcNow)
    {
        foreach (PropertyEntry prop in entry.Properties)
        {
            string propName = prop.Metadata.Name;

            if (CreationPropertyNames.Contains(propName)
                && IsDateTimeOffsetProperty(prop))
            {
                if (IsDefaultValue(prop.CurrentValue))
                    prop.CurrentValue = utcNow;
            }

            if (ModificationPropertyNames.Contains(propName)
                && IsDateTimeOffsetProperty(prop))
            {
                if (IsDefaultValue(prop.CurrentValue))
                    prop.CurrentValue = utcNow;
            }
        }
    }

    private static void HandleModifiedEntity(EntityEntry entry, DateTimeOffset utcNow)
    {
        foreach (PropertyEntry prop in entry.Properties)
        {
            string propName = prop.Metadata.Name;

            if (ModificationPropertyNames.Contains(propName)
                && IsDateTimeOffsetProperty(prop))
            {
                prop.CurrentValue = utcNow;
            }

            if (CreationPropertyNames.Contains(propName)
                && IsDateTimeOffsetProperty(prop))
            {
                prop.IsModified = false;
            }
        }
    }

    private static bool IsDateTimeOffsetProperty(PropertyEntry property)
    {
        Type type = property.Metadata.ClrType;
        return type == typeof(DateTimeOffset) || type == typeof(DateTimeOffset?);
    }

    private static bool IsDefaultValue(object? value)
    {
        return value switch
        {
            null => true,
            DateTimeOffset dto => dto == default,
            _ => false
        };
    }
}
