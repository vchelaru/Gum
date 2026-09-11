using Gum.Services;
using Gum.ToolStates;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorTabPlugin_XNA.Services;
internal class SinglePixelTextureService
{
    public void RefreshSinglePixelTexture()
    {
        var projectState = Locator.GetRequiredService<IProjectState>();
        var gumProject = projectState.GumProjectSave;



        var hasCustomSinglePixelTexture =
            gumProject.SinglePixelTextureFile != null &&
            gumProject.SinglePixelTextureTop != null &&
            gumProject.SinglePixelTextureLeft != null &&
            gumProject.SinglePixelTextureRight != null &&
            gumProject.SinglePixelTextureBottom != null;

        var renderer = global::RenderingLibrary.Graphics.Renderer.Self;

        if (hasCustomSinglePixelTexture)
        {
            var loaderManager =
                global::RenderingLibrary.Content.LoaderManager.Self;

            renderer.SinglePixelTexture = loaderManager.LoadContent<Microsoft.Xna.Framework.Graphics.Texture2D>(gumProject.SinglePixelTextureFile);

            renderer.SinglePixelSourceRectangle = new Rectangle(
                gumProject.SinglePixelTextureLeft.Value,
                gumProject.SinglePixelTextureTop.Value,
                width: gumProject.SinglePixelTextureRight.Value - gumProject.SinglePixelTextureLeft.Value,
                height: gumProject.SinglePixelTextureBottom.Value - gumProject.SinglePixelTextureTop.Value);
        }
        else
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
