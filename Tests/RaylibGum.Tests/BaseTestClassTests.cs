using Gum.GueDeriving;
using Gum.Wireframe;
using RenderingLibrary;
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
}
