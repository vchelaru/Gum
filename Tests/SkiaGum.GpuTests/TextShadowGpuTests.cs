using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.GpuTests;

/// <summary>
/// Issue #7910: the cached Text shadow mask is an Alpha8 image tinted by the shadow color, which only a GPU canvas
/// can show going wrong. Needs a GL context, like the other tests in this project.
/// </summary>
[Collection(GpuCollection.Name)]
public class TextShadowGpuTests
{
    readonly WglGpuContext _gpu;

    public TextShadowGpuTests(WglGpuContext gpu)
    {
        _gpu = gpu;
    }

    [Fact]
    public void Draw_TextDropshadow_TintsTheShadowWithTheShadowColor()
    {
        SKColor shadowAt = default;
        SKColor farAway = default;
        _gpu.WithSurface(300, 140, (surface, context) =>
        {
            GraphicalUiElement.SetPropertyOnRenderable = global::SkiaGum.CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
            SystemManagers managers = new SystemManagers();
            managers.Initialize();
            SystemManagers.Default = managers;
            managers.Canvas = surface.Canvas;

            TextRuntime text = new TextRuntime { Text = "WWWWWWWW", FontSize = 40, X = 20, Y = 20 };
            text.HasDropshadow = true;
            text.DropshadowColor = new SKColor(255, 0, 0, 255);
            text.DropshadowBlur = 2;
            text.DropshadowOffsetX = 0;
            text.DropshadowOffsetY = 30;
            text.AddToManagers(managers, null);

            surface.Canvas.Clear(SKColors.Black);
            managers.Draw();
            context.Flush(true, true);

            using SKBitmap bitmap = new SKBitmap(300, 140);
            surface.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes, 0, 0);
            // The offset puts a band of pure shadow below the white text.
            for (int y = 70; y < 90; y++)
            {
                for (int x = 20; x < 200; x++)
                {
                    SKColor c = bitmap.GetPixel(x, y);
                    if (c.Red > shadowAt.Red)
                    {
                        shadowAt = c;
                    }
                }
            }
            farAway = bitmap.GetPixel(280, 130);
        });

        shadowAt.Red.ShouldBeGreaterThan((byte)150);
        shadowAt.Green.ShouldBeLessThan((byte)40);
        shadowAt.Blue.ShouldBeLessThan((byte)40);
        farAway.Red.ShouldBe((byte)0);
    }
}
