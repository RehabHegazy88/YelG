using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.QuestionBank;

namespace YelG.Model.Evaluations
{
    public class Answer : MainBaseEntity<User>
    {
        public long EvaluationId { get; set; }
        public Evaluation Evaluation { get; set; } = null!;
        public long QuestionId { get; set; }
        public Question Question { get; set; } = null!;

        /// <summary>
        /// One value for single-answer questions, several for multi-select (including custom
        /// answers the reviewer typed). Stored as a JSON column.
        /// </summary>
        public List<string> Values { get; set; } = new();

    }
}
