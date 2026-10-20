using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Shouldly;
using SkiaHostTests;
using XnaGameTime = Microsoft.Xna.Framework.GameTime;

namespace KniSkiaGum.Tests;

// Pins the shared SkiaHostTestBase reset that every interactive Skia host test project (KniSkiaGum,
// SilkNetGum, StrideGum, UnityGum) derives from. Not derived from BaseTestClass: these tests build
// one by hand to prove what it cleans up.
public class BaseTestClassTests
{
    // Mimics a test that sizes the shared Root for a different canvas through Update and leaves the
    // canvas that way, as a game that zooms does.
    private static void LeaveCanvasAndRootResized(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        GumService.Default.Update(new XnaGameTime());
    }

    private static ContainerRuntime AddFixedSizeChildToRoot()
    {
        ContainerRuntime child = new();
        child.Width = 200;
        child.Height = 100;
        child.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        GumService.Default.Root.Children!.Add(child);
        return child;
    }

    [Fact]
    public void Dispose_ShouldRestoreCanvas_WhenTestLeftItResized()
    {
        BaseTestClass testBase = new();
        LeaveCanvasAndRootResized(120, 60);

        testBase.Dispose();

        GraphicalUiElement.CanvasWidth.ShouldBe(SkiaHostTestFramework.CanvasWidth);
        GraphicalUiElement.CanvasHeight.ShouldBe(SkiaHostTestFramework.CanvasHeight);
    }

    [Fact]
    public void Dispose_ShouldPlaceRootAtOrigin_WhenTestLeftRootSizedForLargerCanvas()
    {
        BaseTestClass testBase = new();
        LeaveCanvasAndRootResized(1600, 960);
        GraphicalUiElement.CanvasWidth = SkiaHostTestFramework.CanvasWidth;
        GraphicalUiElement.CanvasHeight = SkiaHostTestFramework.CanvasHeight;
        GumService.Default.Root.UpdateLayout();

        testBase.Dispose();

        ContainerRuntime child = AddFixedSizeChildToRoot();
        try
        {
            child.GetAbsoluteX().ShouldBe(0);
            child.GetAbsoluteY().ShouldBe(0);
            GumService.Default.Root.GetAbsoluteWidth().ShouldBe(SkiaHostTestFramework.CanvasWidth);
            GumService.Default.PopupRoot.Width.ShouldBe(SkiaHostTestFramework.CanvasWidth);
            GumService.Default.ModalRoot.Height.ShouldBe(SkiaHostTestFramework.CanvasHeight);
        }
        finally
        {
            GumService.Default.Root.Children!.Clear();
        }
    }
}
