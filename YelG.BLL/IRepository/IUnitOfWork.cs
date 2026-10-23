namespace YelG.BLL.IRepository
{
    /// <summary>
    /// The solution's only unit of work: commits everything the repositories changed in the current request
    /// in one transaction (one SaveChanges). Translates concurrency and duplicate-key errors into business
    /// exceptions. Services change data through repositories and commit here; they never use the DbContext.
    /// </summary>
    public interface IUnitOfWork
    {
        Task SaveChangesAsync(CancellationToken ct = default);
    }
}
