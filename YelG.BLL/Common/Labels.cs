using YelG.Model.Enums;

namespace YelG.BLL.Common
{
    /// <summary>Arabic labels for enum values, shared by the screens and the Excel exports.</summary>
    public static class Labels
    {
        public static string Of(EvaluationStatus status) => status switch
        {
            EvaluationStatus.Submitted => "بانتظار الاعتماد",
            EvaluationStatus.Approved => "معتمد",
            EvaluationStatus.Returned => "مُعاد للتعديل",
            _ => "مسودة"
        };

        public static string Of(ConfidenceGrade grade) => grade switch
        {
            ConfidenceGrade.High => "عالية",
            ConfidenceGrade.Medium => "متوسطة",
            _ => "منخفضة"
        };

        public static string Of(ScopeItemStatus status) => status switch
        {
            ScopeItemStatus.InProgress => "قيد التنفيذ",
            ScopeItemStatus.Waiting => "بانتظار مستندات",
            ScopeItemStatus.Review => "قيد المراجعة",
            ScopeItemStatus.Completed => "مكتملة",
            _ => "مخططة"
        };

        public static string Of(TaskOwner owner) => owner switch
        {
            TaskOwner.Client => "العميل",
            TaskOwner.Shared => "مشترك",
            _ => "فريق YELG"
        };

        public static string Of(TaskPriority priority) => priority switch
        {
            TaskPriority.High => "عالية",
            TaskPriority.Low => "منخفضة",
            _ => "متوسطة"
        };

        public static string Of(TaskFrequency frequency) => frequency switch
        {
            TaskFrequency.OneTime => "مرة واحدة",
            TaskFrequency.Weekly => "أسبوعي",
            TaskFrequency.BiWeekly => "كل أسبوعين",
            TaskFrequency.Monthly => "شهري",
            TaskFrequency.Quarterly => "ربع سنوي",
            TaskFrequency.SemiAnnual => "نصف سنوي",
            TaskFrequency.Annual => "سنوي",
            _ => "حسب الحاجة"
        };

        public static string Of(QuestionKind kind) => kind switch
        {
            QuestionKind.Binary => "ثنائي",
            QuestionKind.Graded => "متدرّج",
            QuestionKind.Numeric => "رقمي",
            _ => "سياق"
        };

        public static string Of(AnswerType type) => type switch
        {
            AnswerType.YesNo => "نعم/لا",
            AnswerType.Choice => "اختيار",
            AnswerType.Multi => "متعدد",
            AnswerType.Number => "رقم",
            AnswerType.Date => "تاريخ",
            _ => "نص"
        };

        public static string Of(QuestionDirection direction) => direction == QuestionDirection.Negative ? "سلبي" : "إيجابي";

        public static string Of(QuestionBankVersionStatus status) => status switch
        {
            QuestionBankVersionStatus.Published => "منشورة",
            QuestionBankVersionStatus.Archived => "مؤرشفة",
            _ => "مسودة"
        };

        public static string Of(RuleOperator op) => op switch
        {
            RuleOperator.Equals => "يساوي",
            RuleOperator.NotEquals => "لا يساوي",
            RuleOperator.In => "واحد من",
            RuleOperator.Contains => "يحتوي",
            RuleOperator.GreaterThan => "أكبر من",
            RuleOperator.GreaterOrEqual => "أكبر من أو يساوي",
            RuleOperator.LessThan => "أقل من",
            RuleOperator.Answered => "تمت الإجابة",
            _ => "بدون إجابة"
        };

        public static string Of(RuleEffectType effect) => effect switch
        {
            RuleEffectType.ScoreOnly => "النتيجة فقط",
            RuleEffectType.ExpandTask => "توسيع مهمة موجودة",
            RuleEffectType.Recommendation => "توصية — بدون ساعات",
            RuleEffectType.OptionalService => "خدمة اختيارية",
            _ => "إضافة إجراء مراجعة"
        };

        public static string Of(ConditionOperator op) => op == ConditionOperator.NotEquals ? "لا يساوي" : "يساوي";

        public static string Of(BasisStatus status) => status == BasisStatus.Approved ? "معتمدة من YELG" : "افتراض مبدئي";

        public static string Percent(double? value) => value is null ? "—" : value.Value.ToString("0.0") + "%";

        public static string Date(DateTimeOffset? value) =>
            value is null ? "—" : ToCairo(value.Value).ToString("yyyy-MM-dd HH:mm");

        /// <summary>Local Cairo wall-clock time, for places that need a real date value (Excel cells).</summary>
        public static DateTime ToCairo(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, CairoTime).DateTime;

        /// <summary>The moment a Cairo calendar day starts, for filtering stored UTC times by local date.</summary>
        public static DateTimeOffset CairoStartOfDay(DateOnly day)
        {
            var local = day.ToDateTime(TimeOnly.MinValue);
            return new DateTimeOffset(local, CairoTime.GetUtcOffset(local));
        }

        /// <summary>A Cairo wall-clock time (e.g. from the old Google Sheet) as an exact moment.</summary>
        public static DateTimeOffset FromCairo(DateTime local)
        {
            var unspecified = DateTime.SpecifyKind(local, DateTimeKind.Unspecified);
            return new DateTimeOffset(unspecified, CairoTime.GetUtcOffset(unspecified));
        }

        public static DateOnly CairoToday(DateTimeOffset now) => DateOnly.FromDateTime(ToCairo(now));

        // Same zone the Apps Script used (Africa/Cairo); Windows id with an IANA fallback.
        private static readonly TimeZoneInfo CairoTime = FindCairo();

        private static TimeZoneInfo FindCairo()
        {
            foreach (var id in new[] { "Africa/Cairo", "Egypt Standard Time" })
                if (TimeZoneInfo.TryFindSystemTimeZoneById(id, out var zone)) return zone;
            return TimeZoneInfo.Utc;
        }
    }
}
