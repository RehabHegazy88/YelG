using YelG.Model.Basic;
using YelG.Model.Evaluations;
using YelG.Model.Scope;

namespace YelG.DAL.Data
{
    /// <summary>
    /// Entities a user deletes from the screens are only flagged (Deleted / DeleteDate from BaseEntity)
    /// and hidden from every query by a global filter (HasQueryFilter in each entity's map, Maps/Main).
    /// Edits inside a question bank draft stay physical deletes: a draft was never used by an evaluation.
    /// </summary>
    public static class SoftDelete
    {
        public static readonly IReadOnlySet<Type> Types = new HashSet<Type>
        {
            typeof(Company), typeof(Evaluation), typeof(TaskTemplate), typeof(AnswerRule)
        };

        public static bool Applies(Type type) => Types.Contains(type);

        /// <summary>Flags the row; the interceptor stamps DeleteDate and the modifier on save.</summary>
        public static void MarkDeleted(BaseEntity<long> entity) => entity.Deleted = true;

        /// <summary>Unique indexes ignore deleted rows, so a deleted code or company name can be used again.</summary>
        public const string ActiveRowsFilter = "[Deleted] = 0";
    }
}
