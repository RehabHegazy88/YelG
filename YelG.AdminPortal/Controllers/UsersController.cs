using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Infrastructure;
using YelG.AdminPortal.Models;
using YelG.BLL.Services.Users;
using YelG.BLL.ViewModel;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    [Authorize(Roles = AppRoles.Admin)]
    public class UsersController : Controller
    {
        private readonly IUserAdminService _users;

        public UsersController(IUserAdminService users)
        {
            _users = users;
        }

        public async Task<IActionResult> Index(CancellationToken ct) => View(await _users.ListAsync(ct));

        [HttpPost]
        public async Task<IActionResult> Create(UserForm form, CancellationToken ct)
        {
            await _users.CreateAsync(ToRequest(form), ct);
            TempData[Alerts.Success] = $"تمت إضافة المستخدم {form.UserName}.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Edit(long id, UserForm form, CancellationToken ct)
        {
            await _users.UpdateAsync(id, ToRequest(form), ct);
            TempData[Alerts.Success] = "تم حفظ المستخدم.";
            return RedirectToAction(nameof(Index));
        }

        private static SaveUserRequest ToRequest(UserForm form) =>
            new(form.UserName, form.DisplayName, form.IsAdmin, form.IsActive, form.Password);
    }
}
