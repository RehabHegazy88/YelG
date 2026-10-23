using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.Companies
{
    public interface ICompanyService
    {
        /// <summary>
        /// Creates the company once and an independent evaluation for every reviewer.
        /// Admin: the chosen reviewers plus the admin. Regular user: only that user.
        /// </summary>
        Task<ReviewerAssignmentResult> CreateAsync(CreateCompanyRequest request, CancellationToken ct = default);

        /// <summary>Admin: renames or re-classifies the company for all its reviewers; answers stay.</summary>
        Task UpdateAsync(long companyId, UpdateCompanyRequest request, CancellationToken ct = default);

        /// <summary>
        /// Admin: makes the reviewer list match <paramref name="reviewerIds"/> (the admin always stays).
        /// Reviewers who already started cannot be removed and are reported as locked.
        /// </summary>
        Task<ReviewerAssignmentResult> SetReviewersAsync(long companyId, IReadOnlyList<long> reviewerIds, CancellationToken ct = default);

        /// <summary>Admin: every company with its reviewers.</summary>
        Task<List<CompanyListItem>> ListAsync(CancellationToken ct = default);
    }
}
