using YelG.BLL.Common;
using YelG.BLL.IRepository;
using YelG.BLL.ViewModel;
using YelG.DAL.Abstractions;
using YelG.Model.Evaluations;
using YelG.Model.QuestionBank;

namespace YelG.BLL.Services.Companies
{
    public sealed class CompanyService : ICompanyService
    {
        private const int MaxReviewers = 250;

        private readonly ICompanyRepository _companies;
        private readonly IEvaluationRepository _evaluations;
        private readonly IQuestionBankRepository _questionBank;
        private readonly IUserRepository _users;
        private readonly IActivityLogRepository _log;
        private readonly IUnitOfWork _unitOfWork;
        private readonly ICurrentUser _currentUser;
        private readonly TimeProvider _clock;

        public CompanyService(
            ICompanyRepository companies,
            IEvaluationRepository evaluations,
            IQuestionBankRepository questionBank,
            IUserRepository users,
            IActivityLogRepository log,
            IUnitOfWork unitOfWork,
            ICurrentUser currentUser,
            TimeProvider clock)
        {
            _companies = companies;
            _evaluations = evaluations;
            _questionBank = questionBank;
            _users = users;
            _log = log;
            _unitOfWork = unitOfWork;
            _currentUser = currentUser;
            _clock = clock;
        }

        public async Task<ReviewerAssignmentResult> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default)
        {
            var actorId = _currentUser.RequireUserId();
            var (name, activity) = await ValidateCompanyAsync(request.Name, request.ActivityId, ct);

            if (await _companies.FindAsync(name, activity.Id, ct) is not null)
                throw new BusinessRuleException("الشركة موجودة بالفعل بنفس الاسم والنشاط. استخدمي «إدارة المقيمين» لإضافة مقيمين لها.");

            var versionId = await RequirePublishedVersionAsync(ct);
            var company = new Company { Name = name, ActivityId = activity.Id };

            var requested = _currentUser.IsAdmin
                ? request.ReviewerIds.Append(actorId)
                : new[] { actorId };

            var (added, skipped) = await AddEvaluationsAsync(company, requested, versionId, ct);
            if (added.Count == 0)
                throw new BusinessRuleException("اختاري مستخدمًا نشطًا واحدًا على الأقل لتقييم الشركة.");

            _companies.Add(company);
            _log.Add("COMPANY_CREATED", $"إنشاء وإسناد شركة: {name}؛ عدد المقيمين: {added.Count}");
            await _unitOfWork.SaveChangesAsync(ct);

            return new ReviewerAssignmentResult(company.Id, added, Array.Empty<long>(), Array.Empty<long>(), skipped);
        }

        public async Task UpdateAsync(long companyId, UpdateCompanyRequest request, CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var company = await _companies.GetForUpdateAsync(companyId, ct)
                ?? throw new NotFoundException("الشركة المطلوبة غير موجودة.");
            var (name, activity) = await ValidateCompanyAsync(request.Name, request.ActivityId, ct);

            var duplicate = await _companies.FindAsync(name, activity.Id, ct);
            if (duplicate is not null && duplicate.Id != company.Id)
                throw new BusinessRuleException("توجد شركة أخرى بنفس الاسم والنشاط.");

            company.Name = name;
            company.ActivityId = activity.Id;
            _log.Add("COMPANY_UPDATED", $"تعديل بيانات الشركة: {name}؛ للمقيمين: {company.Evaluations.Count}");
            await _unitOfWork.SaveChangesAsync(ct);
        }

