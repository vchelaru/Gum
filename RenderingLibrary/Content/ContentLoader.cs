using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using ToolsUtilities;

namespace RenderingLibrary.Content
{
    /// <summary>
    /// Provides a simple implementation of IContentLoader for applications
    /// using the LoaderManager and not specifying their own custom ContentLoader.
    /// This content loader uses the default SystemManagers internally.
    /// </summary>
    public class ContentLoader : IContentLoader
    {
        public SystemManagers SystemManagers { get; set; }

        //List<Atlas> atlases = new List<Atlas>();

        public T LoadContent<T>(string contentName)
        {
            if(typeof(T) == typeof(Texture2D))
            {
                var texture = LoadTexture2D(contentName, SystemManagers);
                return (T)(object)texture;
            }
            //else if(typeof(T) == (typeof(AtlasedTexture)))
            //{
            //    foreach(var atlas in atlases)
            //    {
            //        if(atlas.Contains(contentName))
            //        {
            //            var asObject = (object)atlas.Get(contentName);
            //            return (T)(asObject);
            //        }
            //    }

            //    return default(T);
            //}
            else
            {
                throw new NotImplementedException();
            }
        }

        public T TryLoadContent<T>( string contentName)
        {
            bool knownType = false;
            try
            {
                if (typeof(T) == typeof(Texture2D))
                {
                    knownType = true;
                    var texture = LoadTexture2D(contentName, SystemManagers);
                    return (T)(object)texture;
                }
                else if (typeof(T) == (typeof(AtlasedTexture)))
                {
                    knownType = true;

                    //foreach (var atlas in atlases)
                    //{
                    //    if (atlas.Contains(contentName))
                    //    {
                    //        var asObject = (object)atlas.Get(contentName);
                    //        return (T)(asObject);
                    //    }
                    //}

                    return default(T);
                }
            }
            catch
            { 
                return default( T );
            }

            if(!knownType)
            {
                throw new NotImplementedException();
            }
            else
            {
                return default(T);
            }
        }

        private Texture2D LoadTexture2D(string fileName, SystemManagers managers)
        {
            string fileNameStandardized = FileManager.Standardize(fileName, false, false);

            if (FileManager.IsRelative(fileNameStandardized) && FileManager.IsUrl(fileName) == false)
            {
                fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

                fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
            }


            Texture2D toReturn = null;
            
            if (LoaderManager.Self.CacheTextures)
            {
                var cached = LoaderManager.Self.GetDisposable(fileNameStandardized) as Texture2D;
                if (cached != null)
                {
                    return cached;
                }
            }

            if (FileManager.IsUrl(fileName))
            {
                toReturn = LoadTextureFromUrl(fileName, managers);
            }
            else
            {
                toReturn = LoadTextureFromFile(fileName, managers);
            }
            if (LoaderManager.Self.CacheTextures)
            {
                LoaderManager.Self.AddDisposable(fileNameStandardized, toReturn);
            }
            return toReturn;

        }

        private Texture2D LoadTextureFromUrl(string fileName, SystemManagers managers)
        {

            Renderer renderer = null;
            if (managers == null)
            {
                renderer = Renderer.Self;
            }
            else
            {
                renderer = managers.Renderer;
            }
            string fileNameStandardized = FileManager.Standardize(fileName, false, false);

            Texture2D texture = null;
            using (var stream = GetUrlStream(fileName))
            {

                texture = Texture2D.FromStream(renderer.GraphicsDevice,
                    stream);

                texture.Name = fileNameStandardized;
            }
            return texture;
        }

        private static System.IO.Stream GetUrlStream(string url)
        {
            byte[] imageData = null;

            using (var wc = new System.Net.WebClient())
                imageData = wc.DownloadData(url);

            return new System.IO.MemoryStream(imageData);
        }


        /// <summary>
        /// Performs a no-caching load of the texture. This will always go to disk to access a file and 
        /// will always return a unique Texture2D. This should not be used in most cases, as caching is preferred
        /// </summary>
        /// <param name="fileName">The filename to load</param>
        /// <param name="managers">The optional SystemManagers to use when loading the file to obtain a GraphicsDevice</param>
        /// <returns>The loaded Texture2D</returns>
        private Texture2D LoadTextureFromFile(string fileName, SystemManagers managers = null)
        {
            string fileNameStandardized = FileManager.Standardize(fileName, false, false);

            if (FileManager.IsRelative(fileNameStandardized))
            {
                fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

                fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
            }

            Texture2D toReturn;
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
                        toReturn.Name = fileName;
                    }
#else
                throw new NotImplementedException();
#endif
            }
#if HAS_SYSTEM_DRAWING_IMAGE

            else if (extension == "bmp")
            {
                var image = System.Drawing.Image.FromFile(fileNameStandardized);
                var bitmap = new System.Drawing.Bitmap(image);
                
                var texture = new Texture2D(renderer.GraphicsDevice, bitmap.Width, bitmap.Height, false, SurfaceFormat.Color);
                var pixels = new Microsoft.Xna.Framework.Color[bitmap.Width * bitmap.Height];
                var index = 0;
                for (var y = 0; y < bitmap.Height; y++)
                {
                    for (var x = 0; x < bitmap.Width; x++)
                    {
                        var color = bitmap.GetPixel(x, y);
                        var r = color.R;
                        var g = color.G;
                        var b = color.B;
                        var a = color.A;
                        pixels[index] = new Microsoft.Xna.Framework.Color(r, g, b, a);
                        index++;
                    }
                }
                
                texture.SetData(pixels);
                texture.Name = fileNameStandardized;

                toReturn = texture;
            }
#endif
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

            return toReturn;
        }        
    }
}
