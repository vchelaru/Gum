using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Content;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;
using static Raylib_cs.Raylib;

namespace RenderingLibrary.Content;

public class ContentLoader : IContentLoader
{
    public T LoadContent<T>(string contentName)
    {
        if (typeof(T) == typeof(Texture2D))
        {
            return (T)(object)LoadTexture2D(contentName);
        }
        else if(typeof(T) == typeof(Font))
        {
            return (T)LoadFont(contentName);
        }
        else
        {
            throw new NotImplementedException($"Error attempting to load {contentName} of type {typeof(T).AssemblyQualifiedName}");
        }
    }

    private object LoadFont(string contentName)
    {
        ///////////////////////////////Early Out////////////////////////////////////
        string contentNameStandardized = StandardizeCaseSensitive(contentName);

        if (LoaderManager.Self.CacheTextures)
        {
            var cached = LoaderManager.Self.GetDisposable(contentNameStandardized) as ManagedFont;
            if(cached != null)
            {
                return cached.Font;
            }
        }
        ///////////////////////////////End Early Out////////////////////////////////

        Font? font = null;

        var isFnt = contentName.ToLower().EndsWith(".fnt");
        // try loading locally first:
        if (System.IO.File.Exists(contentName))
        {
            if (isFnt)
            {
                font = Raylib.LoadFont(contentName);
            }
            else
            {
                font = LoadFontEx(contentName, 24, null, 0);
            }
        }

        if (isFnt && font == null)
        {
            // If we got here, but we have an FNT file, then we should just return null:
            font = default(Font);
        }

        if (System.IO.File.Exists(contentName + ".ttf") && font == null)
        {
            font = LoadFontEx(contentName, 24, null, 0);
        }

        if(font == null)
        {
            var systemFontPath = GetSystemFontPath(contentName);
            if (File.Exists(systemFontPath))
            {
                font = LoadFontEx(systemFontPath, 24, null, 0);
            }
            else
            {
                font = default(Font);
            }
        }

        if (LoaderManager.Self.CacheTextures && font != null)
        {
            var managedFont = new ManagedFont(font.Value);

            LoaderManager.Self.AddDisposable(contentNameStandardized, managedFont);
        }


        return font;
    }

    private static Texture2D? LoadTexture2D(string fileName)
    {
        ///////////////////////////////Early Out////////////////////////////////////

        string fileNameStandardized = StandardizeCaseSensitive(fileName);
        if (LoaderManager.Self.CacheTextures)
        {
            var cached = LoaderManager.Self.GetDisposable(fileNameStandardized) as ManagedTexture;
            if (cached != null)
            {
                return cached.Texture;
            }
        }
        ///////////////////////////////End Early Out////////////////////////////////



        Texture2D? toReturn = null;
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

        var directory =
            OperatingSystem.IsWindows() ? "C:/Windows/Fonts"
            : OperatingSystem.IsLinux() ? "/usr/share/fonts/truetype"
            : OperatingSystem.IsMacOS() ? "/System/Library/Fonts"
            : string.Empty;

        // first check no-space since that's what Windows does:
        var noSpace = Path.Combine(directory, fontFileName.Replace(" ", ""));
        if(System.IO.File.Exists(noSpace))
        {
            return noSpace;
        }
        else
        {
            return Path.Combine(directory, fontFileName);
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
