using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;

namespace YelG.Model.QuestionBank
{
    /// <summary>
    /// A frozen snapshot of the question bank and its weights. Every evaluation points to
    /// the version it was answered against, so later edits never change old results.
    /// </summary>
    public class QuestionBankVersion : MainBaseEntity<User>
    {
        public int VersionNo { get; set; }
        public QuestionBankVersionStatus Status { get; set; } = QuestionBankVersionStatus.Draft;
        /// <summary>What changed in this version (written when it is published).</summary>
        public string? ReleaseNotes { get; set; }
        public DateTimeOffset? PublishedAt { get; set; }
        public long? PublishedById { get; set; }

        public ICollection<Category> Categories { get; set; } = new List<Category>();
    }
}
