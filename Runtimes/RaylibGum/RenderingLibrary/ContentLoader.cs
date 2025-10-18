using Raylib_cs;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Raylib_cs.Raylib;

namespace RenderingLibrary.Content;

internal class ContentLoader : IContentLoader
{
    public T LoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            Image image = LoadImage(contentName);

            //Transform it as a texture
            return (T)(object)LoadTextureFromImage(image);
        }
        else
        {
            throw new NotImplementedException($"Error attempting to load {contentName} of type {typeof(T).AssemblyQualifiedName}");
        }
    }

    public T TryLoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            Image image = LoadImage(contentName);

            //Transform it as a texture
            return (T)(object)LoadTextureFromImage(image);
        }
        else
        {
            return default(T);
        }
    }
}
