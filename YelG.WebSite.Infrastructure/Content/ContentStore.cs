using System.Linq.Expressions;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Enquiries;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Infrastructure.Content
{
    public sealed class ContentStore : IContentStore
    {
        private readonly WebSiteDbContext _db;

        public ContentStore(WebSiteDbContext db) => _db = db;

        public Task<T?> FindAsync<T>(long id, CancellationToken ct = default) where T : BaseEntity =>
            _db.Set<T>().FirstOrDefaultAsync(x => x.Id == id, ct);

        public Task<T?> FirstAsync<T>(CancellationToken ct = default) where T : BaseEntity =>
            _db.Set<T>().OrderBy(x => x.Id).FirstOrDefaultAsync(ct);

        public Task<List<T>> ListAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : ContentItem =>
            Ordered(_db.Set<T>().AsNoTracking(), filter).ToListAsync(ct);

        public Task<List<T>> ListForUpdateAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : ContentItem =>
            Ordered(_db.Set<T>(), filter).ToListAsync(ct);

        public Task<int> CountAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : BaseEntity =>
            filter is null ? _db.Set<T>().CountAsync(ct) : _db.Set<T>().CountAsync(filter, ct);

        public Task<bool> AnyAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : BaseEntity =>
            _db.Set<T>().AnyAsync(filter, ct);

        public Task<DateTime?> LastModifiedAsync<T>(CancellationToken ct = default) where T : BaseEntity =>
            _db.Set<T>().MaxAsync(x => (DateTime?)x.LastModifiedDate, ct);

        // by the row's own type: callers often hold it as a base type (ContentItem)
        public void Add<T>(T entity) where T : BaseEntity => _db.Add((object)entity);

        public void Remove(BaseEntity entity) => _db.Remove(entity);

        public Task SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

        public async Task<IReadOnlyList<ContentUsage>> FindUsagesAsync(Type type, long id, CancellationToken ct = default)
        {
            var entityType = _db.Model.FindEntityType(type) ?? throw new ArgumentException($"{type.Name} is not mapped.", nameof(type));
            var usages = new List<ContentUsage>();

            foreach (var fk in entityType.GetReferencingForeignKeys())
            {
                var dependent = fk.DeclaringEntityType;
                // a row's own child lists go with it; enquiries keep the service name they were sent with
                if (fk.DeleteBehavior == DeleteBehavior.Cascade || !SoftDelete.Applies(dependent.ClrType)
                    || dependent.ClrType == typeof(Enquiry)) continue;

                var table = StoreObjectIdentifier.Table(dependent.GetTableName()!, dependent.GetSchema());
                var column = fk.Properties[0].GetColumnName(table);
                var deleted = dependent.FindProperty(nameof(BaseEntity.Deleted))!.GetColumnName(table);
                // table and column names come from the model, never from input
                var sql = $"SELECT COUNT(*) AS [Value] FROM {Quote(table.Schema, table.Name)} WHERE [{column}] = @id AND [{deleted}] = 0";
                var count = await _db.Database.SqlQueryRaw<int>(sql, new SqlParameter("@id", id)).SingleAsync(ct);
                if (count > 0) usages.Add(new ContentUsage(dependent.ClrType, count));
            }
            return usages;
        }

        private static string Quote(string? schema, string name) => schema is null ? $"[{name}]" : $"[{schema}].[{name}]";

        private static IQueryable<T> Ordered<T>(IQueryable<T> query, Expression<Func<T, bool>>? filter) where T : ContentItem =>
            (filter is null ? query : query.Where(filter)).OrderBy(x => x.SortOrder).ThenBy(x => x.Id);
    }
}
