using System.Linq.Expressions;
using YelG.WebSite.Domain.Common;

namespace YelG.WebSite.Application.Admin
{
    /// <summary>
    /// Data access for the admin panel's content screens (Infrastructure, over the website database).
    /// Reads for lists are not tracked; <see cref="FindAsync{T}"/> and <see cref="FirstAsync{T}"/> return
    /// tracked rows that <see cref="SaveChangesAsync"/> writes back. Deleted rows are never returned.
    /// </summary>
    public interface IContentStore
    {
        /// <summary>A row to edit (tracked), or null.</summary>
        Task<T?> FindAsync<T>(long id, CancellationToken ct = default) where T : BaseEntity;

        /// <summary>The row of a one-row table (a section, the site settings) to edit (tracked), or null.</summary>
        Task<T?> FirstAsync<T>(CancellationToken ct = default) where T : BaseEntity;

        /// <summary>Rows in display order (SortOrder, then Id), not tracked.</summary>
        Task<List<T>> ListAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : ContentItem;

        /// <summary>Rows to change together, e.g. to renumber a list (tracked), in display order.</summary>
        Task<List<T>> ListForUpdateAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : ContentItem;

        Task<int> CountAsync<T>(Expression<Func<T, bool>>? filter = null, CancellationToken ct = default) where T : BaseEntity;

        Task<bool> AnyAsync<T>(Expression<Func<T, bool>> filter, CancellationToken ct = default) where T : BaseEntity;

        /// <summary>The latest change to any row of the table, for the dashboard; null when it is empty.</summary>
        Task<DateTime?> LastModifiedAsync<T>(CancellationToken ct = default) where T : BaseEntity;

        void Add<T>(T entity) where T : BaseEntity;

        /// <summary>Soft delete (the save converts it), or a real delete for enquiries.</summary>
        void Remove(BaseEntity entity);

        Task SaveChangesAsync(CancellationToken ct = default);

        /// <summary>
        /// Rows that still point to a row (by a foreign key), per table, ignoring deleted rows and the row's own
        /// child lists. A row in use is not deleted: the editor sees where it is used instead.
        /// </summary>
        Task<IReadOnlyList<ContentUsage>> FindUsagesAsync(Type type, long id, CancellationToken ct = default);
    }

    /// <summary><paramref name="Count"/> rows of <paramref name="EntityType"/> point to the row.</summary>
    public sealed record ContentUsage(Type EntityType, int Count);
}
