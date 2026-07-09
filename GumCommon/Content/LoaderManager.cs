using System;
using System.Collections.Generic;
using System.Linq;

namespace RenderingLibrary.Content;

/// <summary>
/// Owns the texture/content cache and the active <see cref="IContentLoader"/> shared by every
/// GumCommon-based runtime (MonoGameGum, RaylibGum, SkiaGum, KniGum, FnaGum) and the Gum tool.
/// Callers use <see cref="LoadContent{T}"/> / <see cref="TryLoadContent{T}"/>, which delegate to
/// <see cref="ContentLoader"/>.
/// </summary>
/// <remarks>
/// <c>RenderingLibrary/Content/LoaderManager.cs</c> is a deliberate duplicate of this file (same
/// namespace/type name, never compiled together): it's compiled only via
/// <c>GumCoreShared.projitems</c>, whose sole consumer is FRB1 (FlatRedBall). See that file's
/// remarks and https://github.com/vchelaru/Gum/issues/3566 for why <see cref="CacheTextures"/>
/// defaults differently there.
/// </remarks>
public class LoaderManager
{
    #region Enums

    /// <summary>
    /// Controls what happens when content is added to the cache under a name that already exists.
    /// </summary>
    public enum ExistingContentBehavior
    {
        /// <summary>
        /// Throw an exception if content is already cached under the same name.
        /// </summary>
        ThrowException,

        /// <summary>
        /// Replace the existing cached content with the new content.
        /// </summary>
        Replace
    }

    #endregion

    #region Fields

    static LoaderManager mSelf;


    #endregion

    #region Properties

