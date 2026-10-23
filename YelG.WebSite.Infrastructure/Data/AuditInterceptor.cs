using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YelG.WebSite.Application.Abstractions;
using YelG.WebSite.Application.Content;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Infrastructure.Data
{
    /// <summary>
    /// On every save: stamps the base entity fields (UTC dates, the signed-in user), turns deletes of
    /// soft-deleted types into Deleted = true, and records an <see cref="AuditLog"/> row for each change made
    /// by a signed-in user. Changes without a user (the seeder) are not audited row by row. After a save that
    /// touched site content, the content version is bumped so the cached public page is rebuilt.
    /// </summary>
    public sealed class AuditInterceptor : SaveChangesInterceptor
    {
        private static readonly HashSet<string> StampFields = new()
        {
            nameof(BaseEntity.AddedDate), nameof(BaseEntity.LastModifiedDate), nameof(BaseEntity.AddedBy_PersonID),
            nameof(BaseEntity.ModifiedBy_PersonID), nameof(BaseEntity.DeleteDate)
        };

        /// <summary>Recorded as "changed" without their values.</summary>
        private static readonly HashSet<string> HiddenValues = new()
        {
            "PasswordHash", "SecurityStamp", "ConcurrencyStamp",
            nameof(Enquiry.Name), nameof(Enquiry.Email), nameof(Enquiry.Company), nameof(Enquiry.Phone),
            nameof(Enquiry.Message), nameof(Enquiry.IpHash), nameof(Enquiry.UserAgent), nameof(Enquiry.ConsentText),
            nameof(EnquiryNote.Text)
        };

        /// <summary>Arabic and symbols stay readable in the stored JSON; the admin screen HTML-encodes it when shown.</summary>
        private static readonly JsonSerializerOptions Json = new()
        {
            WriteIndented = false,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;
        private readonly IContentVersion _contentVersion;
        private List<Pending>? _pending;
        private bool _contentChanged;

        public AuditInterceptor(ICurrentUser currentUser, TimeProvider clock, IContentVersion contentVersion)
        {
            _currentUser = currentUser;
            _clock = clock;
            _contentVersion = contentVersion;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Prepare(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Prepare(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
        {
            ContentSaved();
            if (WriteAudit(eventData.Context)) eventData.Context!.SaveChanges();
            return base.SavedChanges(eventData, result);
        }

        public override async ValueTask<int> SavedChangesAsync(
            SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
        {
            ContentSaved();
            if (WriteAudit(eventData.Context)) await eventData.Context!.SaveChangesAsync(cancellationToken);
            return await base.SavedChangesAsync(eventData, result, cancellationToken);
        }

        public override void SaveChangesFailed(DbContextErrorEventData eventData)
        {
            _pending = null;
            _contentChanged = false;
            base.SaveChangesFailed(eventData);
        }

        public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
        {
            _pending = null;
            _contentChanged = false;
            return base.SaveChangesFailedAsync(eventData, cancellationToken);
        }

        private void Prepare(DbContext? context)
        {
            if (context is null) return;
            var now = _clock.GetUtcNow().UtcDateTime;
            var userId = _currentUser.UserId;
            var audit = userId is not null ? new List<Pending>() : null;

            foreach (var entry in context.ChangeTracker.Entries().ToList())
            {
                if (entry.State is EntityState.Unchanged or EntityState.Detached || entry.Entity is AuditLog) continue;
                if (entry.Entity is BaseEntity and not (Enquiry or EnquiryNote)) _contentChanged = true;
                var action = entry.State switch
                {
                    EntityState.Added => "Created",
                    EntityState.Deleted => "Deleted",
                    _ => "Updated"
                };

                switch (entry.Entity)
                {
                    case BaseEntity entity when entry.State == EntityState.Deleted && SoftDelete.Applies(entity.GetType()):
                        entry.State = EntityState.Modified;
                        entity.Deleted = true;
                        entity.DeleteDate = now;
                        entity.LastModifiedDate = now;
                        entity.ModifiedBy_PersonID = userId;
                        break;
                    case BaseEntity entity when entry.State == EntityState.Added:
                        entity.AddedDate = entity.LastModifiedDate = now;
                        entity.AddedBy_PersonID ??= userId;
                        break;
                    case BaseEntity entity when entry.State == EntityState.Modified:
                        entity.LastModifiedDate = now;
                        entity.ModifiedBy_PersonID = userId;
                        action = ActionOf(entry);
                        break;
                    case AdminUser user when entry.State == EntityState.Added:
                        user.AddedDate = user.LastModifiedDate = now;
                        break;
                    case AdminUser user when entry.State == EntityState.Modified:
                        user.LastModifiedDate = now;
                        break;
                }

                if (audit is not null && Audited(entry))
                    audit.Add(new Pending(entry, action, Changes(entry, action)));
            }

            _pending = audit is { Count: > 0 } ? audit : null;
        }

        private void ContentSaved()
        {
            if (!_contentChanged) return;
            _contentChanged = false;
            _contentVersion.Bump();
        }

        /// <summary>Adds the audit rows once the keys of new rows are known. True when there is something to save.</summary>
        private bool WriteAudit(DbContext? context)
        {
            if (context is null || _pending is null) return false;
            var pending = _pending;
            _pending = null;
            var now = _clock.GetUtcNow().UtcDateTime;

            foreach (var p in pending)
            {
                var key = p.Entry.Metadata.FindPrimaryKey();
                var id = key is null ? null : string.Join(",", key.Properties.Select(k => p.Entry.Property(k.Name).CurrentValue));
                context.Set<AuditLog>().Add(new AuditLog
                {
                    OccurredAt = now,
                    UserId = _currentUser.UserId,
                    UserName = _currentUser.UserName,
                    Action = p.Action,
                    EntityType = p.Entry.Metadata.ClrType.Name,
                    EntityId = id is { Length: > 40 } ? id[..40] : id,
                    Changes = p.Changes
                });
            }
            return true;
        }

        private static bool Audited(EntityEntry entry) =>
            entry.Entity is BaseEntity or AdminUser or AdminRole
            || entry.Metadata.ClrType == typeof(Microsoft.AspNetCore.Identity.IdentityUserRole<long>);

        /// <summary>Publish / hide and reorder are named, so the audit log reads like what the editor did.</summary>
        private static string ActionOf(EntityEntry entry)
        {
            var changed = entry.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)
                                                      && !StampFields.Contains(p.Metadata.Name)).Select(p => p.Metadata.Name).ToList();
            var anyText = entry.ComplexProperties.Any(c => c.Properties.Any(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)));
            if (anyText) return "Updated";
            if (changed.SequenceEqual(new[] { nameof(ContentItem.IsPublished) }) || changed.SequenceEqual(new[] { nameof(SectionBase.IsPublished) }))
                return Equals(entry.Property(changed[0]).CurrentValue, true) ? "Published" : "Hidden";
            if (changed.SequenceEqual(new[] { nameof(ContentItem.SortOrder) })) return "Reordered";
            if (changed.Contains(nameof(BaseEntity.Deleted)) && Equals(entry.Property(nameof(BaseEntity.Deleted)).CurrentValue, true)) return "Deleted";
            return "Updated";
        }

        private static string? Changes(EntityEntry entry, string action)
        {
            var changes = new Dictionary<string, object?>();
            void Add(string name, object? oldValue, object? newValue)
            {
                if (StampFields.Contains(name)) return;
                var hidden = HiddenValues.Contains(name);
                changes[name] = action == "Created"
                    ? (hidden ? "(set)" : newValue)
                    : new { old = hidden ? "(hidden)" : oldValue, @new = hidden ? "(changed)" : newValue };
            }

            if (action is "Created")
            {
                foreach (var p in entry.Properties.Where(p => !p.Metadata.IsPrimaryKey() && p.CurrentValue is not null))
                    Add(p.Metadata.Name, null, p.CurrentValue);
                foreach (var c in entry.ComplexProperties)
                    foreach (var p in c.Properties.Where(p => !string.IsNullOrEmpty(p.CurrentValue as string)))
                        Add($"{c.Metadata.Name}.{p.Metadata.Name}", null, p.CurrentValue);
            }
            else if (action is "Deleted" && entry.State == EntityState.Deleted)
            {
                return null;   // hard delete (enquiries): no personal data copied into the log
            }
            else
            {
                foreach (var p in entry.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)))
                    Add(p.Metadata.Name, p.OriginalValue, p.CurrentValue);
                foreach (var c in entry.ComplexProperties)
                    foreach (var p in c.Properties.Where(p => p.IsModified && !Equals(p.OriginalValue, p.CurrentValue)))
                        Add($"{c.Metadata.Name}.{p.Metadata.Name}", p.OriginalValue, p.CurrentValue);
            }

            return changes.Count == 0 ? null : JsonSerializer.Serialize(changes, Json);
        }

        private sealed record Pending(EntityEntry Entry, string Action, string? Changes);
    }
}
