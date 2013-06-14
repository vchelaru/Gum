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

        Dictionary<string, Texture2D> mCachedTextures = new Dictionary<string, Texture2D>();

        ContentManager mContentManager;


        #endregion

        #region Properties

        public bool CacheTextures
        {
            get { return mCacheTextures; }
            set
            {
                mCacheTextures = value;

                if (!mCacheTextures)
                {
                    foreach (KeyValuePair<string, Texture2D> kvp in mCachedTextures)
                    {
                        kvp.Value.Dispose();
                    }

                    mCachedTextures.Clear();

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

        #endregion

        #region Methods

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
                System.IO.File.Exists(invalidTextureLocation))
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
                toReturn = Load(fileName, managers );

            }
            catch(Exception e)
            {
                errorMessage = e.ToString();
                toReturn = InvalidTexture;
            }

            return toReturn;
        }


        public Texture2D Load(string fileName, SystemManagers managers)
        {
            string fileNameStandardized = FileManager.Standardize(fileName, false, false);
            Texture2D toReturn = null;
            lock (mCachedTextures)
            {
                if (CacheTextures)
                {

                    if (mCachedTextures.ContainsKey(fileNameStandardized))
                    {
                        return mCachedTextures[fileNameStandardized];
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

                    Paloma.TargaImage tgaImage = new Paloma.TargaImage(fileName);
                    using (MemoryStream stream = new MemoryStream())
                    {
                        tgaImage.Image.Save(stream, System.Drawing.Imaging.ImageFormat.Png);
                        stream.Seek(0, SeekOrigin.Begin); //must do this, or error is thrown in next line
                        toReturn = Texture2D.FromStream(renderer.GraphicsDevice, stream);
                    }
                }
                else
                {
                    using (FileStream stream = System.IO.File.OpenRead(fileName))
                    {
                        Texture2D texture = Texture2D.FromStream(renderer.GraphicsDevice,
                            stream);

                        texture.Name = fileName;

                        if (CacheTextures)
                        {
                            mCachedTextures.Add(fileNameStandardized, texture);
                        }

                        toReturn = texture;

                    }
                }
            }
            return toReturn;
        }

        public SpriteFont LoadSpriteFont(string fileName)
        {
            return mContentManager.Load<SpriteFont>(fileName);

        }

        #endregion
    }
}
