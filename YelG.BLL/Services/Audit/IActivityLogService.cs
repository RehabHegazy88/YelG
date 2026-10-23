using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Audit
{
    public interface IActivityLogService
    {
        /// <summary>Admin: one page of matching log rows, newest first, with the summary counts and user list.</summary>
        Task<ActivityLogPage> SearchAsync(ActivityLogFilter filter, CancellationToken ct = default);
    }
}
