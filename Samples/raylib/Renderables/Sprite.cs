using Raylib_cs;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using static Raylib_cs.Raylib;

namespace GumTest.Renderables;
internal class Sprite : InvisibleRenderable, IAspectRatio, ITextureCoordinate
{
    public Texture2D Texture { get; set; }
    public System.Drawing.Rectangle? SourceRectangle { get; set; }


    public float? TextureWidth => Texture.Width;

    public float? TextureHeight => Texture.Height;

    public float AspectRatio => TextureHeight > 0 && TextureWidth != null ?
        (float)TextureWidth.Value / TextureHeight.Value : 1;

    bool ITextureCoordinate.Wrap{ get; set; }

    public override void Render(ISystemManagers managers)
    {
        int x = (int)this.GetAbsoluteLeft();
        int y = (int)this.GetAbsoluteTop();

        DrawTexture(Texture, x, y, Color.White);
    }
}
