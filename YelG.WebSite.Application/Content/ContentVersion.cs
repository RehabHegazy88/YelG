namespace YelG.WebSite.Application.Content
{
    /// <summary>
    /// A number that changes whenever site content is saved. The cached homepage is keyed by it, so the
    /// first request after a save reads the new content. One process serves the site, so a counter in
    /// memory is enough.
    /// </summary>
    public interface IContentVersion
    {
        long Current { get; }

        void Bump();
    }

    public sealed class ContentVersion : IContentVersion
    {
        private long _current;

        public long Current => Interlocked.Read(ref _current);

        public void Bump() => Interlocked.Increment(ref _current);
    }
}
