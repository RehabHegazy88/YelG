namespace YelG.BLL.Services.Users
{
    public interface IAccountService
    {
        /// <summary>
        /// The signed-in user changes their own password. Wrong current passwords count toward the
        /// sign-in throttle (same limits as the login page). Other open sessions of the user end
        /// within a minute (the security stamp changes); the caller refreshes the current one.
        /// </summary>
        /// <param name="address">Client IP address, for the throttle.</param>
        Task ChangePasswordAsync(string currentPassword, string newPassword, string? address, CancellationToken ct = default);
    }
}