    /// <summary>
    /// The shared singleton instance.
    /// </summary>
    public static LoaderManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new LoaderManager();
            }
            return mSelf;
        }
    }

    /// <summary>
    /// The active content-loading strategy that <see cref="LoadContent{T}"/> and
    /// <see cref="TryLoadContent{T}"/> delegate to. Assign your own <see cref="IContentLoader"/> to
    /// customize asset resolution.
    /// </summary>
    public IContentLoader ContentLoader
    {
        get;
        set;
    }

    // Defaults to true here (unlike the RenderingLibrary/Content/LoaderManager.cs duplicate, which
    // defaults false): this class's consumers mostly ride the built-in ContentLoader, which checks
    // this flag before consulting the cache. See issue #3566 for the full explanation.
    bool mCacheTextures = true;
    Dictionary<string, IDisposable> mCachedDisposables = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// The currently cached content, keyed by standardized (case-insensitive) file path. Read-only;
    /// use <see cref="AddDisposable"/> / <see cref="Dispose(string)"/> to modify the cache.
    /// </summary>
    public IReadOnlyDictionary<string, IDisposable> CachedDisposables => mCachedDisposables;

    /// <summary>
    /// Whether loaded textures are cached and reused. When set to <c>false</c>, the entire cache is
    /// immediately disposed and cleared, so any still-referenced textures become invalid. Defaults to
    /// <c>true</c>.
    /// </summary>
    public bool CacheTextures
    {
        get { return mCacheTextures; }
        set
        {
            mCacheTextures = value;

            if (!mCacheTextures)
            {
                foreach (KeyValuePair<string, IDisposable> kvp in mCachedDisposables)
                {
                    kvp.Value.Dispose();
                }

                mCachedDisposables.Clear();

            }
        }
    }

    #endregion


    /// <summary>
    /// Loads content of type <typeparamref name="T"/> by delegating to the active
    /// <see cref="ContentLoader"/>.
    /// </summary>
    /// <remarks>
    /// This method does not cache; it forwards to <see cref="ContentLoader"/>. Caching is the loader's
    /// responsibility (it calls <see cref="GetDisposable"/> before loading and <see cref="AddDisposable"/>
    /// after), because each backend's caching and disposal model differs and only the loader knows which
    /// applies.
    /// </remarks>
    public T LoadContent<T>(string contentName)
    {
#if FULL_DIAGNOSTICS
        if (this.ContentLoader == null)
        {
            throw new Exception("The content loader is null - you must set it prior to calling LoadContent.");
        }
#endif

        return ContentLoader.LoadContent<T>(contentName);
    }

    /// <summary>
    /// Attempts to load content of type <typeparamref name="T"/> by delegating to
    /// <see cref="ContentLoader"/>, returning <c>default(T)</c> instead of throwing when the content
    /// cannot be loaded. As with <see cref="LoadContent{T}"/>, caching is handled by the loader.
    /// </summary>
    public T TryLoadContent<T>(string contentName)
    {

#if FULL_DIAGNOSTICS
        if (this.ContentLoader == null)
        {
            throw new Exception("The content loader is null - you must set it prior to calling LoadContent. " +
                "If you haven't yet, you must first initialize Gum.");
        }
#endif
        return ContentLoader.TryLoadContent<T>(contentName);
    }

    /// <summary>
    /// Returns the cached asset of type <typeparamref name="T"/> stored under <paramref name="contentName"/>,
    /// or <c>default(T)</c> if none is cached. Unlike <see cref="LoadContent{T}"/>, this never loads from disk.
    /// </summary>
    public T TryGetCachedDisposable<T>(string contentName)
    {
        if (mCachedDisposables.ContainsKey(contentName))
        {
            return (T)mCachedDisposables[contentName];
        }
        else
        {
            return default(T);
        }
    }

    /// <summary>
    /// Returns the cached asset stored under <paramref name="name"/>, or <c>null</c> if none is cached.
    /// Called by <see cref="IContentLoader"/> implementations to check the cache before loading.
    /// </summary>
    public IDisposable? GetDisposable(string name)
    {
        if (mCachedDisposables.ContainsKey(name))
        {
            return mCachedDisposables[name];
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// Adds a loaded, disposable asset to the cache under the given (standardized) name. Called by
    /// <see cref="IContentLoader"/> implementations after a successful load.
    /// </summary>
    /// <param name="name">The cache key, typically a standardized absolute file path.</param>
    /// <param name="disposable">The loaded asset to cache.</param>
    /// <param name="existingContentBehavior">Whether to throw or replace when <paramref name="name"/> is already cached.</param>
    public void AddDisposable(string name, IDisposable disposable, ExistingContentBehavior existingContentBehavior = ExistingContentBehavior.ThrowException)
    {
#if FULL_DIAGNOSTICS
        if (mCachedDisposables.ContainsKey(name) && existingContentBehavior == ExistingContentBehavior.ThrowException)
        {
            throw new ArgumentException(
                $"The cached disposable already contains an entry for {name}:{mCachedDisposables[name]}");
        }
#endif
        if(existingContentBehavior == ExistingContentBehavior.ThrowException)
        {
            mCachedDisposables.Add(name, disposable);
        }
        else
        {
            mCachedDisposables[name] = disposable;
        }
    }

    /// <summary>
    /// Disposes every cached asset and empties the cache.
    /// </summary>
    public void DisposeAndClear()
    {
        foreach (var item in mCachedDisposables.Values)
        {
            item.Dispose();
        }

        mCachedDisposables.Clear();
    }

    /// <summary>
    /// Disposes the cached asset stored under <paramref name="name"/> and removes it from the cache.
    /// Does nothing if no asset is cached under that name.
    /// </summary>
    public void Dispose(string name)
    {
        if (mCachedDisposables.ContainsKey(name))
        {
            mCachedDisposables[name].Dispose();
            mCachedDisposables.Remove(name);
        }
    }

    /// <summary>
    /// Removes the given asset from the cache without disposing it. Useful for long-lived assets (such
    /// as a default font) that should survive a cache clear and remain owned by the caller.
    /// </summary>
    public void RemoveWithoutDisposing(IDisposable disposable)
    {
        var kvp = mCachedDisposables.FirstOrDefault(item => item.Value == disposable);

        if(kvp.Value == disposable)
        {
            mCachedDisposables.Remove(kvp.Key);
        }
    }
}
