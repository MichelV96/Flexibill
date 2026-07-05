using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MVSoftware.Flexibill.Application.Common.Interfaces;
using MVSoftware.Flexibill.Domain.Common;
using MVSoftware.Flexibill.Infrastructure.Persistence.Auditing;

namespace MVSoftware.Flexibill.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Technisch Ontwerp, hoofdstuk 15: legt wijzigingen aan <see cref="IAuditable"/>-aggregates
/// vast in een append-only AuditLog-tabel, in dezelfde SaveChanges-aanroep/transactie als de
/// business-wijziging zelf. Werkt op aggregate-root-niveau: een wijziging die uitsluitend een
/// owned child-collectie raakt (bv. alleen een InvoiceLine coderen, zonder dat een scalar
/// property van de Invoice zelf verandert) genereert nog geen aparte audit-regel - bekende
/// beperking voor een latere stap.
/// </summary>
public sealed class AuditInterceptor(ICurrentUserContext currentUserContext, IAuditSourceProvider auditSourceProvider, IDateTimeProvider dateTimeProvider)
    : SaveChangesInterceptor
{
    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        if (eventData.Context is not null)
        {
            AddAuditEntries(eventData.Context);
        }

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AddAuditEntries(DbContext context)
    {
        var (userId, displayName) = ResolveCurrentUser();

        var auditableEntries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is IAuditable && e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        foreach (var entry in auditableEntries)
        {
            var scalarProperties = entry.Properties.Where(p => !p.Metadata.IsShadowProperty() || p.Metadata.IsPrimaryKey()).ToList();

            string? oldValuesJson = entry.State == EntityState.Added
                ? null
                : JsonSerializer.Serialize(scalarProperties.ToDictionary(p => p.Metadata.Name, p => p.OriginalValue));
            string? newValuesJson = entry.State == EntityState.Deleted
                ? null
                : JsonSerializer.Serialize(scalarProperties.ToDictionary(p => p.Metadata.Name, p => p.CurrentValue));

            var action = entry.State switch
            {
                EntityState.Added => AuditAction.Added,
                EntityState.Deleted => AuditAction.Deleted,
                _ => AuditAction.Modified
            };

            var entityId = (Guid)entry.Property(nameof(Entity.Id)).CurrentValue!;

            context.Set<AuditLogEntry>().Add(AuditLogEntry.Create(
                entry.Entity.GetType().Name,
                entityId,
                action,
                userId,
                displayName,
                auditSourceProvider.Source,
                dateTimeProvider.UtcNow,
                oldValuesJson,
                newValuesJson));
        }
    }

    private (Guid? UserId, string? DisplayName) ResolveCurrentUser()
    {
        try
        {
            return (currentUserContext.UserId, currentUserContext.DisplayName);
        }
        catch (InvalidOperationException)
        {
            // Geen ingelogde gebruiker beschikbaar (bv. achtergrond-/seed-scenario's) - de
            // wijziging zelf blijft gewoon doorgaan, alleen "wie" is dan onbekend.
            return (null, null);
        }
    }
}
