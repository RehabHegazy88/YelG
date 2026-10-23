namespace YelG.Model.Enums
{
    public enum TaskFrequency
    {
        OneTime = 0,    // مرة واحدة
        Weekly = 1,     // أسبوعي
        BiWeekly = 2,   // كل أسبوعين
        Monthly = 3,    // شهري
        Quarterly = 4,  // ربع سنوي
        SemiAnnual = 5, // نصف سنوي
        Annual = 6,     // سنوي
        AsNeeded = 7    // حسب الحاجة
    }

    public enum TaskPriority
    {
        Low = 0,        // منخفضة
        Medium = 1,     // متوسطة
        High = 2        // عالية
    }

    public enum TaskOwner
    {
        YelgTeam = 0,   // فريق YELG
        Client = 1,     // العميل
        Shared = 2      // مشترك
    }

    public enum BasisStatus
    {
        Draft = 0,      // افتراض مبدئي
        Approved = 1    // معتمد من YELG
    }

    public enum RuleOperator
    {
        Equals = 0,
        NotEquals = 1,
        In = 2,             // values separated by |
        Contains = 3,
        GreaterThan = 4,
        GreaterOrEqual = 5,
        LessThan = 6,
        Answered = 7,
        NotAnswered = 8
    }

    /// <summary>What an answer does to the scope of work.</summary>
    public enum RuleEffectType
    {
        ScoreOnly = 0,          // النتيجة فقط
        ExpandTask = 1,         // توسيع مهمة موجودة
        AddProcedure = 2,       // إضافة إجراء مراجعة
        Recommendation = 3,     // توصية — بدون ساعات
        OptionalService = 4     // خدمة اختيارية
    }

    public enum ScopeItemStatus
    {
        Planned = 0,        // مخططة
        InProgress = 1,     // قيد التنفيذ
        Waiting = 2,        // بانتظار مستندات
        Review = 3,         // قيد المراجعة
        Completed = 4       // مكتملة
    }
}
