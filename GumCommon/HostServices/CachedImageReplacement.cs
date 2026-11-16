namespace Gum;

#if !IOS && !ANDROID

/// <summary>
/// What a host did when hot reload asked it to put a changed image's new pixels into the texture it
/// cached for that file. See <see cref="GumHotReloadManager.ReplaceCachedImage"/>.
/// </summary>
public enum CachedImageReplacement
{
    /// <summary>The cached texture could not be replaced: nothing was cached, or its format cannot be written.</summary>
    NotReplaced,

    /// <summary>The cached texture now holds the new pixels; everything drawing it shows the new art.</summary>
    Replaced,

    /// <summary>The new image is another size than the cached texture, which cannot change size in place.</summary>
    SizeChanged,
}

#endif
