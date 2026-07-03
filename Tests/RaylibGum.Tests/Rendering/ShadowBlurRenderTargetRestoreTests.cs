using Gum.GueDeriving;
using Raylib_cs;
using RenderingLibrary.Graphics;
using Shouldly;
using static Raylib_cs.Raylib;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Issue #3464: a blurred dropshadow drawn inside a render-target container's bake bakes to the
/// screen instead of the container's offscreen texture. <see cref="Gum.Renderables.ShadowBlurRenderer.Draw"/>
/// runs its own offscreen <c>BeginTextureMode</c>/<c>EndTextureMode</c> passes while the container's
/// own <c>BeginTextureMode</c> is still active; raylib's <c>EndTextureMode</c> unconditionally
/// unbinds the active render texture and does not restore an enclosing one, so after the shadow's
/// passes the shadow's own composite — and every later sibling in the same bake — draws to the
/// default framebuffer (the screen) instead of back into the container's texture. Fixed by having
/// the renderer track the active bake's render texture (mirroring <c>ActiveCamera2D</c>) and having
/// the shadow re-establish it after its offscreen passes, before its own composite.
/// </summary>
public class ShadowBlurRenderTargetRestoreTests : BaseTestClass
{
    private static Color ReadRenderTargetPixel(RenderTexture2D renderTexture, int x, int y)
    {
        Image image = LoadImageFromTexture(renderTexture.Texture);
        try
        {
            return GetImageColor(image, x, renderTexture.Texture.Height - 1 - y);
        }
        finally
        {
            UnloadImage(image);
        }
    }

    [Fact]
    public void Draw_BlurredShadowInsideRenderTarget_BakesShadowAndLaterSiblingIntoContainerTexture()
    {
        ContainerRuntime container = new();
        container.X = 0;
        container.Y = 0;
        container.Width = 100;
        container.Height = 100;
        container.IsRenderTarget = true;

        // Shadow offset well clear of the body so the shadow region can be sampled independently.
        RectangleRuntime shadowed = new();
        shadowed.X = 10;
        shadowed.Y = 10;
        shadowed.Width = 30;
        shadowed.Height = 30;
        shadowed.IsFilled = true;
        shadowed.FillColor = new Color((byte)0, (byte)0, (byte)255, (byte)255);
        shadowed.HasDropshadow = true;
        shadowed.DropshadowColor = new Color((byte)0, (byte)0, (byte)0, (byte)255);
        shadowed.DropshadowOffsetX = 0;
        shadowed.DropshadowOffsetY = 40;
        shadowed.DropshadowBlur = 4;

        // Sibling drawn AFTER the shadowed shape — proves the bake's render target is still bound
        // for draws following the shadow, not just the shadow's own composite.
        RectangleRuntime sibling = new();
        sibling.X = 60;
        sibling.Y = 60;
        sibling.Width = 30;
        sibling.Height = 30;
        sibling.IsFilled = true;
        sibling.FillColor = new Color((byte)0, (byte)255, (byte)0, (byte)255);

        container.Children.Add(shadowed);
        container.Children.Add(sibling);

        GumService.Default.Root.Children.Add(container);
        GumService.Default.Root.UpdateLayout();

        BeginDrawing();
        GumService.Default.Draw();
        EndDrawing();

        RenderTexture2D renderTexture = Renderer.Self.TryGetBakedRenderTargetFor(container)!.Value;

        // Center of the shifted shadow region (shape is x:10-40,y:10-40; shadow shifted to y:50-80).
        Color shadowRegion = ReadRenderTargetPixel(renderTexture, 25, 65);
        // Center of the sibling (x:60-90, y:60-90).
        Color siblingRegion = ReadRenderTargetPixel(renderTexture, 75, 75);

        shadowRegion.A.ShouldBeGreaterThan((byte)50);
        siblingRegion.G.ShouldBeGreaterThan((byte)200);

        GumService.Default.Root.Children.Clear();
    }
}
