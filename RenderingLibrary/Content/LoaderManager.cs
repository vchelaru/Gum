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
    public class LoaderManager
    {
        public enum ExistingContentBehavior
        {
            ThrowException,
            Replace
        }

        #region Fields

        bool mCacheTextures = false;

        static LoaderManager mSelf;
        
        Dictionary<string, IDisposable> mCachedDisposables = new Dictionary<string, IDisposable>(StringComparer.OrdinalIgnoreCase);

        public IReadOnlyDictionary<string, IDisposable> CachedDisposables => mCachedDisposables;

        ContentManager mContentManager;

        

        #endregion

        #region Properties

        public IContentLoader ContentLoader
        {
            get;
            set;
        }

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

        public Texture2D InvalidTexture => Sprite.InvalidTexture;
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

        [Obsolete("Use Text.DefaultFont instead")]
        public SpriteFont DefaultFont => Text.DefaultFont;

        [Obsolete("Use Text.DefaultBitmapFont instead")]
        public BitmapFont DefaultBitmapFont => Text.DefaultBitmapFont;

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

        public void Dispose(string name)
        {
            if(mCachedDisposables.ContainsKey(name))
            {
                mCachedDisposables[name].Dispose();
                mCachedDisposables.Remove(name);
            }
        }

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

            return toReturn;
        }

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

        public void DisposeAndClear()
        {
            foreach (var item in mCachedDisposables.Values)
            {
                item.Dispose();
            }

            mCachedDisposables.Clear();
        }

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
