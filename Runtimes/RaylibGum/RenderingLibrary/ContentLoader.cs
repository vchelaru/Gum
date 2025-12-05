using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using static Raylib_cs.Raylib;

namespace RenderingLibrary.Content;

internal class ContentLoader : IContentLoader
{
    public T LoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            return (T)(object)LoadTexture2D(contentName);
        }
        else if(typeof(T) == typeof(Font))
        {
            // try loading locally first:
            if(System.IO.File.Exists(contentName))
            {
                if(contentName.ToLower().EndsWith(".fnt"))
                {
                    return (T)(object)Raylib.LoadFont(contentName);
                }
                else
                {
                    return (T)(object)LoadFontEx(contentName, 24, null, 0);
                }
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

    private static Texture2D? LoadTexture2D(string fileName)
    {
        string fileNameStandardized = StandardizeCaseSensitive(fileName);

        Texture2D? toReturn = null;

        if (LoaderManager.Self.CacheTextures)
        {
            var cached = LoaderManager.Self.GetDisposable(fileNameStandardized) as ManagedTexture;
            if (cached != null)
            {
                return cached.Texture;
            }
        }

        if (FileManager.IsUrl(fileName))
        {
            throw new NotImplementedException("Loading textures from URLs is not implemented yet.");
        }
        else
        {
            toReturn = LoadTextureFromFile(fileName);
        }

        if (LoaderManager.Self.CacheTextures && toReturn != null)
        {
            var managedTexture = new ManagedTexture(toReturn.Value);

            LoaderManager.Self.AddDisposable(fileNameStandardized, managedTexture);
        }

        return toReturn;
    }

    private static Texture2D LoadTextureFromFile(string fileName)
    {
        Image image = LoadImage(fileName);
        //Transform it as a texture
        var toReturn = LoadTextureFromImage(image);
        return toReturn;
    }

    public static string StandardizeCaseSensitive(string fileName)
    {
        const bool preserveCase = true;

        string fileNameStandardized = FileManager.Standardize(fileName, preserveCase, false);

        if (FileManager.IsRelative(fileNameStandardized) && FileManager.IsUrl(fileName) == false)
        {
            fileNameStandardized = FileManager.RelativeDirectory + fileNameStandardized;

            fileNameStandardized = FileManager.RemoveDotDotSlash(fileNameStandardized);
        }

        return fileNameStandardized;
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
