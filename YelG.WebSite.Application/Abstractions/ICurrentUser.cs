namespace YelG.WebSite.Application.Abstractions
{
    /// <summary>The signed-in admin user, implemented by the web project. Null id on public pages and in the seeder.</summary>
    public interface ICurrentUser
    {
        long? UserId { get; }
        string? UserName { get; }
        bool IsAdmin { get; }
    }
}
