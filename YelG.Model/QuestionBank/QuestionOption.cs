using YelG.Model.Auth;
using YelG.Model.Basic;
namespace YelG.Model.QuestionBank
{
    public class QuestionOption : BaseEntity<long>
    {
        public long QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        public string Text { get; set; } = string.Empty;
        public int SortOrder { get; set; }

        /// <summary>Score 0..1 used by the scoring engine. Null means "not applicable" and is skipped.</summary>
        public decimal? Score { get; set; }
    }
}
