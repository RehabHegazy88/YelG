using Microsoft.EntityFrameworkCore;
using YelG.BLL.IRepository;
using YelG.DAL.Abstractions;
using YelG.DAL.Data;
using YelG.Model.Audit;

namespace YelG.BLL.Repository.Audit
{
    public sealed class ActivityLogRepository : IActivityLogRepository
    {
        private const int MaxDetails = 1000;

        private readonly YelgDbContext _context;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public ActivityLogRepository(YelgDbContext context, ICurrentUser currentUser, TimeProvider clock)
        {
            _context = context;
            _currentUser = currentUser;
            _clock = clock;
        }

        public void Add(string action, string details) => Add(action, details, _currentUser.UserId, _currentUser.UserName);

        public void Add(string action, string details, long? userId, string? userName)
        {
            _context.ActivityLogs.Add(new ActivityLog
            {
                OccurredAt = _clock.GetUtcNow(),
                UserId = userId,
                UserName = userName,
                Action = action,
                Details = details.Length > MaxDetails ? details[..MaxDetails] : details
            });
        }

        public async Task<(List<ActivityLog> Rows, int Total)> SearchAsync(ActivityLogQuery query, int skip, int take,
            CancellationToken ct = default)
        {
            var rows = _context.ActivityLogs.AsNoTracking();
            if (query.FromUtc is { } from) rows = rows.Where(l => l.OccurredAt >= from);
            if (query.ToUtc is { } to) rows = rows.Where(l => l.OccurredAt < to);
            if (query.UserId is { } userId) rows = rows.Where(l => l.UserId == userId);
            if (query.Actions is { } actions) rows = rows.Where(l => actions.Contains(l.Action));
            if (!string.IsNullOrEmpty(query.Search))
                rows = rows.Where(l => l.Details!.Contains(query.Search) || l.UserName!.Contains(query.Search));

            var total = await rows.CountAsync(ct);
            var page = await rows
                .OrderByDescending(l => l.OccurredAt).ThenByDescending(l => l.Id)
                .Skip(skip).Take(take)
                .ToListAsync(ct);
            return (page, total);
        }

        public Task<int> CountAsync(DateTimeOffset sinceUtc, IReadOnlyList<string>? actions, CancellationToken ct = default)
        {
            var rows = _context.ActivityLogs.Where(l => l.OccurredAt >= sinceUtc);
            if (actions is not null) rows = rows.Where(l => actions.Contains(l.Action));
            return rows.CountAsync(ct);
        }

        public Task<int> CountUsersAsync(DateTimeOffset sinceUtc, CancellationToken ct = default) =>
            _context.ActivityLogs
                .Where(l => l.OccurredAt >= sinceUtc && l.UserId != null)
                .Select(l => l.UserId)
                .Distinct()
                .CountAsync(ct);
    }
}
