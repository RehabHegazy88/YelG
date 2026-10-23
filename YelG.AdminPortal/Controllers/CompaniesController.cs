using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.AdminPortal.Models;
using YelG.BLL.Services.Companies;
using YelG.BLL.Services.QuestionBank;
using YelG.BLL.Services.Users;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    public class CompaniesController : Controller
    {
        private readonly ICompanyService _companies;
        private readonly IUserAdminService _users;
        private readonly IQuestionBankService _questionBank;

        public CompaniesController(ICompanyService companies, IUserAdminService users, IQuestionBankService questionBank)
        {
            _companies = companies;
            _users = users;
            _questionBank = questionBank;
        }

        [Authorize(Roles = AppRoles.Admin)]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            return View(new CompaniesViewModel(
                await _companies.ListAsync(ct),
                (await _users.ListAsync(ct)).Where(u => u.IsActive).ToList(),
                await _questionBank.GetActivitiesAsync(ct)));
        }

        /// <summary>Admin: assign to the chosen reviewers. Regular user: their own evaluation only.</summary>
        [HttpPost]
        public async Task<IActionResult> Create(CompanyForm form, CancellationToken ct)
        {
            var result = await _companies.CreateAsync(new CreateCompanyRequest(form.Name, form.ActivityId, form.ReviewerIds), ct);

            var message = User.IsAdmin()
                ? $"تمت إضافة الشركة وإسنادها إلى {result.AddedUserIds.Count} مقيم."
                : "تمت إضافة الشركة إلى تقييماتك.";
            if (result.Skipped.Count > 0) message += $" تم تخطي {result.Skipped.Count} حساب غير متاح.";
            TempData[Alerts.Success] = message;

            return User.IsAdmin()
                ? RedirectToAction(nameof(Index))
                : RedirectToAction(nameof(EvaluationsController.Index), "Evaluations");
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Edit(long id, CompanyForm form, CancellationToken ct)
        {
            await _companies.UpdateAsync(id, new UpdateCompanyRequest(form.Name, form.ActivityId), ct);
            TempData[Alerts.Success] = "تم تحديث الشركة لدى جميع المقيمين.";
            return RedirectToAction(nameof(Index));
        }

        [Authorize(Roles = AppRoles.Admin)]
        [HttpPost]
        public async Task<IActionResult> Reviewers(long id, CompanyForm form, CancellationToken ct)
        {
            var result = await _companies.SetReviewersAsync(id, form.ReviewerIds, ct);

            var parts = new List<string> { "تم حفظ المقيمين" };
            if (result.AddedUserIds.Count > 0) parts.Add($"إضافة {result.AddedUserIds.Count}");
            if (result.RemovedUserIds.Count > 0) parts.Add($"إزالة {result.RemovedUserIds.Count}");
            if (result.LockedUserIds.Count > 0) parts.Add($"تعذّرت إزالة {result.LockedUserIds.Count} لأنهم بدأوا التقييم");
            TempData[Alerts.Success] = string.Join("؛ ", parts) + ".";

            return RedirectToAction(nameof(Index));
        }
    }
}
