using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace RenderingLibrary.Content;
public static class LoaderManagerExtensionMethods
{
    public static void Initialize(this LoaderManager loaderManager, string invalidTextureLocation, string defaultFontLocation, IServiceProvider serviceProvider, SystemManagers managers)
    {

        CreateInvalidTextureGraphic(loaderManager, invalidTextureLocation, managers);

        if (defaultFontLocation == null)
        {
            defaultFontLocation = "hudFont";
        }

        if (defaultFontLocation.EndsWith(".fnt"))
        {
            Text.DefaultBitmapFont = new BitmapFont(defaultFontLocation);
            // Remove the loaded contnet from the loaderManager so it is never accidentally disposed
            // when we clear cache
            LoaderManager.Self.RemoveWithoutDisposing(Text.DefaultBitmapFont);
            foreach(var texture in Text.DefaultBitmapFont.Textures)
            {
                LoaderManager.Self.RemoveWithoutDisposing(texture);
            }

        }
    }

    private static void CreateInvalidTextureGraphic(LoaderManager loaderManager, string invalidTextureLocation, SystemManagers managers)
    {
        if (!string.IsNullOrEmpty(invalidTextureLocation) &&
            FileManager.FileExists(invalidTextureLocation))
        {

            Sprite.InvalidTexture = loaderManager.LoadContent<Texture2D>(invalidTextureLocation);
            // Remove the loaded contnet from the loaderManager so it is never accidentally disposed
            // when we clear cache
            loaderManager.RemoveWithoutDisposing(Sprite.InvalidTexture);
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
}
