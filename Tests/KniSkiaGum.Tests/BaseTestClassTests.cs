using Gum;
using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Gum.Tests.Shared;
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
            GumService.Default.Root.AbsoluteWidth.ShouldBe(SkiaHostTestFramework.CanvasWidth);
            GumService.Default.PopupRoot.Width.ShouldBe(SkiaHostTestFramework.CanvasWidth);
            GumService.Default.ModalRoot.Height.ShouldBe(SkiaHostTestFramework.CanvasHeight);
        }
        finally
        {
            GumService.Default.Root.Children!.Clear();
        }
    }

    // A test that changes where files resolve from (directly, or through GumService Initialize/
    // Uninitialize or a project load) would otherwise change which font or file every later test
    // loads (#6496).
    [Fact]
    public void Dispose_RestoresFileManagerState_WhenATestChangedIt()
    {
        FileManagerStateAssert.DisposeRestores(() => new BaseTestClass());
    }

    // SystemManagers.Initialize sets MissingFileBehavior to ThrowException, so a test that initializes
    // a runtime would otherwise make every later test throw on a missing file (#6556).
    [Fact]
    public void Dispose_RestoresGraphicalUiElementState_WhenATestChangedIt()
    {
        GraphicalUiElementStateAssert.DisposeRestores(() => new BaseTestClass());
    }

    // A project or fallback Standard Element one test leaves on ObjectFinder.Self would otherwise change
    // how later tests resolve elements and font paths (#6502).
    [Fact]
    public void Dispose_ResetsObjectFinder_WhenATestLeftAProjectAndFallbacks()
    {
        ObjectFinderStateAssert.DisposeResets(() => new BaseTestClass());
    }
}
