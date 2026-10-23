using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.BLL.Services.Exports;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    /// <summary>Excel downloads. Who sees which rows is decided by the export service, as on the screens.</summary>
    public class ExportController : Controller
    {
        private readonly IExcelExportService _export;

        public ExportController(IExcelExportService export)
        {
            _export = export;
        }

        public async Task<IActionResult> Results(CancellationToken ct) =>
            Download(await _export.ExportResultsAsync(ct));

        /// <param name="id">One evaluation; without it every evaluation the user may see.</param>
        public async Task<IActionResult> Scope(long? id, CancellationToken ct) =>
            Download(await _export.ExportScopeAsync(id, ct));

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> QuestionBank(long id, CancellationToken ct) =>
            Download(await _export.ExportQuestionBankAsync(id, ct));

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> TaskLibrary(CancellationToken ct) =>
            Download(await _export.ExportTaskLibraryAsync(ct));

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> ActivityLog([FromQuery] ActivityLogFilter filter, CancellationToken ct) =>
            Download(await _export.ExportActivityLogAsync(filter, ct));

        private FileContentResult Download(ExportFile file) => File(file.Content, file.ContentType, file.FileName);
    }
}
