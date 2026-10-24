using YelG.WebSite.Domain.Audit;

namespace YelG.WebSite.Application.Admin
{
    /// <summary>Reads the audit log for the admin panel, newest first.</summary>
    public interface IAuditLogQuery
    {
        Task<AuditLogPage> PageAsync(AuditLogFilter filter, int page, int pageSize, CancellationToken ct = default);

        Task<AuditLog?> FindAsync(long id, CancellationToken ct = default);

        /// <summary>The entity types and users that appear in the log, for the filters.</summary>
        Task<(IReadOnlyList<string> EntityTypes, IReadOnlyList<string> Users)> FilterValuesAsync(CancellationToken ct = default);
    }

    public sealed record AuditLogFilter(string? EntityType = null, string? UserName = null, string? EntityId = null,
        DateOnly? From = null, DateOnly? To = null);

    public sealed record AuditLogPage(IReadOnlyList<AuditLog> Items, int Total, int Page, int PageSize)
    {
        public int PageCount => Math.Max(1, (Total + PageSize - 1) / PageSize);
    }
}