        public async Task<ReviewerAssignmentResult> SetReviewersAsync(long companyId, IReadOnlyList<long> reviewerIds, CancellationToken ct = default)
        {
            var actorId = _currentUser.RequireAdmin();
            var company = await _companies.GetForUpdateAsync(companyId, ct)
                ?? throw new NotFoundException("الشركة المطلوبة غير موجودة.");

            var desired = Normalize(reviewerIds.Append(actorId));
            var current = company.Evaluations.ToList();

            var versionId = await RequirePublishedVersionAsync(ct);
            var toAdd = desired.Where(id => current.All(e => e.ReviewerId != id));
            var (added, skipped) = await AddEvaluationsAsync(company, toAdd, versionId, ct);

            var removed = new List<long>();
            var locked = new List<long>();
            foreach (var evaluation in current.Where(e => !desired.Contains(e.ReviewerId)))
            {
                if (evaluation.HasStarted)
                {
                    locked.Add(evaluation.ReviewerId);
                    continue;
                }
                _evaluations.Remove(evaluation);   // soft delete
                removed.Add(evaluation.ReviewerId);
            }

            _log.Add("COMPANY_REVIEWERS_UPDATE",
                $"تحديث مقيمي الشركة: {company.Name}؛ إضافة {added.Count}؛ إزالة {removed.Count}؛ تعذر إزالة {locked.Count}");
            await _unitOfWork.SaveChangesAsync(ct);

            return new ReviewerAssignmentResult(company.Id, added, removed, locked, skipped);
        }

        public async Task<List<CompanyListItem>> ListAsync(CancellationToken ct = default)
        {
            _currentUser.RequireAdmin();
            var companies = await _companies.ListAsync(ct);

            return companies.Select(c => new CompanyListItem(
                c.Id,
                c.Name,
                c.ActivityId,
                c.Activity.ArabicName,
                c.Evaluations
                    .OrderBy(e => e.Reviewer.DisplayName)
                    .Select(e => new ReviewerSummary(e.ReviewerId, e.Reviewer.UserName ?? string.Empty,
                        e.Reviewer.DisplayName, e.Reviewer.IsActive, e.Id, e.Status))
                    .ToList(),
                c.LastModifiedDate.AsUtc())).ToList();
        }

        private async Task<(List<long> Added, List<SkippedReviewer> Skipped)> AddEvaluationsAsync(
            Company company, IEnumerable<long> reviewerIds, long versionId, CancellationToken ct)
        {
            var ids = Normalize(reviewerIds);
            var users = (await _users.GetByIdsAsync(ids, ct)).ToDictionary(u => u.Id);
            var actorId = _currentUser.RequireUserId();
            var now = _clock.GetUtcNow();
            var added = new List<long>();
            var skipped = new List<SkippedReviewer>();

            foreach (var id in ids)
            {
                if (!users.TryGetValue(id, out var user))
                {
                    skipped.Add(new SkippedReviewer(id, "المستخدم غير موجود."));
                    continue;
                }
                if (!user.IsActive)
                {
                    skipped.Add(new SkippedReviewer(id, "الحساب غير متاح للتقييم."));
                    continue;
                }

                company.Evaluations.Add(new Evaluation
                {
                    ReviewerId = id,
                    QuestionBankVersionId = versionId,
                    // Self-created evaluations have no assigner, so their owner may delete them.
                    AssignedById = _currentUser.IsAdmin ? actorId : null,
                    AssignedAt = now
                });
                added.Add(id);
            }

            return (added, skipped);
        }

        private async Task<(string Name, Activity Activity)> ValidateCompanyAsync(string? rawName, long activityId, CancellationToken ct)
        {
            var name = Text.Clean(rawName, 150);
            if (name.Length < 2) throw new BusinessRuleException("اسم الشركة يجب أن يكون حرفين على الأقل.");

            var activity = (await _questionBank.GetActivitiesAsync(ct)).FirstOrDefault(a => a.Id == activityId)
                ?? throw new BusinessRuleException("النشاط المحدد غير صالح.");
            return (name, activity);
        }

        private async Task<long> RequirePublishedVersionAsync(CancellationToken ct) =>
            await _questionBank.GetPublishedVersionIdAsync(ct)
            ?? throw new BusinessRuleException("لا توجد نسخة منشورة من بنك الأسئلة.");

        private static List<long> Normalize(IEnumerable<long> ids) =>
            ids.Where(id => id > 0).Distinct().Take(MaxReviewers).ToList();
    }
}
