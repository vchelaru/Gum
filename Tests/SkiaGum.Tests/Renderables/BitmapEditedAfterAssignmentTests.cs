using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// A Sprite or NineSlice <c>Texture</c> is a reference to the bitmap, so pixels written to it and announced with
/// <see cref="SKBitmap.NotifyPixelsChanged"/> show on the next draw without assigning the bitmap again, as a
/// MonoGame Texture2D does after SetData.
/// </summary>
public class BitmapEditedAfterAssignmentTests : IDisposable
{
    private readonly SKSurface _surface;
    private readonly SKFilterMode _savedFilter = global::RenderingLibrary.Graphics.Renderer.TextureFilter;

    public BitmapEditedAfterAssignmentTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(16, 8));
        GumService.Default.Initialize(_surface.Canvas, 16, 8);
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = SKFilterMode.Nearest;
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;
    }

    public static TheoryData<string> RuntimeNames => new() { nameof(SpriteRuntime), nameof(NineSliceRuntime) };

    private SKColor DrawnPixel(GraphicalUiElement runtime)
    {
        _surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Root.Children.Clear();
        GumService.Default.Root.Children.Add(runtime);
        GumService.Default.Draw();
        using SKImage snapshot = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(snapshot);
        return bitmap.GetPixel(1, 1);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_BitmapErasedAndNotifiedAfterAssignment_ShowsTheNewPixels(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement runtime = name == nameof(SpriteRuntime) ? new SpriteRuntime() : new NineSliceRuntime();
        runtime.Width = 3;
        runtime.Height = 3;
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        if (runtime is SpriteRuntime sprite) sprite.Texture = bitmap;
        else ((NineSliceRuntime)runtime).Texture = bitmap;
        DrawnPixel(runtime).ShouldBe(SKColors.Red);

        bitmap.Erase(SKColors.Blue);
        bitmap.NotifyPixelsChanged();

        DrawnPixel(runtime).ShouldBe(SKColors.Blue);
    }
}
