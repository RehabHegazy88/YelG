using YelG.Model.Audit;

namespace YelG.BLL.IRepository
{
    /// <param name="FromUtc">Inclusive.</param>
    /// <param name="ToUtc">Exclusive.</param>
    /// <param name="Actions">Only these action codes; null for all.</param>
    /// <param name="Search">Text inside the details or the stored user name.</param>
    public sealed record ActivityLogQuery(
        DateTimeOffset? FromUtc, DateTimeOffset? ToUtc, long? UserId, IReadOnlyList<string>? Actions, string? Search);

    public interface IActivityLogRepository
    {
        /// <summary>Queues a log row for the current user; it is written with the next SaveChanges.</summary>
        void Add(string action, string details);

        /// <summary>For events before the user is signed in (login), when the current user is not known yet.</summary>
        void Add(string action, string details, long? userId, string? userName);

        /// <summary>Matching rows, newest first, and how many match in total.</summary>
        Task<(List<ActivityLog> Rows, int Total)> SearchAsync(ActivityLogQuery query, int skip, int take, CancellationToken ct = default);

        Task<int> CountAsync(DateTimeOffset sinceUtc, IReadOnlyList<string>? actions, CancellationToken ct = default);

        /// <summary>Distinct signed-in users with any activity since the given time.</summary>
        Task<int> CountUsersAsync(DateTimeOffset sinceUtc, CancellationToken ct = default);
    }
}
