using YelG.Model.Auth;
using YelG.Model.Basic;
namespace YelG.Model.QuestionBank
{
    /// <summary>
    /// Scores a numeric answer: MinValue &lt;= value &lt; MaxValue gives Score.
    /// A null bound is open. Numeric questions without bands do not enter the score.
    /// </summary>
    public class QuestionScoreBand : BaseEntity<long>
    {
        public long QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }

        /// <summary>0..1, like an option score.</summary>
        public decimal Score { get; set; }
        public int SortOrder { get; set; }
    }
}
