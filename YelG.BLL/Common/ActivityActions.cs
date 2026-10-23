namespace YelG.BLL.Common
{
    /// <summary>Arabic label and group of every action code written to the activity log.</summary>
    public static class ActivityActions
    {
        public sealed record ActionInfo(string Code, string Label, string Group);

        public const string Login = "الدخول";
        public const string Evaluations = "التقييمات";
        public const string Companies = "الشركات";
        public const string Scope = "نطاق العمل";
        public const string QuestionBank = "بنك الأسئلة";
        public const string TaskLibrary = "مكتبة المهام";
        public const string Users = "المستخدمون";
        public const string Exports = "التصدير";
        public const string System = "النظام";

        public static IReadOnlyList<string> Groups { get; } = new[]
        {
            Login, Evaluations, Companies, Scope, QuestionBank, TaskLibrary, Users, Exports, System
        };

        public static IReadOnlyList<ActionInfo> All { get; } = new ActionInfo[]
        {
            new("LOGIN", "تسجيل دخول", Login),
            new("LOGIN_FAILED", "محاولة دخول فاشلة", Login),
            new("LOGIN_LOCKED", "إيقاف الدخول مؤقتًا", Login),
            new("LOGIN_THROTTLED", "تجاوز حد محاولات الدخول", Login),
            new("LOGOUT", "تسجيل خروج", Login),

            new("SAVE_STATE", "حفظ إجابات التقييم", Evaluations),
            new("ADMIN_STATE_UPDATE", "تعديل الـAdmin لتقييم", Evaluations),
            new("EVALUATION_SUBMITTED", "إرسال تقييم للاعتماد", Evaluations),
            new("EVALUATION_APPROVED", "اعتماد تقييم", Evaluations),
            new("EVALUATION_RETURNED", "إعادة تقييم للتعديل", Evaluations),
            new("EVALUATION_DELETED", "حذف تقييم", Evaluations),

            new("COMPANY_CREATED", "إضافة شركة", Companies),
            new("COMPANY_UPDATED", "تعديل شركة", Companies),
            new("COMPANY_REVIEWERS_UPDATE", "تعديل المقيمين", Companies),
            new("COMPANY_ASSIGNED", "إسناد شركة", Companies),          // old system
            new("COMPANY_UNASSIGNED", "إلغاء إسناد شركة", Companies),  // old system
            new("COMPANY_SYNC", "مزامنة الشركات", Companies),          // old system

            new("SCOPE_ITEM_UPDATED", "تحديث مهمة في النطاق", Scope),

            new("QUESTION_BANK_DRAFT", "إنشاء مسودة", QuestionBank),
            new("QUESTION_BANK_DISCARD", "حذف مسودة", QuestionBank),
            new("QUESTION_BANK_UPDATE", "نشر نسخة", QuestionBank),

            new("TASK_TEMPLATE_CREATE", "إضافة مهمة", TaskLibrary),
            new("TASK_TEMPLATE_UPDATE", "تعديل مهمة", TaskLibrary),
            new("TASK_TEMPLATE_DELETE", "حذف مهمة", TaskLibrary),
            new("ANSWER_RULE_CREATE", "إضافة قاعدة", TaskLibrary),
            new("ANSWER_RULE_UPDATE", "تعديل قاعدة", TaskLibrary),
            new("ANSWER_RULE_DELETE", "حذف قاعدة", TaskLibrary),
            new("PRICING_UPDATE", "تعديل المعدلات", TaskLibrary),

            new("USER_CREATE", "إضافة مستخدم", Users),
            new("USER_UPDATE", "تعديل مستخدم", Users),
            new("PASSWORD_CHANGED", "تغيير كلمة المرور", Users),
            new("PASSWORD_CHANGE_FAILED", "محاولة تغيير كلمة مرور فاشلة", Users),

            new("EXPORT_EXCEL", "تصدير Excel", Exports),

            new("SYSTEM_SETUP", "تهيئة النظام", System),               // old system
            new("LEGACY_IMPORT", "استيراد بيانات النظام القديم", System)
        };

        private static readonly Dictionary<string, ActionInfo> ByCode = All.ToDictionary(a => a.Code);

        /// <summary>Known action, or the raw code in an "أخرى" group so old or new codes still show.</summary>
        public static ActionInfo Of(string code) =>
            ByCode.TryGetValue(code, out var info) ? info : new ActionInfo(code, code, "أخرى");

        public static IReadOnlyList<string> CodesOf(string group) =>
            All.Where(a => a.Group == group).Select(a => a.Code).ToList();
    }
}
