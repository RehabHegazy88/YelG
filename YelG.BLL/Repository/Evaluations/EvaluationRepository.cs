using YelG.BLL.Common;
using Microsoft.EntityFrameworkCore;
using YelG.BLL.IRepository;
using YelG.DAL.Data;
using YelG.Model.Evaluations;

namespace YelG.BLL.Repository.Evaluations
{
    public sealed class EvaluationRepository : IEvaluationRepository
    {
        private readonly YelgDbContext _context;

        public EvaluationRepository(YelgDbContext context)
        {
            _context = context;
        }

        public Task<Evaluation?> GetWithAnswersAsync(long evaluationId, CancellationToken ct = default)
        {
            return _context.Evaluations
                .AsNoTracking()
                .AsSplitQuery()
                .Include(e => e.Company).ThenInclude(c => c.Activity)
                .Include(e => e.Reviewer)
                .Include(e => e.Answers).ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(e => e.Id == evaluationId, ct);
        }

        public Task<Evaluation?> GetForUpdateAsync(long evaluationId, CancellationToken ct = default)
        {
            return _context.Evaluations
                .AsSplitQuery()
                .Include(e => e.Company)
                .Include(e => e.Reviewer)
                .Include(e => e.Answers).ThenInclude(a => a.Question)
                .FirstOrDefaultAsync(e => e.Id == evaluationId, ct);
        }

        public Task<List<Evaluation>> ListAsync(long? reviewerId, CancellationToken ct = default)
        {
            var query = _context.Evaluations
                .AsNoTracking()
                .AsSplitQuery()
                .Include(e => e.Company).ThenInclude(c => c.Activity)
                .Include(e => e.Reviewer)
                .Include(e => e.Answers).ThenInclude(a => a.Question)
                .Where(e => e.Company.IsActive);

            if (reviewerId is not null) query = query.Where(e => e.ReviewerId == reviewerId);

            return query
                .OrderByDescending(e => e.LastModifiedDate)
                .ToListAsync(ct);
        }

        public void Add(Evaluation evaluation) => _context.Evaluations.Add(evaluation);

        /// <summary>Soft delete: the evaluation and its answers stay in the database, flagged as deleted.</summary>
        public void Remove(Evaluation evaluation) => SoftDelete.MarkDeleted(evaluation);

        public void MarkChanged(Evaluation evaluation, byte[]? openedRowVersion)
        {
            var entry = _context.Entry(evaluation);
            if (openedRowVersion is { Length: > 0 })
                entry.Property(e => e.RowVersion).OriginalValue = openedRowVersion;
            if (entry.State == EntityState.Unchanged)
                entry.State = EntityState.Modified;
        }
    }
}
