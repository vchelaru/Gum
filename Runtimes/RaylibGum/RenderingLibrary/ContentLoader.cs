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
        else if(typeof(T) == typeof(Font))
        {
            // try loading locally first:
            if(System.IO.File.Exists(contentName))
            {
                return (T)(object)LoadFontEx(contentName, 24, null, 0);
            }
            if (System.IO.File.Exists(contentName + ".ttf"))
            {
                return (T)(object)LoadFontEx(contentName, 24, null, 0);
            }

            else
            {
                return (T)(object)LoadFontEx(GetSystemFontPath(contentName), 24, null, 0);
            }
        }
        else
        {
            throw new NotImplementedException($"Error attempting to load {contentName} of type {typeof(T).AssemblyQualifiedName}");
        }
    }
    string GetSystemFontPath(string fontFileName)
    {
        if(fontFileName.EndsWith(".ttf") == false)
        {
            fontFileName = fontFileName + ".ttf";
        }
        if (OperatingSystem.IsWindows())
            return Path.Combine("C:/Windows/Fonts", fontFileName);
        else if (OperatingSystem.IsLinux())
            return Path.Combine("/usr/share/fonts/truetype", fontFileName);
        else if (OperatingSystem.IsMacOS())
            return Path.Combine("/System/Library/Fonts", fontFileName);

        return fontFileName; // Fallback
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
