using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Content;
using ToolsUtilities;
using Color = System.Drawing.Color;
using Matrix = System.Numerics.Matrix4x4;

namespace RenderingLibrary.Content
{
    /// <summary>
    /// Owns the shared texture/content cache and the active <see cref="IContentLoader"/>. All Gum
    /// runtime asset loading flows through here: callers use <see cref="LoadContent{T}"/> /
    /// <see cref="TryLoadContent{T}"/>, which delegate to <see cref="ContentLoader"/>.
    /// </summary>
    /// <remarks>
    /// To customize how content names resolve to assets (a custom asset store, or an engine resource
    /// that is already in memory), assign your own loader to <see cref="ContentLoader"/>.
    /// The cache itself is a dictionary of <see cref="IDisposable"/> keyed by standardized file path;
    /// see <see cref="LoadContent{T}"/> for why the cache is populated by the loader rather than here.
    /// <para>
    /// This file is a deliberate duplicate of <c>GumCommon/Content/LoaderManager.cs</c> (same
    /// namespace/type name, never compiled together). This copy is compiled only via
    /// <c>GumCoreShared.projitems</c>, whose sole consumer is FRB1 (FlatRedBall) — several of its
    /// solutions (FlatRedBall.Forms per-platform, GlueView, samples, tests) reference the legacy
    /// <c>GumCore*</c> project family directly. It looks orphaned from a Gum-repo-only grep, but it
    /// is not; do not delete without checking the FRB1 repo. See
    /// https://github.com/vchelaru/Gum/issues/3566 for the full investigation.
    /// </para>
    /// </remarks>
    public class LoaderManager
    {
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

        #region Fields

        // Intentionally false, unlike GumCommon's copy (true): FRB1 always replaces ContentLoader
        // with its own ContentManagerWrapper (see FRBDK/Glue/GumPlugin/GumPlugin/Embedded/ContentManagerWrapper.cs
        // in the FRB1 repo), which delegates to FlatRedBall's own ContentManager and never consults
        // this class's cache. So this flag is moot in practice for FRB1 games; false is just the
        // safer default for the brief window before that swap happens. See issue #3566.
        bool mCacheTextures = false;

        static LoaderManager mSelf;
        
        Dictionary<string, IDisposable> mCachedDisposables = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// The currently cached content, keyed by standardized (case-insensitive) file path. Read-only;
        /// use <see cref="AddDisposable"/> / <see cref="Dispose(string)"/> to modify the cache.
        /// </summary>
        public IReadOnlyDictionary<string, IDisposable> CachedDisposables => mCachedDisposables;

        ContentManager mContentManager;

        

        #endregion

        #region Properties

        /// <summary>
        /// The active content-loading strategy that <see cref="LoadContent{T}"/> and
        /// <see cref="TryLoadContent{T}"/> delegate to. Each backend sets a default loader during
        /// initialization; assign your own <see cref="IContentLoader"/> to customize asset resolution.
        /// </summary>
        public IContentLoader ContentLoader
        {
            get;
            set;
        }

        /// <summary>
        /// Whether loaded textures are cached and reused. When set to <c>false</c>, the entire cache is
        /// immediately disposed and cleared, so any still-referenced textures become invalid; setting it
        /// back to <c>true</c> forces subsequent loads to go to disk again.
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

        /// <summary>
        /// A placeholder texture (red X) returned by <see cref="LoadOrInvalid"/> when a requested
        /// texture cannot be loaded.
        /// </summary>
        public Texture2D InvalidTexture => Sprite.InvalidTexture;

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
        /// Obsolete. Use <see cref="Text.DefaultFont"/> instead.
        /// </summary>
        [Obsolete("Use Text.DefaultFont instead")]
        public SpriteFont DefaultFont => Text.DefaultFont;

        /// <summary>
        /// Obsolete. Use <see cref="Text.DefaultBitmapFont"/> instead.
        /// </summary>
        [Obsolete("Use Text.DefaultBitmapFont instead")]
        public BitmapFont DefaultBitmapFont => Text.DefaultBitmapFont;

