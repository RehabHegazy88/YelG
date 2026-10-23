using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Audit;

namespace YelG.BLL.Services.Audit
{
    public sealed class ActivityLogService : IActivityLogService
    {
        public const int PageSize = 50;
        private const int MaxSearch = 100;

        private readonly IActivityLogRepository _log;
        private readonly IUserRepository _users;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public ActivityLogService(IActivityLogRepository log, IUserRepository users, ICurrentUser currentUser, TimeProvider clock)
        {
            _log = log;
            _users = users;
            _currentUser = currentUser;
            _clock = clock;
        }

        public async Task<ActivityLogPage> SearchAsync(ActivityLogFilter filter, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var users = await UsersAsync(ct);

            var query = ToQuery(filter);
            var page = Math.Max(1, filter.Page);
            var (rows, total) = await _log.SearchAsync(query, (page - 1) * PageSize, PageSize, ct);
            if (rows.Count == 0 && total > 0)
            {
                // Page past the end (filters changed): show the last page instead of an empty table.
                page = (total + PageSize - 1) / PageSize;
                (rows, total) = await _log.SearchAsync(query, (page - 1) * PageSize, PageSize, ct);
            }

            var now = _clock.GetUtcNow();
            var today = Labels.CairoStartOfDay(Labels.CairoToday(now));
            var stats = new ActivityLogStats(
                await _log.CountAsync(today, null, ct),
                await _log.CountUsersAsync(now.AddDays(-7), ct),
                await _log.CountAsync(now.AddDays(-7), new[] { "LOGIN_FAILED", "LOGIN_LOCKED" }, ct),
                await _log.CountAsync(now.AddDays(-30), ActivityActions.CodesOf(ActivityActions.Exports), ct));

            return new ActivityLogPage(ToRows(rows, users), total, page, PageSize, stats, users);
        }

        /// <summary>Turns the screen's filter (Cairo days, group name) into a repository query.</summary>
        internal static ActivityLogQuery ToQuery(ActivityLogFilter filter)
        {
            var (from, to) = (filter.From, filter.To);
            if (from is { } f && to is { } t && f > t) (from, to) = (t, f);

            var search = filter.Search?.Trim();
            if (search?.Length > MaxSearch) search = search[..MaxSearch];

            return new ActivityLogQuery(
                from is { } start ? Labels.CairoStartOfDay(start) : null,
                to is { } end ? Labels.CairoStartOfDay(end.AddDays(1)) : null,
                filter.UserId,
                ActivityActions.Groups.Contains(filter.Group) ? ActivityActions.CodesOf(filter.Group!) : null,
                string.IsNullOrEmpty(search) ? null : search);
        }

        internal static List<ActivityLogRow> ToRows(IEnumerable<ActivityLog> rows, IReadOnlyList<UserChoice> users)
        {
            var names = users.ToDictionary(u => u.Id, u => u.DisplayName);
            return rows.Select(l =>
            {
                var action = ActivityActions.Of(l.Action);
                var name = l.UserId is { } id && names.TryGetValue(id, out var display) ? display
                    : !string.IsNullOrEmpty(l.UserName) ? l.UserName
                    : "—";
                return new ActivityLogRow(l.Id, l.OccurredAt, l.UserId, name, l.Action, action.Label, action.Group, l.Details);
            }).ToList();
        }

        internal async Task<List<UserChoice>> UsersAsync(CancellationToken ct) =>
            (await _users.ListWithRolesAsync(ct)).Select(u => new UserChoice(u.User.Id, u.User.DisplayName)).ToList();
    }
}
