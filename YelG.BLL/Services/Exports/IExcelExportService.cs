using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Exports
{
    public interface IExcelExportService
    {
        /// <summary>
        /// Summary, category results and answers of every evaluation the user may see:
        /// all of them for an admin, the user's own otherwise.
        /// </summary>
        Task<ExportFile> ExportResultsAsync(CancellationToken ct = default);

        /// <summary>
        /// Scope of work (tasks, recommendations, optional services, first-year price) of one evaluation
        /// (owner or admin), or of every evaluation the user may see when <paramref name="evaluationId"/> is null.
        /// </summary>
        Task<ExportFile> ExportScopeAsync(long? evaluationId, CancellationToken ct = default);

        /// <summary>Admin: categories with weights and activity factors, questions, options and number bands of a version.</summary>
        Task<ExportFile> ExportQuestionBankAsync(long versionId, CancellationToken ct = default);

        /// <summary>Admin: task library, answer rules and role rates.</summary>
        Task<ExportFile> ExportTaskLibraryAsync(CancellationToken ct = default);

        /// <summary>Admin: activity log rows matching the screen's filters (newest first, capped at <see cref="ExcelExportService.MaxLogRows"/>).</summary>
        Task<ExportFile> ExportActivityLogAsync(ActivityLogFilter filter, CancellationToken ct = default);
    }
}
