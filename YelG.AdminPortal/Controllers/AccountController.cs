using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using YelG.AdminPortal.Models;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.Services.Users;
using YelG.AdminPortal.Infrastructure;
using YelG.Model.Auth;

namespace YelG.AdminPortal.Controllers
{
    public class AccountController : Controller
    {
        private const string InvalidLogin = "اسم المستخدم أو كلمة المرور غير صحيحة.";

        private readonly SignInManager<User> _signIn;
        private readonly UserManager<User> _users;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly LoginThrottle _throttle;
        private readonly IAccountService _account;

        public AccountController(
            SignInManager<User> signIn,
            UserManager<User> users,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            LoginThrottle throttle,
            IAccountService account)
        {
            _signIn = signIn;
            _users = users;
            _log = log;
            _unitOfWork = unitOfWork;
            _throttle = throttle;
            _account = account;
        }

        [AllowAnonymous]
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            if (User.Identity?.IsAuthenticated == true) return LocalRedirect(SafeReturnUrl(returnUrl));
            return View(new LoginViewModel { ReturnUrl = returnUrl });
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userName = model.UserName.Trim();
            var address = HttpContext.Connection.RemoteIpAddress?.ToString();

            // Checked before the password, so a blocked name or address learns nothing from more guesses.
            if (_throttle.RetryAfter(address, userName) is { } wait)
            {
                Response.StatusCode = StatusCodes.Status429TooManyRequests;
                Response.Headers.RetryAfter = ((int)Math.Ceiling(wait.TotalSeconds)).ToString();
                ModelState.AddModelError(string.Empty,
                    $"محاولات دخول خاطئة كثيرة. حاولي مرة أخرى بعد {Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes))} دقيقة.");
                return View(model);
            }

            var user = await _users.FindByNameAsync(userName);
            if (user is null || !user.IsActive)
            {
                await Task.Delay(350); // same small delay as the Apps Script login, so misses are not faster
                await FailAsync(user is null ? "اسم مستخدم غير موجود" : "حساب موقوف", user?.Id, userName, address);
                ModelState.AddModelError(string.Empty, InvalidLogin);
                return View(model);
            }

            var result = await _signIn.PasswordSignInAsync(user, model.Password, isPersistent: false, lockoutOnFailure: true);
            if (result.IsLockedOut)
            {
                _throttle.RecordFailure(address, userName);
                await LogLoginAsync("LOGIN_LOCKED", "الدخول موقوف مؤقتًا بعد محاولات خاطئة", user.Id, user.UserName);
                ModelState.AddModelError(string.Empty, "تم إيقاف الدخول مؤقتًا بعد عدة محاولات خاطئة. حاولي بعد 15 دقيقة.");
                return View(model);
            }
            if (!result.Succeeded)
            {
                await FailAsync("كلمة مرور خاطئة", user.Id, user.UserName, address);
                ModelState.AddModelError(string.Empty, InvalidLogin);
                return View(model);
            }

            _throttle.Reset(userName);
            await LogLoginAsync("LOGIN", "دخول ناجح", user.Id, user.UserName);
            return LocalRedirect(SafeReturnUrl(model.ReturnUrl));
        }

        [HttpPost]
        public async Task<IActionResult> Logout()
        {
            _log.Add("LOGOUT", "خروج");
            await _unitOfWork.SaveChangesAsync();
            await _signIn.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        [AllowAnonymous]
        public IActionResult AccessDenied() => View();

        [HttpGet]
        public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

        [HttpPost]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model, CancellationToken ct)
        {
            if (!ModelState.IsValid) return View(model);
            try
            {
                await _account.ChangePasswordAsync(model.CurrentPassword, model.NewPassword,
                    HttpContext.Connection.RemoteIpAddress?.ToString(), ct);
            }
            catch (BusinessRuleException ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(new ChangePasswordViewModel());   // never echo passwords back into the form
            }

            // The security stamp changed: re-issue this session's cookie so only the other sessions end.
            if (await _users.GetUserAsync(User) is { } user) await _signIn.RefreshSignInAsync(user);
            TempData[Alerts.Success] = "تم تغيير كلمة المرور. الجلسات المفتوحة على أجهزة أخرى ستنتهي خلال دقيقة.";
            return RedirectToAction(nameof(ChangePassword));
        }

        /// <summary>Logs a failed attempt, and the block once when this failure reaches a limit.</summary>
        private async Task FailAsync(string reason, long? userId, string? userName, string? address)
        {
            // The failure first, so the block shows above it in the newest-first log.
            _log.Add("LOGIN_FAILED", reason, userId, Truncate(userName));
            if (_throttle.RecordFailure(address, userName ?? string.Empty))
                _log.Add("LOGIN_THROTTLED", $"إيقاف محاولات الدخول مؤقتًا بعد تكرار الخطأ؛ العنوان: {address ?? "غير معروف"}",
                    userId, Truncate(userName));
            await _unitOfWork.SaveChangesAsync();
        }

        private static string? Truncate(string? userName) => userName is { Length: > 256 } ? userName[..256] : userName;

        private async Task LogLoginAsync(string action, string details, long? userId, string? userName)
        {
            _log.Add(action, details, userId, Truncate(userName));
            await _unitOfWork.SaveChangesAsync();
        }

        private string SafeReturnUrl(string? returnUrl) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : Url.Action("Index", "Home")!;
    }
}
