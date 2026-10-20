using RenderingLibrary.Content;

namespace SkiaGum.Content
{
    /// <summary>
    /// Loads Skia content out of embedded resources. Behaves as <see cref="RenderingLibrary.Content.ContentLoader"/>, which is the loader to use going forward.
    /// </summary>
    /// <remarks>
    /// Sealed on purpose: nothing here is virtual, so deriving from it gains nothing. To customize
    /// loading, implement <see cref="IContentLoader"/> and delegate to an instance of this class for
    /// the content names you do not handle.
    /// </remarks>
    public sealed class EmbeddedResourceContentLoader : IContentLoader
    {
        private readonly RenderingLibrary.Content.ContentLoader _contentLoader = new RenderingLibrary.Content.ContentLoader();

        /// <inheritdoc/>
        public T? LoadContent<T>(string contentName) => _contentLoader.LoadContent<T>(contentName);

        /// <inheritdoc/>
        public T? TryLoadContent<T>(string contentName) => _contentLoader.TryLoadContent<T>(contentName);
    }
}
