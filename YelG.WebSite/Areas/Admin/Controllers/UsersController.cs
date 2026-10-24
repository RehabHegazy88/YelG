using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using YelG.WebSite.Application.Common;
using YelG.WebSite.Areas.Admin.Models;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    /// <summary>Admin and Editor accounts (Admins only). There is always at least one active Admin.</summary>
    [Route("admin/users")]
    [Authorize(Policy = AdminPolicies.AdminOnly)]
    public sealed class UsersController : AdminController
    {
        private readonly UserManager<AdminUser> _users;

        public UsersController(UserManager<AdminUser> users) => _users = users;

        private long CurrentUserId => long.Parse(_users.GetUserId(User)!);

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var users = await _users.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(ct);
            var rows = new List<(AdminUser, string)>();
            foreach (var user in users) rows.Add((user, await RoleOf(user)));
            return View(new UserListModel { Users = rows, CurrentUserId = CurrentUserId });
        }

        [HttpGet("new")]
        public IActionResult New() => View("Edit", new UserInput());

        [HttpPost("new")]
        public async Task<IActionResult> Create(UserInput input)
        {
            if (string.IsNullOrEmpty(input.Password)) ModelState.AddModelError(nameof(input.Password), "أدخل كلمة مرور مؤقتة.");
            if (!AdminRoles.All.Contains(input.Role)) ModelState.AddModelError(nameof(input.Role), "اختر دورًا.");
            if (!ModelState.IsValid) return View("Edit", input);

            var email = input.Email.Trim();
            var user = new AdminUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = input.DisplayName.Trim(),
                IsActive = input.IsActive,
                MustChangePassword = true,
                LockoutEnabled = true
            };
            var result = await _users.CreateAsync(user, input.Password!);
            if (result.Succeeded) result = await _users.AddToRoleAsync(user, input.Role);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", IdentityMessages.Arabic(error));
                return View("Edit", input);
            }

            Flash($"تمت إضافة حساب {user.DisplayName}. سلّم كلمة المرور المؤقتة لصاحب الحساب بطريقة آمنة؛ سيُطلب تغييرها عند أول دخول.");
            return Redirect("/admin/users");
        }

        [HttpGet("{id:long}")]
        public async Task<IActionResult> Edit(long id)
        {
            if (await _users.FindByIdAsync(id.ToString()) is not { } user) return NotFound();
            return View(new UserInput
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Email = user.Email ?? "",
                Role = await RoleOf(user),
                IsActive = user.IsActive,
                IsLockedOut = await _users.IsLockedOutAsync(user),
                IsSelf = user.Id == CurrentUserId
            });
        }

        [HttpPost("{id:long}")]
        public async Task<IActionResult> Update(long id, UserInput input)
        {
            if (await _users.FindByIdAsync(id.ToString()) is not { } user) return NotFound();
            ModelState.Remove(nameof(input.Password));
            input.Id = id;
            input.IsSelf = id == CurrentUserId;
            input.IsLockedOut = await _users.IsLockedOutAsync(user);
            if (!AdminRoles.All.Contains(input.Role)) ModelState.AddModelError(nameof(input.Role), "اختر دورًا.");

            var currentRole = await RoleOf(user);
            var losesAdmin = currentRole == AdminRoles.Admin && user.IsActive && (input.Role != AdminRoles.Admin || !input.IsActive);
            if (input.IsSelf && losesAdmin)
                ModelState.AddModelError("", "لا يمكنك إلغاء صلاحية المدير أو إيقاف حسابك أنت.");
            else if (losesAdmin && await ActiveAdminCount() <= 1)
                ModelState.AddModelError("", "يجب أن يبقى مدير واحد مفعّل على الأقل.");
            if (!ModelState.IsValid) return View("Edit", input);

            var email = input.Email.Trim();
            var deactivated = user.IsActive && !input.IsActive;
            user.DisplayName = input.DisplayName.Trim();
            user.IsActive = input.IsActive;
            var result = await _users.SetEmailAsync(user, email);
            if (result.Succeeded) result = await _users.SetUserNameAsync(user, email);
            if (result.Succeeded && currentRole != input.Role)
            {
                if (currentRole.Length > 0) result = await _users.RemoveFromRoleAsync(user, currentRole);
                if (result.Succeeded) result = await _users.AddToRoleAsync(user, input.Role);
            }
            if (result.Succeeded) result = await _users.UpdateAsync(user);
            // a new stamp ends the account's open sessions at their next check (deactivated, or a role change)
            if (result.Succeeded && (deactivated || currentRole != input.Role)) result = await _users.UpdateSecurityStampAsync(user);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", IdentityMessages.Arabic(error));
                return View("Edit", input);
            }

            Flash("تم حفظ الحساب.");
            return Redirect("/admin/users");
        }

        [HttpPost("{id:long}/password")]
        public async Task<IActionResult> ResetPassword(long id, ResetPasswordInput input)
        {
            if (await _users.FindByIdAsync(id.ToString()) is not { } user) return NotFound();
            if (!ModelState.IsValid)
            {
                FlashError(string.Join(" ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)));
                return Redirect($"/admin/users/{id}");
            }

            var token = await _users.GeneratePasswordResetTokenAsync(user);
            var result = await _users.ResetPasswordAsync(user, token, input.Password);
            if (result.Succeeded)
            {
                user.MustChangePassword = true;
                result = await _users.UpdateAsync(user);
            }
            if (result.Succeeded) await _users.SetLockoutEndDateAsync(user, null);

            if (result.Succeeded) Flash("تم تعيين كلمة مرور مؤقتة؛ سيُطلب تغييرها عند الدخول التالي.");
            else FlashError(string.Join(" ", result.Errors.Select(IdentityMessages.Arabic)));
            return Redirect($"/admin/users/{id}");
        }

        [HttpPost("{id:long}/unlock")]
        public async Task<IActionResult> Unlock(long id)
        {
            if (await _users.FindByIdAsync(id.ToString()) is not { } user) return NotFound();
            await _users.SetLockoutEndDateAsync(user, null);
            await _users.ResetAccessFailedCountAsync(user);
            Flash("تم إلغاء إيقاف الدخول.");
            return Redirect($"/admin/users/{id}");
        }

        private async Task<string> RoleOf(AdminUser user) =>
            (await _users.GetRolesAsync(user)).FirstOrDefault(AdminRoles.All.Contains) ?? "";

        private async Task<int> ActiveAdminCount() =>
            (await _users.GetUsersInRoleAsync(AdminRoles.Admin)).Count(u => u.IsActive);
    }
}
