namespace YelG.WebSite.Domain.Audit
{
    /// <summary>
    /// Who changed what and when. Written automatically on save for every content, media, settings,
    /// user and enquiry change; append-only, so it does not use the editable base entity.
    /// </summary>
    public class AuditLog
    {
        public long Id { get; set; }

        /// <summary>UTC.</summary>
        public DateTime OccurredAt { get; set; }

        /// <summary>Null when the system (the seeder) made the change.</summary>
        public long? UserId { get; set; }
        public string? UserName { get; set; }

        /// <summary>Created, Updated, Deleted, Published, Hidden, Reordered, …</summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>Entity type name, e.g. "Service".</summary>
        public string EntityType { get; set; } = string.Empty;

        public string? EntityId { get; set; }

        /// <summary>JSON: changed properties with their old and new values.</summary>
        public string? Changes { get; set; }
    }
}
