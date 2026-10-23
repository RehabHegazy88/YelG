using YelG.DAL.Data;
using Microsoft.EntityFrameworkCore;
using YelG.BLL.IRepository;
using YelG.DAL;
using YelG.Model.Evaluations;

namespace YelG.BLL.Repository.Companies
{
    public sealed class CompanyRepository : ICompanyRepository
    {
        private readonly YelgDbContext _context;

        public CompanyRepository(YelgDbContext context)
        {
            _context = context;
        }

        public Task<Company?> GetForUpdateAsync(long companyId, CancellationToken ct = default)
        {
            return _context.Companies
                .AsSplitQuery()
                .Include(c => c.Evaluations).ThenInclude(e => e.Answers)
                .FirstOrDefaultAsync(c => c.Id == companyId && c.IsActive, ct);
        }

        public Task<Company?> FindAsync(string name, long activityId, CancellationToken ct = default)
        {
            return _context.Companies
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Name == name && c.ActivityId == activityId, ct);
        }

        public Task<List<Company>> ListAsync(CancellationToken ct = default)
        {
            return _context.Companies
                .AsNoTracking()
                .AsSplitQuery()
                .Include(c => c.Activity)
                .Include(c => c.Evaluations).ThenInclude(e => e.Reviewer)
                .Where(c => c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync(ct);
        }

        public void Add(Company company) => _context.Companies.Add(company);

        /// <summary>Soft delete: the company and its evaluations are flagged, not removed.</summary>
        public void Remove(Company company)
        {
            SoftDelete.MarkDeleted(company);
            foreach (var evaluation in company.Evaluations) SoftDelete.MarkDeleted(evaluation);
        }
    }
}
