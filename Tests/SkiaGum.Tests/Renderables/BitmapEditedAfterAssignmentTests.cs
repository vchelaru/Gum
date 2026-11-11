using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderables;

/// <summary>
/// Assigning a bitmap to a Sprite or NineSlice <c>Texture</c> copies its pixels, and Skia gives Gum no signal when
/// the bitmap is edited afterward, so <c>RefreshTextures</c> copies them again (#7882).
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
        GumService.Default.PopupRoot.Children.Clear();
        GumService.Default.ModalRoot.Children.Clear();
        _surface.Dispose();
        global::RenderingLibrary.Graphics.Renderer.TextureFilter = _savedFilter;
    }

    public static TheoryData<string> RuntimeNames => new() { nameof(SpriteRuntime), nameof(NineSliceRuntime) };

    private static GraphicalUiElement CreateRuntime(string name, SKBitmap? bitmap)
    {
        GraphicalUiElement runtime = name == nameof(SpriteRuntime) ? new SpriteRuntime() : new NineSliceRuntime();
        runtime.Width = 3;
        runtime.Height = 3;
        runtime.WidthUnits = DimensionUnitType.Absolute;
        runtime.HeightUnits = DimensionUnitType.Absolute;
        if (runtime is SpriteRuntime sprite) sprite.Texture = bitmap;
        else ((NineSliceRuntime)runtime).Texture = bitmap;
        return runtime;
    }

    private SKColor DrawnPixel(GraphicalUiElement runtime, GraphicalUiElement root)
    {
        _surface.Canvas.Clear(SKColors.Transparent);
        GumService.Default.Root.Children.Clear();
        GumService.Default.PopupRoot.Children.Clear();
        GumService.Default.ModalRoot.Children.Clear();
        root.Children.Add(runtime);
        GumService.Default.Draw();
        using SKImage snapshot = _surface.Snapshot();
        using SKBitmap bitmap = SKBitmap.FromImage(snapshot);
        return bitmap.GetPixel(1, 1);
    }

    private SKColor DrawnPixel(GraphicalUiElement runtime) => DrawnPixel(runtime, GumService.Default.Root);

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void Draw_BitmapErasedAfterAssignment_ShowsOldPixelsUntilRefreshed(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement runtime = CreateRuntime(name, bitmap);
        DrawnPixel(runtime).ShouldBe(SKColors.Red);

        bitmap.Erase(SKColors.Blue);
        bitmap.NotifyPixelsChanged();

        DrawnPixel(runtime).ShouldBe(SKColors.Red);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void RefreshTextures_Element_ShowsTheNewPixels(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement runtime = CreateRuntime(name, bitmap);
        DrawnPixel(runtime).ShouldBe(SKColors.Red);

        bitmap.Erase(SKColors.Blue);
        runtime.RefreshTextures();

        DrawnPixel(runtime).ShouldBe(SKColors.Blue);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void RefreshTextures_ParentContainer_ReachesChildren(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        ContainerRuntime container = new();
        GraphicalUiElement runtime = CreateRuntime(name, bitmap);
        container.Children.Add(runtime);
        DrawnPixel(container).ShouldBe(SKColors.Red);

        bitmap.Erase(SKColors.Blue);
        container.RefreshTextures();

        DrawnPixel(container).ShouldBe(SKColors.Blue);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void RefreshTextures_BitmapOverload_UpdatesEveryRenderableSharingTheBitmap(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement first = CreateRuntime(name, bitmap);
        GraphicalUiElement second = CreateRuntime(name, bitmap);
        ContainerRuntime container = new();
        container.Children.Add(first);
        container.Children.Add(second);

        bitmap.Erase(SKColors.Blue);
        container.RefreshTextures(bitmap);

        DrawnPixel(first).ShouldBe(SKColors.Blue);
        DrawnPixel(second).ShouldBe(SKColors.Blue);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void RefreshTextures_BitmapOverload_LeavesRenderablesWithOtherBitmapsUntouched(string name)
    {
        using SKBitmap edited = new(3, 3);
        edited.Erase(SKColors.Red);
        using SKBitmap other = new(3, 3);
        other.Erase(SKColors.Green);
        GraphicalUiElement runtime = CreateRuntime(name, other);
        DrawnPixel(runtime).ShouldBe(SKColors.Green);

        other.Erase(SKColors.Blue);
        runtime.RefreshTextures(edited);

        DrawnPixel(runtime).ShouldBe(SKColors.Green);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void GumServiceRefreshTextures_AllRoots_ShowsTheNewPixels(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GumService service = GumService.Default;
        GraphicalUiElement[] roots = { service.Root, service.PopupRoot, service.ModalRoot };
        foreach (GraphicalUiElement root in roots)
        {
            GraphicalUiElement runtime = CreateRuntime(name, bitmap);
            DrawnPixel(runtime, root).ShouldBe(SKColors.Red);

            bitmap.Erase(SKColors.Blue);
            service.RefreshTextures();

            DrawnPixel(runtime, root).ShouldBe(SKColors.Blue);
            bitmap.Erase(SKColors.Red);
        }
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void GumServiceRefreshTextures_BitmapOverload_ReachesPopupRoot(string name)
    {
        using SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement runtime = CreateRuntime(name, bitmap);
        DrawnPixel(runtime, GumService.Default.PopupRoot).ShouldBe(SKColors.Red);

        bitmap.Erase(SKColors.Blue);
        GumService.Default.RefreshTextures(bitmap);

        DrawnPixel(runtime, GumService.Default.PopupRoot).ShouldBe(SKColors.Blue);
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void RefreshTextures_NullTexture_DoesNotThrow(string name)
    {
        GraphicalUiElement runtime = CreateRuntime(name, null);

        Should.NotThrow(() => runtime.RefreshTextures());
        Should.NotThrow(() => runtime.RefreshTextures(new SKBitmap(1, 1)));
        Should.NotThrow(() => GumService.Default.RefreshTextures());
    }

    [Theory]
    [MemberData(nameof(RuntimeNames))]
    public void RefreshTextures_DisposedBitmap_DoesNotThrowAndKeepsDrawing(string name)
    {
        SKBitmap bitmap = new(3, 3);
        bitmap.Erase(SKColors.Red);
        GraphicalUiElement runtime = CreateRuntime(name, bitmap);
        DrawnPixel(runtime).ShouldBe(SKColors.Red);
        bitmap.Dispose();

        Should.NotThrow(() => runtime.RefreshTextures());
        Should.NotThrow(() => runtime.RefreshTextures(bitmap));

        DrawnPixel(runtime).ShouldBe(SKColors.Red);
    }
}
