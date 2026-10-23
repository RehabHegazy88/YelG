using Microsoft.EntityFrameworkCore;
using YelG.BLL.IRepository;
using YelG.DAL.Data;
using YelG.Model.Auth;

namespace YelG.BLL.Repository.Users
{
    public sealed class UserRepository : IUserRepository
    {
        private readonly YelgDbContext _context;

        public UserRepository(YelgDbContext context)
        {
            _context = context;
        }

        public Task<List<User>> GetByIdsAsync(IEnumerable<long> userIds, CancellationToken ct = default)
        {
            var ids = userIds.Distinct().ToList();
            return _context.Users
                .AsNoTracking()
                .Where(u => ids.Contains(u.Id))
                .ToListAsync(ct);
        }

        public async Task<List<(User User, List<string> Roles)>> ListWithRolesAsync(CancellationToken ct = default)
        {
            var users = await _context.Users.AsNoTracking().OrderBy(u => u.DisplayName).ToListAsync(ct);
            var roles = await (
                from userRole in _context.UserRoles
                join role in _context.Roles on userRole.RoleId equals role.Id
                select new { userRole.UserId, role.Name }).AsNoTracking().ToListAsync(ct);

            var byUser = roles.GroupBy(r => r.UserId).ToDictionary(g => g.Key, g => g.Select(r => r.Name!).ToList());
            return users.Select(u => (u, byUser.TryGetValue(u.Id, out var names) ? names : new List<string>())).ToList();
        }
    }
}
