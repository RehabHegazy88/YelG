using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Application.Admin;
using YelG.WebSite.Domain.Audit;
using YelG.WebSite.Infrastructure.Data;

namespace YelG.WebSite.Infrastructure.Content
{
    public sealed class AuditLogQuery : IAuditLogQuery
    {
        private readonly WebSiteDbContext _db;

        public AuditLogQuery(WebSiteDbContext db) => _db = db;

        public async Task<AuditLogPage> PageAsync(AuditLogFilter filter, int page, int pageSize, CancellationToken ct = default)
        {
            var query = _db.AuditLogs.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.EntityType)) query = query.Where(a => a.EntityType == filter.EntityType);
            if (!string.IsNullOrWhiteSpace(filter.UserName)) query = query.Where(a => a.UserName == filter.UserName);
            if (!string.IsNullOrWhiteSpace(filter.EntityId)) query = query.Where(a => a.EntityId == filter.EntityId);
            if (filter.From is { } from) query = query.Where(a => a.OccurredAt >= from.ToDateTime(TimeOnly.MinValue));
            if (filter.To is { } to) query = query.Where(a => a.OccurredAt < to.AddDays(1).ToDateTime(TimeOnly.MinValue));

            var total = await query.CountAsync(ct);
            page = Math.Clamp(page, 1, Math.Max(1, (total + pageSize - 1) / pageSize));
            var items = await query.OrderByDescending(a => a.OccurredAt).ThenByDescending(a => a.Id)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new AuditLogPage(items, total, page, pageSize);
        }

        public Task<AuditLog?> FindAsync(long id, CancellationToken ct = default) =>
            _db.AuditLogs.AsNoTracking().FirstOrDefaultAsync(a => a.Id == id, ct);

        public async Task<(IReadOnlyList<string> EntityTypes, IReadOnlyList<string> Users)> FilterValuesAsync(CancellationToken ct = default)
        {
            var types = await _db.AuditLogs.AsNoTracking().Select(a => a.EntityType).Distinct().OrderBy(t => t).ToListAsync(ct);
            var users = await _db.AuditLogs.AsNoTracking().Where(a => a.UserName != null).Select(a => a.UserName!).Distinct()
                .OrderBy(u => u).ToListAsync(ct);
            return (types, users);
        }
    }
}
