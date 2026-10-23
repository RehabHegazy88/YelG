namespace YelG.Model.Audit
{
    /// <summary>
    /// Append-only audit trail of what users did. Rows are never edited or deleted, so it does not
    /// inherit the editable base entities (no modifier, no soft delete); OccurredAt is its time.
    /// Not the same as YelG.Model.Basic.ActivityLog (the template's HTTP request log, not mapped here).
    /// </summary>
    public class ActivityLog
    {
        public long Id { get; set; }
        public DateTimeOffset OccurredAt { get; set; }
        public long? UserId { get; set; }
        public string? UserName { get; set; }

        /// <summary>Action code, e.g. LOGIN, SAVE_STATE, EVALUATION_APPROVED.</summary>
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
