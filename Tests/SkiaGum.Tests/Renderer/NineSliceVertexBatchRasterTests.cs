using Gum;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaSharp;

namespace SkiaGum.Tests.Renderer;

// #8153: NineSlices batch into DrawVertices only on a GPU canvas (SkiaGum.GpuTests covers that); a CPU raster
// canvas keeps one lattice per NineSlice, since DrawVertices blends semi-transparent texels differently there.
public class NineSliceVertexBatchRasterTests : IDisposable
{
    readonly SKSurface _surface;
    readonly SKBitmap _texture;

    public NineSliceVertexBatchRasterTests()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        _surface = SKSurface.Create(new SKImageInfo(200, 60));
        GumService.Default.Initialize(_surface.Canvas, 200, 60);
        _texture = new SKBitmap(12, 12);
        _texture.Erase(SKColors.Red);
    }

    public void Dispose()
    {
        GumService.Default.Root.Children.Clear();
        _surface.Dispose();
        _texture.Dispose();
    }

    [Fact]
    public void Draw_SameTextureNineSlicesOnRasterCanvas_DrawEachLattice()
    {
        for (int i = 0; i < 4; i++)
        {
            GumService.Default.Root.Children.Add(new NineSliceRuntime
            {
                Texture = _texture,
                X = 4 + i * 40,
                Y = 4,
                Width = 36,
                Height = 30,
                WidthUnits = DimensionUnitType.Absolute,
                HeightUnits = DimensionUnitType.Absolute,
            });
        }

        GumService.Default.Draw();

        SystemManagers.Default.Renderer.RenderStateChangeStatistics.DrawCallCount.ShouldBe(4);
    }
}
