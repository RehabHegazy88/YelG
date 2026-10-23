using YelG.BLL.ViewModel;

namespace YelG.BLL.Services.QuestionBank
{
    /// <summary>
    /// Admin editing of the question bank and its weights. Only a draft version can change;
    /// publishing it makes it the version new evaluations use, while existing evaluations keep theirs.
    /// </summary>
    public interface IQuestionBankAdminService
    {
        Task<QuestionBankOverview> GetOverviewAsync(CancellationToken ct = default);

        Task<QuestionBankEditor> GetEditorAsync(long versionId, CancellationToken ct = default);

        /// <summary>Copies the published version into a new draft, or returns the draft that already exists.</summary>
        Task<long> CreateDraftAsync(CancellationToken ct = default);

        Task DiscardDraftAsync(long versionId, CancellationToken ct = default);

        Task<PublishResult> PublishAsync(long versionId, string? notes, CancellationToken ct = default);

        /// <summary>Adds a category when <paramref name="categoryId"/> is null, otherwise edits it.</summary>
        Task SaveCategoryAsync(long versionId, long? categoryId, SaveCategoryRequest request, CancellationToken ct = default);

        /// <summary>Deletes the category and its questions; other questions branched from them become unconditional.</summary>
        Task DeleteCategoryAsync(long versionId, long categoryId, CancellationToken ct = default);

        /// <param name="direction">-1 moves up, +1 moves down.</param>
        Task MoveCategoryAsync(long versionId, long categoryId, int direction, CancellationToken ct = default);

        /// <summary>Adds a question when <paramref name="questionId"/> is null, otherwise edits it (options and bands are replaced).</summary>
        Task SaveQuestionAsync(long versionId, long? questionId, SaveQuestionRequest request, CancellationToken ct = default);

        /// <summary>Deletes the question; questions branched from it become unconditional.</summary>
        Task DeleteQuestionAsync(long versionId, long questionId, CancellationToken ct = default);

        Task MoveQuestionAsync(long versionId, long questionId, int direction, CancellationToken ct = default);

        Task SaveCategoryWeightsAsync(long versionId, long categoryId, SaveCategoryWeightsRequest request, CancellationToken ct = default);
    }
}
