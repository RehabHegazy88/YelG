using YelG.Model.Auth;
using YelG.Model.Basic;
using YelG.Model.Enums;
using YelG.Model.Evaluations;

namespace YelG.Model.Scope
{
    /// <summary>
    /// Execution tracking of a generated task for one evaluation (SCOPE_STATE).
    /// The task list itself is generated from the answers; this row stores only what people set.
    /// </summary>
    public class ScopeItem : MainBaseEntity<User>
    {
        public long EvaluationId { get; set; }
        public Evaluation Evaluation { get; set; } = null!;
        public long TaskTemplateId { get; set; }
        public TaskTemplate TaskTemplate { get; set; } = null!;

        /// <summary>True for a row that tracks an optional service offered by a rule.</summary>
        public bool IsOptionalService { get; set; }
        public bool OptionalIncluded { get; set; }

        public ScopeItemStatus Status { get; set; } = ScopeItemStatus.Planned;
        public TaskOwner Owner { get; set; } = TaskOwner.YelgTeam;
        public DateOnly? DueDate { get; set; }
        public string? EvidenceNote { get; set; }
    }
}
