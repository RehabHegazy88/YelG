using YelG.Model.Auth;
using YelG.Model.Basic;
namespace YelG.Model.QuestionBank
{
    /// <summary>Business activity of a company (توزيع وتجارة، تصنيع، ...).</summary>
    public class Activity : LookupEntity
    {
        public int SortOrder { get; set; }
        public bool IsActive { get; set; } = true;
    }

    /// <summary>Multiplier applied to a category's base weight for a given activity (FACTORS).</summary>
    public class ActivityFactor : BaseEntity<long>
    {
        public long CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public long ActivityId { get; set; }
        public Activity Activity { get; set; } = null!;

        public decimal Factor { get; set; } = 1m;
    }
}
