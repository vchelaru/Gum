using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public T LoadContent<T>(string contentName)
        {
            if(typeof(T) == typeof(Texture2D))
            {
                var texture = LoaderManager.Self.Load(contentName, SystemManagers);
                return (T)(object)texture;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
