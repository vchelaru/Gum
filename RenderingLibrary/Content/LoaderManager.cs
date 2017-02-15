using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using System.IO;
using RenderingLibrary.Graphics;
using Microsoft.Xna.Framework.Content;
using ToolsUtilities;

namespace RenderingLibrary.Content
{
    public class LoaderManager
    {
        #region Fields

        bool mCacheTextures = false;

        static LoaderManager mSelf;
        Texture2D mInvalidTexture;
        
        SpriteFont mDefaultSpriteFont;
        BitmapFont mDefaultBitmapFont;

        Dictionary<string, IDisposable> mCachedDisposables = new Dictionary<string, IDisposable>();

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

        public Texture2D InvalidTexture
        {
            get { return mInvalidTexture; }
        }

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

        public SpriteFont DefaultFont
        {
            get { return mDefaultSpriteFont; }
        }

        public BitmapFont DefaultBitmapFont
        {
            get
            {
                return mDefaultBitmapFont;
            }
        }

        public IEnumerable<string> ValidTextureExtensions
        {
            get
            {
                yield return "png";
                yield return "jpg";
                yield return "tga";
                yield return "gif";
            }
        }

        #endregion

        #region Methods

        public void AddDisposable(string name, IDisposable disposable)
        {
            mCachedDisposables.Add(name, disposable);
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
                    mDefaultBitmapFont = new BitmapFont(defaultFontLocation, managers);
                }
                else
                {
                    mDefaultSpriteFont = mContentManager.Load<SpriteFont>(defaultFontLocation);
                }
            }
        }

        private void CreateInvalidTextureGraphic(string invalidTextureLocation, SystemManagers managers)
        {
            if (!string.IsNullOrEmpty(invalidTextureLocation) &&
                FileManager.FileExists(invalidTextureLocation))
            {

                mInvalidTexture = Load(invalidTextureLocation, managers);
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
                mInvalidTexture = imageData.ToTexture2D(false);
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

        public SpriteFont LoadSpriteFont(string fileName)
        {
            return mContentManager.Load<SpriteFont>(fileName);

        }

        /// <summary>
        /// Loads a Texture2D from a file name.  Supports
        /// .tga, png, jpg, and .gif.
        /// </summary>
        /// <param name="fileName">The name of the file (full file name) to load from.</param>
        /// <param name="managers">The SystemManagers to pull the GraphicsDevice for.  A valid
        /// GraphicsDevice is needed to load Texture2D's.  If "null" is passed, then the singleton
        /// Renderer will be used.  </param>
        /// <returns></returns>
        // TODO: Need to remove this to ContentLoader, but that would
        // require moving the cached textures there too
        [Obsolete("Use LoadContent instead")]
        internal Texture2D Load(string fileName, SystemManagers managers)
        {
            string fileNameStandardized = FileManager.Standardize(fileName, false, false);

            if (FileManager.IsRelative(fileNameStandardized))
            {
                fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

                fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
            }


            Texture2D toReturn = null;
            lock (mCachedDisposables)
            {
                if (CacheTextures)
                {

                    if (mCachedDisposables.ContainsKey(fileNameStandardized))
                    {
                        return (Texture2D)mCachedDisposables[fileNameStandardized];
                    }
                }

                string extension = FileManager.GetExtension(fileName);
                Renderer renderer = null;
                if (managers == null)
                {
                    renderer = Renderer.Self;
                }
                else
                {
                    renderer = managers.Renderer;
                }
                if (extension == "tga")
                {
#if RENDERING_LIB_SUPPORTS_TGA
                    if (renderer.GraphicsDevice == null)
                    {
                        throw new Exception("The renderer is null - did you forget to call Initialize?");
                    }

                    Paloma.TargaImage tgaImage = new Paloma.TargaImage(fileName);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        tgaImage.Image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        stream.Seek(0, SeekOrigin.Begin); //must do this, or error is thrown in next line
                        toReturn = Texture2D.FromStream(renderer.GraphicsDevice, stream);
                    }
#else
                    throw new NotImplementedException();
#endif
                }
                else
                {
                    using (var stream = FileManager.GetStreamForFile(fileNameStandardized))
                    {
                        Texture2D texture = null;

                        texture = Texture2D.FromStream(renderer.GraphicsDevice,
                            stream);

                        texture.Name = fileNameStandardized;

                        toReturn = texture;

                    }
                }
                if (CacheTextures)
                {
                    mCachedDisposables.Add(fileNameStandardized, toReturn);
                }
            }
            return toReturn;
        }

        #endregion
    }
}
