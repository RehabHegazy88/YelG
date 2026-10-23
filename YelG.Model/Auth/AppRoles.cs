namespace YelG.Model.Auth
{
    /// <summary>Role names used by [Authorize] and the seeder; each maps to a <see cref="RoleType"/>.</summary>
    public static class AppRoles
    {
        public const string Admin = "Admin";
        public const string User = "User";

        public static RoleType TypeOf(string roleName) => roleName == Admin ? RoleType.Admin : RoleType.Auditor;
    }

    /// <summary>Accounts the system creates itself; they never sign in.</summary>
    public static class BuiltInUsers
    {
        /// <summary>Owner of rows written without a signed-in user (seeding, startup).</summary>
        public const long SystemId = 1;
        public const string SystemUserName = "system";
    }
}
