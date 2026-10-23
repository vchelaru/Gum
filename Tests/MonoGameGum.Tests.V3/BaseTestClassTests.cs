using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using Gum.Tests.Shared;
using Shouldly;

namespace MonoGameGum.Tests.V3;

// Not derived from BaseTestClass: these tests build one by hand to prove what it cleans up.
public class BaseTestClassTests
{
    // Mimics a test that sizes the shared Root for a bigger canvas through Update, then restores the
    // canvas and lays Root out again. Root is centered, so its left edge is now the restored canvas
    // center minus half of the stale, wider width.
    private static void LeaveRootSizedForLargerCanvas()
    {
        GraphicalUiElement.CanvasWidth = 1600;
        GraphicalUiElement.CanvasHeight = 960;
        Gum.GumService.Default.Update(new GameTime());
        GraphicalUiElement.CanvasWidth = 800;
        GraphicalUiElement.CanvasHeight = 600;
        Gum.GumService.Default.Root.UpdateLayout();
    }

    private static ContainerRuntime AddFixedSizeChildToRoot()
    {
        ContainerRuntime child = new();
        child.Width = 200;
        child.Height = 100;
        child.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        Gum.GumService.Default.Root.Children!.Add(child);
        return child;
    }

    [Fact]
    public void Constructor_ShouldPlaceRootAtOrigin_WhenPreviousTestLeftRootSizedForLargerCanvas()
    {
        LeaveRootSizedForLargerCanvas();

        BaseTestClass testBase = new();
        try
        {
            ContainerRuntime child = AddFixedSizeChildToRoot();

            child.GetAbsoluteX().ShouldBe(0);
            child.GetAbsoluteY().ShouldBe(0);
        }
        finally
        {
            testBase.Dispose();
        }
    }

    [Fact]
    public void Dispose_ShouldPlaceRootAtOrigin_WhenTestLeftRootSizedForLargerCanvas()
    {
        BaseTestClass testBase = new();
        LeaveRootSizedForLargerCanvas();

        testBase.Dispose();

        ContainerRuntime child = AddFixedSizeChildToRoot();
        try
        {
            child.GetAbsoluteX().ShouldBe(0);
            child.GetAbsoluteY().ShouldBe(0);
            Gum.GumService.Default.PopupRoot.Width.ShouldBe(800);
            Gum.GumService.Default.ModalRoot.Height.ShouldBe(600);
        }
        finally
        {
            Gum.GumService.Default.Root.Children!.Clear();
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
        // Dispose resets the canvas to fixed values (ResetCanvasAndRoots), so only MissingFileBehavior
        // here depends on the restore.
        GraphicalUiElementStateAssert.DisposeRestores(() => new BaseTestClass(), checkCanvas: false);
    }

    // A project or fallback Standard Element one test leaves on ObjectFinder.Self would otherwise change
    // how later tests resolve elements and font paths (#6502).
    [Fact]
    public void Dispose_ResetsObjectFinder_WhenATestLeftAProjectAndFallbacks()
    {
        ObjectFinderStateAssert.DisposeResets(() => new BaseTestClass());
    }
}
