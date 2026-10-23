using System.Linq.Expressions;
using YelG.WebSite.Domain.Common;
using YelG.WebSite.Domain.Enquiries;

namespace YelG.WebSite.Infrastructure.Data
{
    /// <summary>
    /// Content, media and settings are soft-deleted: removing them flags Deleted / DeleteDate (the interceptor
    /// converts the delete) and a global filter hides them. Enquiries hold personal data and are deleted for real.
    /// </summary>
    public static class SoftDelete
    {
        private static readonly HashSet<Type> HardDeleted = new() { typeof(Enquiry), typeof(EnquiryNote) };

        public static bool Applies(Type type) => typeof(BaseEntity).IsAssignableFrom(type) && !HardDeleted.Contains(type);

        /// <summary>Unique indexes ignore deleted rows, so a deleted key can be used again.</summary>
        public const string ActiveRowsFilter = "[Deleted] = 0";

        /// <summary>e => !e.Deleted for the given entity type.</summary>
        internal static LambdaExpression NotDeletedFilter(Type type)
        {
            var e = Expression.Parameter(type, "e");
            var deleted = Expression.Property(e, nameof(BaseEntity.Deleted));
            return Expression.Lambda(Expression.Not(deleted), e);
        }
    }
}
