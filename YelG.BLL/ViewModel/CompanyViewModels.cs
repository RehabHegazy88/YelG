using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    /// <param name="ReviewerIds">Ignored for regular users: they always evaluate the company themselves.</param>
    public sealed record CreateCompanyRequest(string Name, long ActivityId, IReadOnlyList<long> ReviewerIds);

    public sealed record UpdateCompanyRequest(string Name, long ActivityId);

    public sealed record ReviewerSummary(
        long UserId, string UserName, string DisplayName, bool IsActive, long EvaluationId, EvaluationStatus Status);

    public sealed record CompanyListItem(
        long Id, string Name, long ActivityId, string ActivityName, IReadOnlyList<ReviewerSummary> Reviewers,
        DateTimeOffset? UpdatedAt);

    public sealed record SkippedReviewer(long UserId, string Reason);

    public sealed record ReviewerAssignmentResult(
        long CompanyId,
        IReadOnlyList<long> AddedUserIds,
        IReadOnlyList<long> RemovedUserIds,
        IReadOnlyList<long> LockedUserIds,
        IReadOnlyList<SkippedReviewer> Skipped);
}
