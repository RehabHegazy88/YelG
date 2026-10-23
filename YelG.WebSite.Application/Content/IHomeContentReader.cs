namespace YelG.WebSite.Application.Content
{
    /// <summary>Loads the homepage content from the database (Infrastructure).</summary>
    public interface IHomeContentReader
    {
        /// <param name="includeHidden">Preview: hidden sections and items are included, as the editor sees them.</param>
        Task<HomeContent> ReadAsync(bool includeHidden, CancellationToken cancellationToken = default);
    }
}
