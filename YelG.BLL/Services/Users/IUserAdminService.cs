using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Users
{
    public interface IUserAdminService
    {
        /// <summary>Admin: every account. Regular users get the active ones only (for display names).</summary>
        Task<List<UserListItem>> ListAsync(CancellationToken ct = default);

        Task<long> CreateAsync(SaveUserRequest request, CancellationToken ct = default);

        /// <summary>
        /// Admin: name, role, active flag and optionally a new password. An admin cannot deactivate
        /// or demote their own account. Role or status changes sign the user out within a minute.
        /// </summary>
        Task UpdateAsync(long userId, SaveUserRequest request, CancellationToken ct = default);
    }
}
