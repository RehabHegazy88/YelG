using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;

namespace YelG.Model.Scope
{
    /// <summary>A task in the YELG task library (TASK_TEMPLATES).</summary>
    public class TaskTemplate : MainBaseEntity<User>
    {
        /// <summary>Business code, e.g. "XL-C05".</summary>
        public string Code { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string? ServiceLine { get; set; }
        public string? Evidence { get; set; }
        public string? Purpose { get; set; }
        public TaskOwner DefaultOwner { get; set; } = TaskOwner.YelgTeam;

        /// <summary>Question code the task came from in the Excel library, for traceability.</summary>
        public string? SourceQuestionCode { get; set; }

        /// <summary>Hours per execution, per role.</summary>
        public RoleHours RoleHours { get; set; } = new();

        /// <summary>Numeric question whose answer scales the effort. Null means fixed volume.</summary>
        public string? DriverQuestionCode { get; set; }
        public decimal DriverUnit { get; set; } = 1m;
        public int MaxMultiplier { get; set; } = 4;

        public TaskFrequency Frequency { get; set; } = TaskFrequency.AsNeeded;
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;
        public bool DefaultIncluded { get; set; }
        public bool IsActive { get; set; } = true;

        public BasisStatus BasisStatus { get; set; } = BasisStatus.Draft;
        public string? BasisNote { get; set; }
    }
}