        /// <summary>
        /// The file extensions (without leading dot) that Gum treats as loadable textures.
        /// </summary>
        public IEnumerable<string> ValidTextureExtensions
        {
            get
            {
                yield return "png";
                yield return "jpg";
                yield return "tga";
                yield return "gif";
                yield return "svg";
                yield return "bmp";
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Adds a loaded, disposable asset to the cache under the given (standardized) name. Called by
        /// <see cref="IContentLoader"/> implementations after a successful load.
        /// </summary>
        /// <param name="name">The cache key, typically a standardized absolute file path.</param>
        /// <param name="disposable">The loaded asset to cache.</param>
        /// <param name="existingContentBehavior">Whether to throw or replace when <paramref name="name"/> is already cached.</param>
        public void AddDisposable(string name, IDisposable disposable, ExistingContentBehavior existingContentBehavior = ExistingContentBehavior.ThrowException)
        {
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
        /// Disposes the cached asset stored under <paramref name="name"/> and removes it from the cache.
        /// Does nothing if no asset is cached under that name.
        /// </summary>
        public void Dispose(string name)
        {
            if(mCachedDisposables.ContainsKey(name))
            {
                mCachedDisposables[name].Dispose();
                mCachedDisposables.Remove(name);
            }
        }

        /// <summary>
        /// Returns the cached asset stored under <paramref name="name"/>, or <c>null</c> if none is cached.
        /// Called by <see cref="IContentLoader"/> implementations to check the cache before loading.
        /// </summary>
        public IDisposable GetDisposable(string name)
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
        /// Creates the <see cref="InvalidTexture"/> placeholder and loads the default font. Called
        /// internally during runtime setup; game code typically does not need to call this directly.
        /// </summary>
        /// <param name="invalidTextureLocation">Optional path to a texture used as the invalid-texture placeholder; a generated red-X texture is used when null or missing.</param>
        /// <param name="defaultFontLocation">Path to the default font (a <c>.fnt</c> bitmap font or a content-pipeline <c>SpriteFont</c>); defaults to "hudFont" when null.</param>
        /// <param name="serviceProvider">The service provider used to create the internal <see cref="ContentManager"/>.</param>
        /// <param name="managers">The <see cref="SystemManagers"/> providing the graphics device.</param>
        public void Initialize(string invalidTextureLocation, string defaultFontLocation, IServiceProvider serviceProvider, SystemManagers managers)
        {
            if (mContentManager == null)
            {
                CreateInvalidTextureGraphic(invalidTextureLocation, managers);

                mContentManager = new ContentManager(serviceProvider, "ContentProject");

                if(defaultFontLocation == null)
                {
                    defaultFontLocation = "hudFont";
                }

                if (defaultFontLocation.EndsWith(".fnt"))
                {
                    Text.DefaultBitmapFont = new BitmapFont(defaultFontLocation, managers);
                }
                else
                {
                    Text.DefaultFont = mContentManager.Load<SpriteFont>(defaultFontLocation);
                }
            }
        }

        private void CreateInvalidTextureGraphic(string invalidTextureLocation, SystemManagers managers)
        {
            if (!string.IsNullOrEmpty(invalidTextureLocation) &&
                FileManager.FileExists(invalidTextureLocation))
            {

                Sprite.InvalidTexture = LoadContent<Texture2D>(invalidTextureLocation);
            }
            else
            {
                ImageData imageData = new ImageData(16, 16, managers);
                imageData.Fill(Microsoft.Xna.Framework.Color.White);
                for (int i = 0; i < 16; i++)
                {
                    imageData.SetPixel(i, i, Microsoft.Xna.Framework.Color.Red);
                    imageData.SetPixel(15 - i, i, Microsoft.Xna.Framework.Color.Red);

                }
                Sprite.InvalidTexture = imageData.ToTexture2D(false);
            }
        }

        /// <summary>
        /// Loads a texture by name, returning <see cref="InvalidTexture"/> (and the exception text via
        /// <paramref name="errorMessage"/>) instead of throwing when the load fails.
        /// </summary>
        public Texture2D LoadOrInvalid(string fileName, SystemManagers managers, out string errorMessage)
        {
            Texture2D toReturn;
            errorMessage = null;
            try
            {
                toReturn = LoadContent<Texture2D>(fileName);
            }
            catch(Exception e)
            {
                errorMessage = e.ToString();
                toReturn = InvalidTexture;
            }

            // On desktop the loader returns null for a missing file instead of throwing. Preserve this
            // method's contract of returning InvalidTexture (rather than null) when the load fails.
            if (toReturn == null)
            {
                toReturn = InvalidTexture;
            }

            return toReturn;
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
        /// Disposes every cached asset and empties the cache.
        /// </summary>
        public void DisposeAndClear()
        {
            foreach (var item in mCachedDisposables.Values)
            {
                item.Dispose();
            }

            mCachedDisposables.Clear();
            mContentManager = null;
        }

        /// <summary>
        /// Attempts to load content of type <typeparamref name="T"/> by delegating to
        /// <see cref="ContentLoader"/>, returning <c>default(T)</c> instead of throwing when the content
        /// cannot be loaded. As with <see cref="LoadContent{T}"/>, caching is handled by the loader.
        /// </summary>
        public T TryLoadContent<T>( string contentName)
        {

#if DEBUG
            if (this.ContentLoader == null)
            {
                throw new Exception("The content loader is null - you must set it prior to calling LoadContent.");
            }
#endif
            return ContentLoader.TryLoadContent<T>(contentName);
        }

        /// <summary>
        /// Loads content of type <typeparamref name="T"/> by delegating to the active
        /// <see cref="ContentLoader"/>.
        /// </summary>
        /// <remarks>
        /// This method does not cache; it simply forwards to <see cref="ContentLoader"/>. Caching is the
        /// loader's responsibility (it calls <see cref="GetDisposable"/> before loading and
        /// <see cref="AddDisposable"/> after) rather than being handled here. The cache logic lives in the
        /// loader, not in <see cref="LoaderManager"/>, because each backend's caching and disposal model
        /// differs and only the loader knows which applies:
        /// <list type="bullet">
        /// <item><description>
        /// Some loaders do their own caching entirely. For example, FlatRedBall's loader delegates to its
        /// own ContentManager, so this cache is bypassed; caching here too would double-cache and create
        /// disposal-ownership conflicts.
        /// </description></item>
        /// <item><description>
        /// Some backends (Raylib, Sokol) cache a disposable wrapper around a value-type texture/font and
        /// must unwrap it on a cache hit — a wrap/unwrap step a generic cache here cannot perform.
        /// </description></item>
        /// <item><description>
        /// Some backends (Skia) cache in a separate object with their own per-type caches and do not use
        /// this cache at all.
        /// </description></item>
        /// </list>
        /// The cache key is also the standardized, <see cref="ToolsUtilities.FileManager.RelativeDirectory"/>-resolved
        /// path, which is computed inside the loader; only the raw name is available here.
        /// </remarks>
        public T LoadContent<T>(string contentName)
        {
#if DEBUG
            if(this.ContentLoader == null)
            {
                throw new Exception("The content loader is null - you must set it prior to calling LoadContent.");
            }
#endif

            return ContentLoader.LoadContent<T>(contentName);
        }

        #endregion
    }
}
