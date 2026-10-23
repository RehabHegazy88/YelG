namespace YelG.BLL.ViewModel
{
    public sealed record UserListItem(
        long Id, string UserName, string DisplayName, bool IsAdmin, bool IsActive, DateTimeOffset? UpdatedAt,
        bool MustChangePassword = false);

    /// <param name="Password">Required when creating; on update, empty keeps the current password.</param>
    public sealed record SaveUserRequest(string UserName, string DisplayName, bool IsAdmin, bool IsActive, string? Password);
}
