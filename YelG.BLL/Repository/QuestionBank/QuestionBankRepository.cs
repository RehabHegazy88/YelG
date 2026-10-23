using Microsoft.EntityFrameworkCore;
using YelG.BLL.IRepository;
using YelG.DAL.Data;
using YelG.Model.Enums;
using YelG.Model.QuestionBank;

namespace YelG.BLL.Repository.QuestionBank
{
    public sealed class QuestionBankRepository : IQuestionBankRepository
    {
        private readonly YelgDbContext _context;

        public QuestionBankRepository(YelgDbContext context)
        {
            _context = context;
        }

        public async Task<QuestionBankVersion?> GetPublishedVersionAsync(CancellationToken ct = default)
        {
            return await GetPublishedVersionIdAsync(ct) is long id ? await GetVersionAsync(id, ct) : null;
        }

        public Task<long?> GetPublishedVersionIdAsync(CancellationToken ct = default)
        {
            return _context.QuestionBankVersions
                .AsNoTracking()
                .Where(v => v.Status == QuestionBankVersionStatus.Published)
                .OrderByDescending(v => v.VersionNo)
                .Select(v => (long?)v.Id)
                .FirstOrDefaultAsync(ct);
        }

        public Task<QuestionBankVersion?> GetVersionAsync(long versionId, CancellationToken ct = default)
        {
            return _context.QuestionBankVersions
                .AsNoTracking()
                .AsSplitQuery()
                .Include(v => v.Categories).ThenInclude(c => c.Questions).ThenInclude(q => q.Options)
                .Include(v => v.Categories).ThenInclude(c => c.Questions).ThenInclude(q => q.ScoreBands)
                .Include(v => v.Categories).ThenInclude(c => c.ActivityFactors)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct);
        }

        public Task<QuestionBankVersion?> GetVersionForUpdateAsync(long versionId, CancellationToken ct = default)
        {
            return _context.QuestionBankVersions
                .AsSplitQuery()
                .Include(v => v.Categories).ThenInclude(c => c.Questions).ThenInclude(q => q.Options)
                .Include(v => v.Categories).ThenInclude(c => c.Questions).ThenInclude(q => q.ScoreBands)
                .Include(v => v.Categories).ThenInclude(c => c.ActivityFactors)
                .FirstOrDefaultAsync(v => v.Id == versionId, ct);
        }

        public async Task<List<(QuestionBankVersion Version, int EvaluationCount, int QuestionCount)>> ListVersionsAsync(CancellationToken ct = default)
        {
            var versions = await _context.QuestionBankVersions
                .AsNoTracking()
                .OrderByDescending(v => v.VersionNo)
                .Select(v => new { Version = v, Questions = v.Categories.SelectMany(c => c.Questions).Count() })
                .ToListAsync(ct);
            var counts = await _context.Evaluations
                .AsNoTracking()
                .GroupBy(e => e.QuestionBankVersionId)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count, ct);

            return versions
                .Select(v => (v.Version, counts.TryGetValue(v.Version.Id, out var n) ? n : 0, v.Questions))
                .ToList();
        }

        public Task<long?> GetDraftVersionIdAsync(CancellationToken ct = default)
        {
            return _context.QuestionBankVersions
                .AsNoTracking()
                .Where(v => v.Status == QuestionBankVersionStatus.Draft)
                .Select(v => (long?)v.Id)
                .FirstOrDefaultAsync(ct);
        }

        public async Task<int> GetMaxVersionNoAsync(CancellationToken ct = default)
        {
            return await _context.QuestionBankVersions.MaxAsync(v => (int?)v.VersionNo, ct) ?? 0;
        }

        public async Task<List<(string RuleCode, string QuestionCode)>> GetRuleQuestionCodesAsync(CancellationToken ct = default)
        {
            var rules = await _context.AnswerRules
                .AsNoTracking()
                .Where(r => r.IsActive)
                .Select(r => new { r.Code, r.QuestionCode })
                .ToListAsync(ct);
            var drivers = await _context.TaskTemplates
                .AsNoTracking()
                .Where(t => t.IsActive && t.DriverQuestionCode != null)
                .Select(t => new { t.Code, QuestionCode = t.DriverQuestionCode! })
                .ToListAsync(ct);
            return rules.Select(r => (r.Code, r.QuestionCode))
                .Concat(drivers.Select(d => (d.Code, d.QuestionCode)))
                .ToList();
        }

        public void AddVersion(QuestionBankVersion version) => _context.QuestionBankVersions.Add(version);

        public void RemoveVersion(QuestionBankVersion version) => _context.QuestionBankVersions.Remove(version);

        public void RemoveCategory(Category category) => _context.Categories.Remove(category);

        public void RemoveQuestion(Question question) => _context.Questions.Remove(question);

        public Task<List<Activity>> GetActivitiesAsync(CancellationToken ct = default)
        {
            return _context.Activities
                .AsNoTracking()
                .Where(a => a.IsActive)
                .OrderBy(a => a.SortOrder)
                .ToListAsync(ct);
        }
    }
}
