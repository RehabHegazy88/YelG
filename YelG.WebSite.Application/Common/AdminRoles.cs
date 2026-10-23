namespace YelG.WebSite.Application.Common
{
    /// <summary>Admin panel roles. An Editor manages content and enquiries; an Admin also users, settings and the audit log.</summary>
    public static class AdminRoles
    {
        public const string Admin = "Admin";
        public const string Editor = "Editor";

        public static readonly IReadOnlyList<string> All = new[] { Admin, Editor };
    }
}
