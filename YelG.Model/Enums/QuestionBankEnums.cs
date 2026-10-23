namespace YelG.Model.Enums
{
    public enum QuestionBankVersionStatus
    {
        Draft = 0,
        Published = 1,
        Archived = 2
    }

    /// <summary>Answer input type (column "Type" in the Apps Script QuestionBank sheet).</summary>
    public enum AnswerType
    {
        YesNo = 0,      // نعم/لا
        Choice = 1,     // اختيار
        Multi = 2,      // متعدد
        Text = 3,       // نص
        Number = 4,     // رقم
        Date = 5        // تاريخ
    }

    /// <summary>How the answer is measured (column "Kind").</summary>
    public enum QuestionKind
    {
        Context = 0,    // سياق — does not enter the score
        Binary = 1,     // ثنائي
        Graded = 2,     // متدرّج
        Numeric = 3     // رقمي
    }

    public enum QuestionDirection
    {
        Positive = 0,   // إيجابي
        Negative = 1    // سلبي
    }

    /// <summary>Branching condition between a child question and its parent answer.</summary>
    public enum ConditionOperator
    {
        Equals = 0,     // =
        NotEquals = 1   // <>
    }
}
