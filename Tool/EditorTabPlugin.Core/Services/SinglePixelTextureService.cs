using Gum.DataTypes;
using Microsoft.Xna.Framework.Graphics;
using System.Drawing;

namespace EditorTabPlugin_XNA.Services;
internal class SinglePixelTextureService
{
    public void RefreshSinglePixelTexture(GumProjectSave gumProject)
    {
        var renderer = global::RenderingLibrary.Graphics.Renderer.Self;

        Texture2D? customTexture = null;
        if (gumProject is
            {
                SinglePixelTextureFile: string textureFile,
                SinglePixelTextureTop: int top,
                SinglePixelTextureLeft: int left,
                SinglePixelTextureRight: int right,
                SinglePixelTextureBottom: int bottom
            })
        {
            var loaderManager =
                global::RenderingLibrary.Content.LoaderManager.Self;

            // Null when the file does not exist; fall back to the plain white pixel below.
            customTexture = loaderManager.LoadContent<Texture2D>(textureFile);

            if (customTexture != null)
            {
                renderer.SinglePixelTexture = customTexture;

                renderer.SinglePixelSourceRectangle = new Rectangle(
                    left,
                    top,
                    width: right - left,
                    height: bottom - top);
            }
        }

        if (customTexture == null)
        {
            var texture = new Texture2D(renderer.GraphicsDevice, 1, 1, false, SurfaceFormat.Color);
            Microsoft.Xna.Framework.Color[] pixels = new Microsoft.Xna.Framework.Color[1];
            pixels[0] = Microsoft.Xna.Framework.Color.White;
            texture.SetData(pixels);

            renderer.SinglePixelTexture = texture;
            renderer.SinglePixelSourceRectangle = null;
        }
    }
}
