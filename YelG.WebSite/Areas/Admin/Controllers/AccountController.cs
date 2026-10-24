using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YelG.WebSite.Infrastructure.Identity;

namespace YelG.WebSite.Areas.Admin.Controllers
{
    [Route("admin")]
    public sealed class AccountController : AdminController
    {
        private readonly SignInManager<AdminUser> _signIn;
        private readonly UserManager<AdminUser> _users;
        private readonly ILogger<AccountController> _logger;

        public AccountController(SignInManager<AdminUser> signIn, UserManager<AdminUser> users, ILogger<AccountController> logger)
        {
            _signIn = signIn;
            _users = users;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("login")]
        public IActionResult Login(string? returnUrl)
        {
            if (User.Identity?.IsAuthenticated == true) return Redirect(SafeReturn(returnUrl, "/admin"));
            return View(new LoginInput { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginInput input)
        {
            if (!ModelState.IsValid) return View(input);

            // one message for every failure, so the form does not reveal which accounts exist
            const string failed = "البريد الإلكتروني أو كلمة المرور غير صحيحة.";
            var user = await _users.FindByEmailAsync(input.Email.Trim());
            if (user is null)
            {
                ModelState.AddModelError("", failed);
                return View(input);
            }

            var result = await _signIn.PasswordSignInAsync(user, input.Password, input.RememberMe, lockoutOnFailure: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin user {UserId} signed in", user.Id);
                return Redirect(SafeReturn(input.ReturnUrl, "/admin"));
            }

            if (result.IsLockedOut)
            {
                _logger.LogWarning("Admin user {UserId} is locked out", user.Id);
                ModelState.AddModelError("", "تم إيقاف الدخول مؤقتًا بعد عدة محاولات خاطئة. حاول بعد ١٥ دقيقة.");
            }
            else
            {
                ModelState.AddModelError("", failed);
            }
            return View(input);
        }

        [AllowTemporaryPassword]
        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            await _signIn.SignOutAsync();
            return Redirect("/admin/login");
        }

        [AllowTemporaryPassword]
        [HttpGet("account/password")]
        public IActionResult ChangePassword() => View(new ChangePasswordInput());

        [AllowTemporaryPassword]
        [HttpPost("account/password")]
        public async Task<IActionResult> ChangePassword(ChangePasswordInput input)
        {
            if (!ModelState.IsValid) return View(input);
            var user = await _users.GetUserAsync(User);
            if (user is null) return Redirect("/admin/login");

            var result = await _users.ChangePasswordAsync(user, input.CurrentPassword, input.NewPassword);
            if (!result.Succeeded)
            {
                foreach (var error in result.Errors) ModelState.AddModelError("", IdentityMessages.Arabic(error));
                return View(input);
            }

            if (user.MustChangePassword)
            {
                user.MustChangePassword = false;
                await _users.UpdateAsync(user);
            }
            await _signIn.RefreshSignInAsync(user);
            Flash("تم تغيير كلمة المرور.");
            return Redirect("/admin");
        }

        [AllowAnonymous]
        [HttpGet("denied")]
        public IActionResult Denied() => View();
    }

    public sealed class LoginInput
    {
        [Required(ErrorMessage = "أدخل البريد الإلكتروني.")]
        [EmailAddress(ErrorMessage = "أدخل بريدًا إلكترونيًا صحيحًا.")]
        [Display(Name = "البريد الإلكتروني")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "أدخل كلمة المرور.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "تذكّرني على هذا الجهاز")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }

    public sealed class ChangePasswordInput
    {
        [Required(ErrorMessage = "أدخل كلمة المرور الحالية.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الحالية")]
        public string CurrentPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "أدخل كلمة المرور الجديدة.")]
        [StringLength(128, MinimumLength = 10, ErrorMessage = "كلمة المرور من ١٠ أحرف على الأقل.")]
        [DataType(DataType.Password)]
        [Display(Name = "كلمة المرور الجديدة")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "أعد كتابة كلمة المرور الجديدة.")]
        [Compare(nameof(NewPassword), ErrorMessage = "كلمتا المرور غير متطابقتين.")]
        [DataType(DataType.Password)]
        [Display(Name = "تأكيد كلمة المرور الجديدة")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>Identity's error codes in the panel's language.</summary>
    public static class IdentityMessages
    {
        public static string Arabic(IdentityError error) => error.Code switch
        {
            nameof(IdentityErrorDescriber.PasswordMismatch) => "كلمة المرور الحالية غير صحيحة.",
            nameof(IdentityErrorDescriber.PasswordTooShort) => "كلمة المرور من ١٠ أحرف على الأقل.",
            nameof(IdentityErrorDescriber.PasswordRequiresDigit) => "يجب أن تحتوي كلمة المرور على رقم.",
            nameof(IdentityErrorDescriber.PasswordRequiresLower) => "يجب أن تحتوي كلمة المرور على حرف إنجليزي صغير.",
            nameof(IdentityErrorDescriber.PasswordRequiresUniqueChars) => "استخدم أحرفًا أكثر تنوعًا في كلمة المرور.",
            nameof(IdentityErrorDescriber.DuplicateEmail) or nameof(IdentityErrorDescriber.DuplicateUserName) => "هذا البريد مستخدم لحساب آخر.",
            nameof(IdentityErrorDescriber.InvalidEmail) or nameof(IdentityErrorDescriber.InvalidUserName) => "البريد الإلكتروني غير صالح.",
            _ => error.Description
        };
    }
}
