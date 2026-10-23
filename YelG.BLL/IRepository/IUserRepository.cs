using YelG.Model.Auth;

namespace YelG.BLL.IRepository
{
    public interface IUserRepository
    {
        /// <summary>Users with the given ids (active or not), read-only.</summary>
        Task<List<User>> GetByIdsAsync(IEnumerable<long> userIds, CancellationToken ct = default);

        /// <summary>All users with the names of their roles, ordered by display name.</summary>
        Task<List<(User User, List<string> Roles)>> ListWithRolesAsync(CancellationToken ct = default);
    }
}
