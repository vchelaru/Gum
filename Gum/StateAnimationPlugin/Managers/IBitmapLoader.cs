namespace StateAnimationPlugin.Managers
{
    /// <summary>
    /// Loads the plugin's embedded icon resources as raw encoded image bytes (ADR-0004: image data
    /// is neutral <c>byte[]</c> at this seam; a WPF-aware caller decodes it, e.g. via
    /// <see cref="BitmapFrameFactory"/>). Drained from the former <c>Singleton&lt;BitmapLoader&gt;</c>
    /// so the animation view models can take it by constructor and be substituted in tests.
    /// </summary>
    public interface IBitmapLoader
    {
        byte[] LoadImage(string resourceName);
    }
}
