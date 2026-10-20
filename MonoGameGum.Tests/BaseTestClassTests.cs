using Gum.GueDeriving;
using Gum.Wireframe;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Graphics;
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

    // Renderer.TextureFilter is a process-wide static; a test that sets Linear (or loads a project
    // that does) must not change how the next test's sprites sample.
    [Fact]
    public void Constructor_ShouldRestorePointTextureFilter_WhenPreviousTestLeftLinear()
    {
        Renderer.TextureFilter = TextureFilter.Linear;

        BaseTestClass testBase = new();

        try
        {
            Renderer.TextureFilter.ShouldBe(TextureFilter.Point);
        }
        finally
        {
            testBase.Dispose();
        }
    }

    [Fact]
    public void Dispose_ShouldRestorePointTextureFilter_WhenTestLeftLinear()
    {
        BaseTestClass testBase = new();
        Renderer.TextureFilter = TextureFilter.Linear;

        testBase.Dispose();

        Renderer.TextureFilter.ShouldBe(TextureFilter.Point);
    }

    // Text.IsMidWordLineBreakEnabled is a process-wide static; a test that turns it off changes how
    // every later Text wraps, so the layout sweep's text heights differ (#6245).
    [Fact]
    public void Constructor_ShouldRestoreMidWordLineBreak_WhenPreviousTestLeftItDisabled()
    {
        Text.IsMidWordLineBreakEnabled = false;

        BaseTestClass testBase = new();

        try
        {
            Text.IsMidWordLineBreakEnabled.ShouldBeTrue();
        }
        finally
        {
            testBase.Dispose();
        }
    }

    [Fact]
    public void Dispose_ShouldRestoreMidWordLineBreak_WhenTestLeftItDisabled()
    {
        BaseTestClass testBase = new();
        Text.IsMidWordLineBreakEnabled = false;

        testBase.Dispose();

        Text.IsMidWordLineBreakEnabled.ShouldBeTrue();
    }
}
