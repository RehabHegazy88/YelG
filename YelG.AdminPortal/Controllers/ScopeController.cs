using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.AdminPortal.Models;
using YelG.BLL.Common;
using YelG.BLL.Services.Evaluations;
using YelG.BLL.Services.Scope;
using YelG.BLL.ViewModel;
using YelG.Model.Enums;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    public class ScopeController : Controller
    {
        private readonly IScopeService _scope;
        private readonly IEvaluationService _evaluations;

        public ScopeController(IScopeService scope, IEvaluationService evaluations)
        {
            _scope = scope;
            _evaluations = evaluations;
        }

        public async Task<IActionResult> Index(long id, CancellationToken ct)
        {
            var evaluation = await _evaluations.GetAsync(id, ct);
            var scope = await _scope.GenerateAsync(id, ct) ?? throw new NotFoundException("التقييم المطلوب غير موجود.");

            // Mirrors ScopeService.UpdateItemAsync so the page only offers what the server will accept.
            var canEdit = User.IsAdmin()
                && (evaluation.ReviewerId == User.UserId()
                    || evaluation.Status is EvaluationStatus.Submitted or EvaluationStatus.Approved);

            return View(new ScopePageViewModel(evaluation, scope, canEdit, await _scope.PriceAsync(scope, ct)));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> UpdateItem(long id, [FromBody] UpdateScopeItemRequest request, CancellationToken ct)
        {
            await _scope.UpdateItemAsync(id, request, ct);
            return Json(new { saved = true });
        }
    }
}
