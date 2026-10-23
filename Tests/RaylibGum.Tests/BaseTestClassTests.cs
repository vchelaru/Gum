using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
using Gum.Tests.Shared;
using Shouldly;

namespace RaylibGum.Tests;

public class BaseTestClassTests
{
    [Fact]
    public void Dispose_ClearsPopupRootAndModalRootChildren()
    {
        // A popup left open (e.g. a ComboBox dropdown) keeps drawing in later tests with a texture
        // Dispose has already freed. A later test's fresh texture can then reuse that GPU id and
        // batch into the leaked popup's draw call, hiding its own (#4901).
        BaseTestClass test = new BaseTestClass();
        Gum.GumService.Default.PopupRoot.Children.Add(new ContainerRuntime());
        Gum.GumService.Default.ModalRoot.Children.Add(new ContainerRuntime());

        test.Dispose();

        Gum.GumService.Default.PopupRoot.Children.ShouldBeEmpty();
        Gum.GumService.Default.ModalRoot.Children.ShouldBeEmpty();
    }

    // Mimics a test that sizes the shared Root for a different canvas through Update and leaves the
    // canvas that way, as a game that zooms does.
    private static void LeaveCanvasAndRootResized(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Update(0);
    }

    [Fact]
    public void Dispose_ShouldRestoreCanvas_WhenTestLeftItResized()
    {
        BaseTestClass test = new BaseTestClass();
        LeaveCanvasAndRootResized(120, 60);

        test.Dispose();

        GraphicalUiElement.CanvasWidth.ShouldBe(800);
        GraphicalUiElement.CanvasHeight.ShouldBe(600);
    }

    [Fact]
    public void Dispose_ShouldSizeRootsToCanvas_WhenTestLeftRootsSizedForLargerCanvas()
    {
        BaseTestClass test = new BaseTestClass();
        LeaveCanvasAndRootResized(1600, 960);

        test.Dispose();

        ContainerRuntime child = new();
        child.Width = 200;
        child.Height = 100;
        child.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.HeightUnits = Gum.DataTypes.DimensionUnitType.Absolute;
        child.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Right;
        child.XUnits = Gum.Converters.GeneralUnitType.PixelsFromLarge;
        Gum.GumService.Default.Root.Children!.Add(child);
        try
        {
            child.GetAbsoluteRight().ShouldBe(800);
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
