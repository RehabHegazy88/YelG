namespace YelG.BLL.Common
{
    /// <summary>Base for errors whose message is safe to show the user (Arabic, no technical detail).</summary>
    public abstract class YelgBusinessException : Exception
    {
        protected YelgBusinessException(string message) : base(message)
        {
        }
    }

    /// <summary>The request breaks a business rule (invalid input, wrong status, duplicate).</summary>
    public sealed class BusinessRuleException : YelgBusinessException
    {
        public BusinessRuleException(string message) : base(message)
        {
        }
    }

    public sealed class NotFoundException : YelgBusinessException
    {
        public NotFoundException(string message) : base(message)
        {
        }
    }

    /// <summary>The signed-in user is not allowed to do this.</summary>
    public sealed class ForbiddenException : YelgBusinessException
    {
        public ForbiddenException(string message = "ليست لديك صلاحية لتنفيذ هذا الإجراء.") : base(message)
        {
        }
    }

    /// <summary>Someone else saved the same record since it was opened.</summary>
    public sealed class ConcurrencyException : YelgBusinessException
    {
        public ConcurrencyException()
            : base("تم تعديل هذا التقييم من مستخدم آخر بعد فتحه. أعيدي تحميل الصفحة ثم احفظي مرة أخرى.")
        {
        }
    }
}
