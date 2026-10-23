using YelG.Model.Enums;

namespace YelG.AdminPortal.Infrastructure
{
    /// <summary>CSS classes for status badges; the Arabic text itself is in YelG.BLL.Common.Labels.</summary>
    public static class StatusCss
    {
        /// <summary>Badge for an activity log action: failures stand out, approvals and logins read as normal.</summary>
        public static string OfAction(string code) => code switch
        {
            "LOGIN_FAILED" or "LOGIN_LOCKED" or "LOGIN_THROTTLED" or "PASSWORD_CHANGE_FAILED" => "status back",
            "EVALUATION_APPROVED" or "QUESTION_BANK_UPDATE" => "status ok",
            "EVALUATION_SUBMITTED" or "EVALUATION_RETURNED" => "status wait",
            _ => "status"
        };

        public static string Of(EvaluationStatus status) => status switch
        {
            EvaluationStatus.Submitted => "status wait",
            EvaluationStatus.Approved => "status ok",
            EvaluationStatus.Returned => "status back",
            _ => "status"
        };

        public static string Of(TaskPriority priority) => priority switch
        {
            TaskPriority.High => "scope-priority high",
            TaskPriority.Medium => "scope-priority medium",
            _ => "scope-priority"
        };

        public static string Of(QuestionBankVersionStatus status) => status switch
        {
            QuestionBankVersionStatus.Published => "status ok",
            QuestionBankVersionStatus.Draft => "status wait",
            _ => "status"
        };
    }
}
