using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.BLL.Services.Scope;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    /// <summary>Admin: YELG task library, answer effects and role rates.</summary>
    [Authorize(Roles = AppRoles.Admin)]
    public class TaskLibraryController : Controller
    {
        private readonly ITaskLibraryAdminService _library;

        public TaskLibraryController(ITaskLibraryAdminService library)
        {
            _library = library;
        }

        public async Task<IActionResult> Index(CancellationToken ct) => View(await _library.GetLibraryAsync(ct));

        public async Task<IActionResult> Rules(CancellationToken ct) => View(await _library.GetLibraryAsync(ct));

        public async Task<IActionResult> Rates(CancellationToken ct) => View(await _library.GetLibraryAsync(ct));

        [HttpPost]
        public async Task<IActionResult> SaveTemplate(long? id, [FromBody] SaveTemplateRequest request, CancellationToken ct) =>
            Json(new { id = await _library.SaveTemplateAsync(id, request, ct) });

        [HttpPost]
        public async Task<IActionResult> DeleteTemplate(long id, CancellationToken ct)
        {
            await _library.DeleteTemplateAsync(id, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> SaveRule(long? id, [FromBody] SaveRuleRequest request, CancellationToken ct) =>
            Json(new { id = await _library.SaveRuleAsync(id, request, ct) });

        [HttpPost]
        public async Task<IActionResult> DeleteRule(long id, CancellationToken ct)
        {
            await _library.DeleteRuleAsync(id, ct);
            return Json(new { saved = true });
        }

        [HttpPost]
        public async Task<IActionResult> SaveRates(SavePricingRequestForm form, CancellationToken ct)
        {
            await _library.SavePricingAsync(new SavePricingRequest(
                new RoleHoursInput(form.Analyst, form.TeamLead, form.Manager, form.Partner),
                form.MarginPct, form.MethodologyStatus, form.MethodologyNote), ct);
            TempData[Alerts.Success] = "تم حفظ معدلات الأدوار. يظهر السعر في نطاق العمل عندما تكون لكل المهام ساعات.";
            return RedirectToAction(nameof(Rates));
        }
    }

    public sealed class SavePricingRequestForm
    {
        public decimal Analyst { get; set; }
        public decimal TeamLead { get; set; }
        public decimal Manager { get; set; }
        public decimal Partner { get; set; }
        public decimal MarginPct { get; set; }
        public YelG.Model.Enums.BasisStatus MethodologyStatus { get; set; }
        public string? MethodologyNote { get; set; }
    }
}
