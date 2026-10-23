using Microsoft.EntityFrameworkCore;
using YelG.BLL.IRepository;
using YelG.DAL.Data;
using YelG.Model.Scope;

namespace YelG.BLL.Repository.Scope
{
    public sealed class ScopeRepository : IScopeRepository
    {
        private readonly YelgDbContext _context;

        public ScopeRepository(YelgDbContext context)
        {
            _context = context;
        }

        public Task<List<TaskTemplate>> GetTemplatesAsync(CancellationToken ct = default)
        {
            return _context.TaskTemplates
                .AsNoTracking()
                .OrderBy(t => t.Id)
                .ToListAsync(ct);
        }

        public Task<List<AnswerRule>> GetRulesAsync(CancellationToken ct = default)
        {
            return _context.AnswerRules
                .AsNoTracking()
                .OrderBy(r => r.Id)
                .ToListAsync(ct);
        }

        public Task<List<ScopeItem>> GetScopeItemsAsync(long evaluationId, CancellationToken ct = default)
        {
            return _context.ScopeItems
                .AsNoTracking()
                .Where(s => s.EvaluationId == evaluationId)
                .ToListAsync(ct);
        }

        public Task<ScopeItem?> GetItemForUpdateAsync(long evaluationId, long templateId, bool isOptionalService, CancellationToken ct = default)
        {
            return _context.ScopeItems.FirstOrDefaultAsync(s =>
                s.EvaluationId == evaluationId && s.TaskTemplateId == templateId && s.IsOptionalService == isOptionalService, ct);
        }

        public Task<TaskTemplate?> GetTemplateAsync(long templateId, CancellationToken ct = default)
        {
            return _context.TaskTemplates.AsNoTracking().FirstOrDefaultAsync(t => t.Id == templateId, ct);
        }

        public void AddItem(ScopeItem item) => _context.ScopeItems.Add(item);

        public Task<TaskTemplate?> GetTemplateForUpdateAsync(long templateId, CancellationToken ct = default) =>
            _context.TaskTemplates.FirstOrDefaultAsync(t => t.Id == templateId, ct);

        public Task<AnswerRule?> GetRuleForUpdateAsync(long ruleId, CancellationToken ct = default) =>
            _context.AnswerRules.FirstOrDefaultAsync(r => r.Id == ruleId, ct);

        public Task<List<AnswerRule>> GetRulesForTemplateAsync(long templateId, CancellationToken ct = default) =>
            _context.AnswerRules.Where(r => r.TaskTemplateId == templateId).ToListAsync(ct);

        public Task<bool> IsTemplateUsedAsync(long templateId, CancellationToken ct = default) =>
            _context.ScopeItems.AnyAsync(s => s.TaskTemplateId == templateId, ct);

        public async Task<HashSet<long>> GetUsedTemplateIdsAsync(CancellationToken ct = default) =>
            (await _context.ScopeItems.Select(s => s.TaskTemplateId).Distinct().ToListAsync(ct)).ToHashSet();

        public Task<bool> TemplateCodeExistsAsync(string code, CancellationToken ct = default) =>
            _context.TaskTemplates.AnyAsync(t => t.Code == code, ct);

        public Task<bool> RuleCodeExistsAsync(string code, CancellationToken ct = default) =>
            _context.AnswerRules.AnyAsync(r => r.Code == code, ct);

        public async Task<PricingSettings> GetPricingForUpdateAsync(CancellationToken ct = default)
        {
            var settings = await _context.PricingSettings.OrderBy(p => p.Id).FirstOrDefaultAsync(ct);
            if (settings is not null) return settings;
            settings = new PricingSettings();
            _context.PricingSettings.Add(settings);
            return settings;
        }

        public void AddTemplate(TaskTemplate template) => _context.TaskTemplates.Add(template);

        public void RemoveTemplate(TaskTemplate template) => SoftDelete.MarkDeleted(template);

        public void AddRule(AnswerRule rule) => _context.AnswerRules.Add(rule);

        public void RemoveRule(AnswerRule rule) => SoftDelete.MarkDeleted(rule);

        public async Task<PricingSettings> GetPricingSettingsAsync(CancellationToken ct = default)
        {
            return await _context.PricingSettings.AsNoTracking().OrderBy(p => p.Id).FirstOrDefaultAsync(ct)
                ?? new PricingSettings();
        }
    }
}
