using Microsoft.AspNetCore.Identity;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.DAL.Abstractions;
using YelG.Model.Auth;

namespace YelG.BLL.Services.Users
{
    public sealed class AccountService : IAccountService
    {
        private readonly UserManager<User> _userManager;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly LoginThrottle _throttle;

        public AccountService(
            UserManager<User> userManager,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            LoginThrottle throttle)
        {
            _userManager = userManager;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _throttle = throttle;
        }

        public async Task ChangePasswordAsync(string currentPassword, string newPassword, string? address, CancellationToken ct = default)
        {
            var user = await _userManager.FindByIdAsync(_currentUser.RequireUserId().ToString())
                ?? throw new ForbiddenException("انتهت الجلسة. سجّلي الدخول مرة أخرى.");
            var userName = user.UserName ?? string.Empty;

            if (_throttle.RetryAfter(address, userName) is { } wait)
                throw new BusinessRuleException(
                    $"محاولات خاطئة كثيرة. حاولي مرة أخرى بعد {Math.Max(1, (int)Math.Ceiling(wait.TotalMinutes))} دقيقة.");
            if (string.IsNullOrEmpty(currentPassword) || string.IsNullOrEmpty(newPassword))
                throw new BusinessRuleException("اكتبي كلمة المرور الحالية والجديدة.");
            if (newPassword == currentPassword)
                throw new BusinessRuleException("كلمة المرور الجديدة يجب أن تختلف عن الحالية.");

            if (!await _userManager.CheckPasswordAsync(user, currentPassword))
            {
                _log.Add("PASSWORD_CHANGE_FAILED", "كلمة المرور الحالية غير صحيحة");
                if (_throttle.RecordFailure(address, userName))
                    _log.Add("LOGIN_THROTTLED", $"إيقاف محاولات تغيير كلمة المرور مؤقتًا بعد تكرار الخطأ؛ العنوان: {address ?? "غير معروف"}");
                await _unitOfWork.SaveChangesAsync(ct);
                throw new BusinessRuleException("كلمة المرور الحالية غير صحيحة.");
            }

            // Checks the password rules and updates the security stamp, which ends the user's other sessions.
            // A temporary password (MustChangePassword) is replaced now, so the hold is lifted in the same save;
            // if the new password is rejected the flag goes back, and the hold stays.
            var wasTemporary = user.MustChangePassword;
            user.MustChangePassword = false;
            var result = await _userManager.ChangePasswordAsync(user, currentPassword, newPassword);
            if (!result.Succeeded) user.MustChangePassword = wasTemporary;
            IdentityErrors.ThrowIfFailed(result);

            _throttle.Reset(userName);
            _log.Add("PASSWORD_CHANGED", "تغيير كلمة المرور");
            await _unitOfWork.SaveChangesAsync(ct);
        }
    }
}
