using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.AdminPortal.Models;
using YelG.BLL.Services.QuestionBank;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    /// <summary>Admin: question bank and weights. Edits apply to the draft only; publishing makes it live.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class QuestionBankController : Controller
    {
        private readonly IQuestionBankAdminService _admin;

        public QuestionBankController(IQuestionBankAdminService admin)
        {
            _admin = admin;
        }

        /// <param name="version">Defaults to the draft when there is one, otherwise the published version.</param>
        public async Task<IActionResult> Index(int? version, CancellationToken ct) =>
            View(await LoadPageAsync(version, ct));

        public async Task<IActionResult> Weights(int? version, CancellationToken ct) =>
            View(await LoadPageAsync(version, ct));

        [HttpPost]
        public async Task<IActionResult> CreateDraft(string? returnTo, CancellationToken ct)
        {
            var draftId = await _admin.CreateDraftAsync(ct);
            TempData[Alerts.Success] = "تم تجهيز مسودة للتعديل. لن تؤثر التعديلات على التقييمات حتى تنشريها.";
            return RedirectToAction(returnTo == nameof(Weights) ? nameof(Weights) : nameof(Index), new { version = draftId });
        }

        [HttpPost]
        public async Task<IActionResult> Discard(long id, CancellationToken ct)
        {
            await _admin.DiscardDraftAsync(id, ct);
            TempData[Alerts.Success] = "تم حذف المسودة. النسخة المنشورة كما هي.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Publish(long id, string? notes, CancellationToken ct)
        {
            var result = await _admin.PublishAsync(id, notes, ct);
            var message = $"تم نشر النسخة v{result.VersionNo}. التقييمات الجديدة ستستخدمها، والتقييمات الحالية تبقى على نسختها.";
            if (result.Warnings.Count > 0) message += " ملاحظات: " + string.Join(" ", result.Warnings);
            TempData[Alerts.Success] = message;
            return RedirectToAction(nameof(Index), new { version = result.VersionId });
        }

        [HttpPost]
        public async Task<IActionResult> SaveCategory(long id, long? categoryId, [FromBody] SaveCategoryRequest request, CancellationToken ct)
        {
            await _admin.SaveCategoryAsync(id, categoryId, request, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(long id, long categoryId, CancellationToken ct)
        {
            await _admin.DeleteCategoryAsync(id, categoryId, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> MoveCategory(long id, long categoryId, int direction, CancellationToken ct)
        {
            await _admin.MoveCategoryAsync(id, categoryId, direction, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> SaveQuestion(long id, long? questionId, [FromBody] SaveQuestionRequest request, CancellationToken ct)
        {
            await _admin.SaveQuestionAsync(id, questionId, request, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteQuestion(long id, long questionId, CancellationToken ct)
        {
            await _admin.DeleteQuestionAsync(id, questionId, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> MoveQuestion(long id, long questionId, int direction, CancellationToken ct)
        {
            await _admin.MoveQuestionAsync(id, questionId, direction, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> SaveWeights(long id, long categoryId, [FromBody] SaveCategoryWeightsRequest request, CancellationToken ct)
        {
            await _admin.SaveCategoryWeightsAsync(id, categoryId, request, ct);
            return Json(new { saved = true });
        }

        private async Task<QuestionBankPageViewModel> LoadPageAsync(int? version, CancellationToken ct)
        {
            var overview = await _admin.GetOverviewAsync(ct);
            var versionId = version ?? overview.DraftId ?? overview.PublishedId
                ?? throw new YelG.BLL.Common.NotFoundException("لا توجد نسخة من بنك الأسئلة.");
            return new QuestionBankPageViewModel(overview, await _admin.GetEditorAsync(versionId, ct));
        }
    }
}
