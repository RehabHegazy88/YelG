using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.AdminPortal.Models;
using YelG.BLL.Common;
using YelG.BLL.Services.Evaluations;
using YelG.BLL.Services.QuestionBank;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    public class EvaluationsController : Controller
    {
        private readonly IEvaluationService _evaluations;
        private readonly IQuestionBankService _questionBank;

        public EvaluationsController(IEvaluationService evaluations, IQuestionBankService questionBank)
        {
            _evaluations = evaluations;
            _questionBank = questionBank;
        }

        /// <summary>The signed-in user's evaluations.</summary>
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            return View(new MyEvaluationsViewModel(await _evaluations.ListMineAsync(ct), await _questionBank.GetActivitiesAsync(ct)));
        }

        /// <summary>Admin: results and approval for every reviewer.</summary>
        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> All(CancellationToken ct)
        {
            return View(await _evaluations.ListAllAsync(ct));
        }

        public async Task<IActionResult> Details(long id, CancellationToken ct)
        {
            var evaluation = await _evaluations.GetAsync(id, ct);
            var form = await _questionBank.GetFormAsync(evaluation.QuestionBankVersionId, ct);
            var initial = await _evaluations.PreviewAsync(id, evaluation.Answers, ct);
            return View(new EvaluationPageViewModel(evaluation, form, initial));
        }

        [HttpPost]
        public async Task<IActionResult> Preview(long id, [FromBody] EvaluationInput input, CancellationToken ct)
        {
            return Json(await _evaluations.PreviewAsync(id, input.AnswersMap(), ct));
        }

        [HttpPost]
        public async Task<IActionResult> Save(long id, [FromBody] EvaluationInput input, CancellationToken ct)
        {
            var saved = await _evaluations.SaveAsync(id, new SaveEvaluationRequest(
                input.AnswersMap(), input.ProfessionalJudgment, input.Note, input.RowVersion), ct);
            return Json(Summary(saved));
        }

        [HttpPost]
        public async Task<IActionResult> Submit(long id, [FromBody] RowVersionInput input, CancellationToken ct) =>
            Json(Summary(await _evaluations.SubmitAsync(id, input.RowVersion, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Approve(long id, [FromBody] RowVersionInput input, CancellationToken ct) =>
            Json(Summary(await _evaluations.ApproveAsync(id, input.RowVersion, ct)));

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Return(long id, [FromBody] RowVersionInput input, CancellationToken ct) =>
            Json(Summary(await _evaluations.ReturnAsync(id, input.RowVersion, ct)));

        [HttpPost]
        public async Task<IActionResult> Delete(long id, CancellationToken ct)
        {
            await _evaluations.DeleteAsync(id, ct);
            TempData[Alerts.Success] = "تم حذف التقييم.";
            return RedirectToAction(nameof(Index));
        }

        private static object Summary(EvaluationDetails details) => new
        {
            details.Status,
            statusLabel = Labels.Of(details.Status),
            details.RowVersion,
            details.Permissions,
            score = ScoreSummary.From(details.Score)
        };
    }
}
