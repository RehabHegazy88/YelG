using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.QuestionBank;

namespace YelG.Model.Evaluations
{
    /// <summary>
    /// The company registry. Written once; each assigned reviewer gets an independent
    /// <see cref="Evaluation"/> (replaces the Companies sheet and ReviewerKeysJSON).
    /// </summary>
    public class Company : MainBaseEntity<User>
    {
        public string Name { get; set; } = string.Empty;
        public long ActivityId { get; set; }
        public Activity Activity { get; set; } = null!;
        public bool IsActive { get; set; } = true;

        public ICollection<Evaluation> Evaluations { get; set; } = new List<Evaluation>();
    }
}
