using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using YelG.DAL.Abstractions;
using YelG.DAL.Data;
using YelG.Model.Auth;
using YelG.Model.Basic;

namespace YelG.DAL.Interceptors
{
    /// <summary>
    /// Stamps the Basic base entities on every save: AddedDate / LastModifiedDate (UTC), and for
    /// MainBaseEntity also AddedBy_PersonID / ModifiedBy_PersonID (the signed-in user, or the built-in
    /// system user when nobody is signed in, e.g. seeding). Sets DeleteDate when a row is soft-deleted.
    /// </summary>
    public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
    {
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public AuditableEntityInterceptor(ICurrentUser currentUser, TimeProvider clock)
        {
            _currentUser = currentUser;
            _clock = clock;
        }

        public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
        {
            Stamp(eventData.Context);
            return base.SavingChanges(eventData, result);
        }

        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            Stamp(eventData.Context);
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }

        private void Stamp(DbContext? context)
        {
            if (context is null) return;
            var now = _clock.GetUtcNow().UtcDateTime;
            var userId = _currentUser.UserId ?? BuiltInUsers.SystemId;

            foreach (var entry in context.ChangeTracker.Entries())
            {
                if (entry.State == EntityState.Deleted && SoftDelete.Applies(entry.Metadata.ClrType))
                    throw new InvalidOperationException(
                        $"{entry.Metadata.ClrType.Name} is soft-deleted: use SoftDelete.MarkDeleted instead of Remove.");

                if (entry.State is not (EntityState.Added or EntityState.Modified)) continue;
                var added = entry.State == EntityState.Added;

                switch (entry.Entity)
                {
                    case MainBaseEntity<User> main:
                        StampDates(main, added, now);
                        if (added && main.AddedBy_PersonID == 0) main.AddedBy_PersonID = userId;
                        if (!added) main.ModifiedBy_PersonID = userId;
                        break;
                    case BaseEntity<long> entity:
                        StampDates(entity, added, now);
                        break;
                    case IBaseEntity other:   // User
                        if (added) other.AddedDate = now;
                        other.LastModifiedDate = now;
                        break;
                }
            }
        }

        private static void StampDates(BaseEntity<long> entity, bool added, DateTime now)
        {
            if (added) entity.AddedDate = now;
            entity.LastModifiedDate = now;
            if (entity.Deleted && entity.DeleteDate is null) entity.DeleteDate = now;
        }
    }
}
