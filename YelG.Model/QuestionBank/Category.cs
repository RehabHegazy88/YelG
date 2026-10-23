using YelG.Model.Auth;
using YelG.Model.Basic;
namespace YelG.Model.QuestionBank
{
    public class Category : MainBaseEntity<User>
    {
        public long QuestionBankVersionId { get; set; }
        public QuestionBankVersion QuestionBankVersion { get; set; } = null!;

        /// <summary>Arabic letter code, e.g. "أ".</summary>
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = "معتمد";
        public int SortOrder { get; set; }

        /// <summary>Base category weight in percent (CATW). All categories of a version sum to 100.</summary>
        public decimal BaseWeight { get; set; }

        public ICollection<Question> Questions { get; set; } = new List<Question>();
        public ICollection<ActivityFactor> ActivityFactors { get; set; } = new List<ActivityFactor>();
    }
}
