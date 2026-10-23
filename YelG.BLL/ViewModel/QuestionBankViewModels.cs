using YelG.Model.Enums;

namespace YelG.BLL.ViewModel
{
    public sealed record ActivityItem(long Id, string Name);

    public sealed record OptionForm(string Text, double? Score);

    public sealed record QuestionForm(
        string Code, string Text, AnswerType AnswerType, QuestionKind Kind, bool IsCritical, bool AllowCustomAnswer,
        string? ParentCode, IReadOnlyList<OptionForm> Options);

    public sealed record CategoryForm(string Code, string Title, string Status, IReadOnlyList<QuestionForm> Questions);

    /// <summary>Everything the evaluation screen needs to render one question bank version.</summary>
    public sealed record QuestionBankForm(long VersionId, IReadOnlyList<CategoryForm> Categories);
}
