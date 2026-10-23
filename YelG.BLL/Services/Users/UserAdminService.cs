using Microsoft.AspNetCore.Identity;
using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Auth;

namespace YelG.BLL.Services.Users
{
    public sealed class UserAdminService : IUserAdminService
    {
        private readonly UserManager<User> _userManager;
        private readonly IUserRepository _users;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public UserAdminService(
            UserManager<User> userManager,
            IUserRepository users,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            TimeProvider clock)
        {
            _userManager = userManager;
            _users = users;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _clock = clock;
        }

        public async Task<List<UserListItem>> ListAsync(CancellationToken ct = default)
        {
            _currentUser.RequireUserId();
            var users = await _users.ListWithRolesAsync(ct);

            return users
                .Where(x => _currentUser.IsAdmin || x.User.IsActive)
                .Select(x => new UserListItem(
                    x.User.Id, x.User.UserName ?? string.Empty, x.User.DisplayName,
                    x.Roles.Contains(AppRoles.Admin), x.User.IsActive, x.User.LastModifiedDate.AsUtc(), x.User.MustChangePassword))
                .ToList();
        }

        public async Task<long> CreateAsync(SaveUserRequest request, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var userName = Text.Clean(request.UserName, 80);
            var displayName = Text.Clean(request.DisplayName, 100);
            if (userName.Length < 2) throw new BusinessRuleException("اسم المستخدم يجب أن يكون حرفين على الأقل.");
            if (string.IsNullOrEmpty(request.Password)) throw new BusinessRuleException("أدخلي كلمة مرور مبدئية للمستخدم الجديد.");

            var user = new User
            {
                UserName = userName,
                DisplayName = displayName.Length > 0 ? displayName : userName,
                IsActive = request.IsActive,
                // The admin chose this password: the user replaces it at first sign-in.
                MustChangePassword = true
            };
            Check(await _userManager.CreateAsync(user, request.Password));
            Check(await _userManager.AddToRoleAsync(user, request.IsAdmin ? AppRoles.Admin : AppRoles.User));

            _log.Add("USER_CREATE", $"إضافة مستخدم: {userName}؛ الدور: {(request.IsAdmin ? "Admin" : "مستخدم")}");
            await _unitOfWork.SaveChangesAsync(ct);
            return user.Id;
        }

        public async Task UpdateAsync(long userId, SaveUserRequest request, CancellationToken ct = default)
        {
            var adminId = _currentUser.RequireAdmin();
            var user = await _userManager.FindByIdAsync(userId.ToString())
                ?? throw new NotFoundException("المستخدم المطلوب غير موجود.");
            if (user.Id == adminId && (!request.IsActive || !request.IsAdmin))
                throw new BusinessRuleException("لا يمكن إيقاف حسابك الحالي أو إزالة صلاحية Admin منه.");

            var wasAdmin = await _userManager.IsInRoleAsync(user, AppRoles.Admin);
            var accessChanged = wasAdmin != request.IsAdmin || user.IsActive != request.IsActive;

            var displayName = Text.Clean(request.DisplayName, 100);
            if (displayName.Length > 0) user.DisplayName = displayName;
            user.IsActive = request.IsActive;
            Check(await _userManager.UpdateAsync(user));

            if (wasAdmin != request.IsAdmin)
            {
                Check(await _userManager.RemoveFromRoleAsync(user, wasAdmin ? AppRoles.Admin : AppRoles.User));
                Check(await _userManager.AddToRoleAsync(user, request.IsAdmin ? AppRoles.Admin : AppRoles.User));
            }

            if (!string.IsNullOrEmpty(request.Password))
            {
                user.MustChangePassword = true;   // a password the admin set is temporary
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                Check(await _userManager.ResetPasswordAsync(user, token, request.Password));
                await _userManager.SetLockoutEndDateAsync(user, null);
                await _userManager.ResetAccessFailedCountAsync(user);
            }

            // Invalidates existing sign-in cookies so the change applies at the next validation.
            if (accessChanged) Check(await _userManager.UpdateSecurityStampAsync(user));

            _log.Add("USER_UPDATE", $"تعديل مستخدم: {user.UserName}؛ الدور: {(request.IsAdmin ? "Admin" : "مستخدم")}؛ نشط: {(request.IsActive ? "نعم" : "لا")}");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        private static void Check(IdentityResult result) => IdentityErrors.ThrowIfFailed(result);
    }
}
