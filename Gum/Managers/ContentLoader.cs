using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Managers
{
    public class ContentLoader : IContentLoader
    {

        public T LoadContent<T>(string contentName)
        {
            if(typeof(T) == typeof(Texture2D))
            {
                var texture = LoaderManager.Self.Load(contentName, null);
                return (T)(object)texture;
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}
