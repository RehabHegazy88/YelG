namespace YelG.Model.Enums
{
    public enum EvaluationStatus
    {
        Draft = 0,      // مسودة
        Submitted = 1,  // بانتظار الاعتماد
        Approved = 2,   // معتمد
        Returned = 3    // مُعاد للتعديل
    }

    /// <summary>Confidence grade, both computed and the reviewer's professional judgment.</summary>
    public enum ConfidenceGrade
    {
        Low = 0,        // منخفضة
        Medium = 1,     // متوسطة
        High = 2        // عالية
    }
}
