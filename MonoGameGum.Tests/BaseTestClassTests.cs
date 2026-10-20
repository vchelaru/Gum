using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests;

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
}
