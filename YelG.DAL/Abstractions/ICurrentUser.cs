namespace YelG.DAL.Abstractions
{
    /// <summary>The signed-in user, implemented by the web project from HttpContext.</summary>
    public interface ICurrentUser
    {
        long? UserId { get; }
        string? UserName { get; }
        bool IsAdmin { get; }
    }
}
