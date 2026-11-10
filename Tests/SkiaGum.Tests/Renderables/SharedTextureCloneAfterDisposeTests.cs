using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using SkiaGum.Renderables;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Cloning a Sprite or NineSlice whose texture bitmap was disposed (content unload) must not crash.
/// </summary>
public class SharedTextureCloneAfterDisposeTests : SharedTextureTestBase
{
    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Clone_AfterTheLoadedBitmapWasDisposed_StillDraws(string name)
    {
        // Content unload disposes the cached bitmap while a renderable built from it is still alive; a
        // clone made then (copy/paste of a shown element) should still show the texture. Today Clone calls
        // SKImage.FromBitmap on the disposed bitmap, which crashes the process natively.
        SKBitmap sheet = Load(GridSize, seed: 0);
        GraphicalUiElement runtime = CreateRuntime(name, sheet, x: 4);
        sheet.Dispose();

        RenderableShapeBase clone = (RenderableShapeBase)((ICloneable)runtime.RenderableComponent!).Clone();

        GetImage(clone).ShouldNotBeNull();
        IsDisposed(GetImage(clone)).ShouldBeFalse();
        DrawDirect(clone).Any(value => value != 0).ShouldBeTrue();
    }

    [Theory]
    [MemberData(nameof(RenderableNames))]
    public void Texture_AssignedADisposedBitmap_IsNoTextureAndDrawsNothing(string name)
    {
        // Skia aborts the process on SKImage.FromBitmap of a disposed bitmap, so the assignment must not
        // reach it. The previous texture is replaced, not kept.
        SKBitmap live = Load(GridSize, seed: 0);
        SKBitmap disposed = Load(GridSize, seed: 1);
        disposed.Dispose();
        RenderableShapeBase renderable = CreateRenderable(name, live);

        SetTexture(renderable, disposed);

        (renderable as ITextureCoordinate)!.TextureWidth.ShouldBeNull();
        DrawDirect(renderable).Any(value => value != 0).ShouldBeFalse();
    }
}
