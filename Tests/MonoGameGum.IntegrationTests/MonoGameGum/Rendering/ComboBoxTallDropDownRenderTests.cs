using System;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// A ComboBox drop-down taller than the canvas draws inside it, shrunk to the canvas height (#6769),
/// for the V3 code-only visual and a from-file theme. Set GUM_SCREENSHOT_DIR to also write
/// 6769-codeonly.png and 6769-fromfile.png for visual review.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class ComboBoxTallDropDownRenderTests : BaseTestClass
{
    private const int Width = 320;
    private const int Height = 160;

    [Fact]
    public void Draw_ShouldKeepTallDropDownInsideCanvas_WithCodeOnlyVisual()
    {
        WithCanvas(() =>
        {
            using MenuFocusRenderTests.MenuLease game = new(gumxPath: null);
            DrawTallDropDown(new ComboBox(), "6769-codeonly.png", dropDownHeight: Height * 2);
        });
    }

    [Fact]
    public void Draw_ShouldKeepTallDropDownInsideCanvas_WithFromFileTheme()
    {
        WithCanvas(() => MenuFocusRenderTests.WithToolProject("FormsTemplate", mutateProject: null, () =>
        {
            InteractiveGue visual = (InteractiveGue)ObjectFinder.Self.GetComponent("Controls/ComboBox")!
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            // The theme's drop-down sizes itself to its rows (FitChildren, up to about 256), taller than the canvas.
            DrawTallDropDown((ComboBox)visual.FormsControlAsObject!, "6769-fromfile.png", dropDownHeight: null);
        }));
    }

    [Fact]
    public void CanvasGrowsWhileOpen_ShouldMatchAFreshOpen_WithFromFileTheme()
    {
        WithCanvas(() => MenuFocusRenderTests.WithToolProject("FormsTemplate", mutateProject: null, () =>
        {
            InteractiveGue visual = (InteractiveGue)ObjectFinder.Self.GetComponent("Controls/ComboBox")!
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            ComboBox comboBox = (ComboBox)visual.FormsControlAsObject!;
            comboBox.AddToRoot();
            comboBox.X = 16;
            comboBox.Y = 16;
            for (int i = 0; i < 40; i++)
            {
                comboBox.Items!.Add("Item " + i);
            }

            Resize(Width, 600);
            comboBox.IsDropDownOpen = true;
            var fresh = Snapshot(comboBox);
            comboBox.IsDropDownOpen = false;

            comboBox.IsDropDownOpen = true;
            Resize(Width, Height);
            Gum.GumService.Default.Root.AnimateSelf(0.016);
            comboBox.ListBox.Visual.AbsoluteHeight.ShouldBe(Height, 0.01f);
            Resize(Width, 600);
            Gum.GumService.Default.Root.AnimateSelf(0.016);

            var grown = Snapshot(comboBox);
            grown.Levels.ShouldBe(fresh.Levels);
            grown.Top.ShouldBe(fresh.Top, 0.01f);
            grown.PopupHeight.ShouldBe(fresh.PopupHeight, 0.01f);
            grown.ClipHeight.ShouldBe(fresh.ClipHeight, 0.01f);
        }));
    }

    private static void Resize(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    // The layout values of every level between the drop-down and its rows, and the drop-down's geometry.
    private static (string Levels, float Top, float PopupHeight, float ClipHeight) Snapshot(ComboBox comboBox)
    {
        GraphicalUiElement popup = comboBox.ListBox.Visual;
        GraphicalUiElement clip = (GraphicalUiElement)comboBox.ListBox.InnerPanel.Parent!;
        System.Text.StringBuilder levels = new();
        for (GraphicalUiElement? level = clip; level != null && level != popup; level = level.Parent as GraphicalUiElement)
        {
            levels.Append($"{level.Name}:{level.HeightUnits}:{level.Height};");
        }
        return (levels.ToString(), popup.AbsoluteTop, popup.AbsoluteHeight, clip.AbsoluteHeight);
    }

    private static void WithCanvas(Action body)
    {
        float previousCanvasWidth = GraphicalUiElement.CanvasWidth;
        float previousCanvasHeight = GraphicalUiElement.CanvasHeight;
        try
        {
            body();
        }
        finally
        {
            GraphicalUiElement.CanvasWidth = previousCanvasWidth;
            GraphicalUiElement.CanvasHeight = previousCanvasHeight;
        }
    }

    private static void DrawTallDropDown(ComboBox comboBox, string pngName, float? dropDownHeight)
    {
        GraphicalUiElement.CanvasWidth = Width;
        GraphicalUiElement.CanvasHeight = Height;
        FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.X).Returns(-1000);
        cursor.Setup(c => c.Y).Returns(-1000);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(-1000);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(-1000);
        FormsUtilities.SetCursor(cursor.Object);

        comboBox.AddToRoot();
        comboBox.X = 16;
        comboBox.Y = 16;
        if (dropDownHeight != null)
        {
            comboBox.ListBox.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
            comboBox.ListBox.Visual.Height = dropDownHeight.Value;
        }
        for (int i = 0; i < 40; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        MenuFocusRenderTests.Capture(SystemManagers.Default.Renderer.GraphicsDevice!, new XnaColor(32, 32, 32), pngName, Width, Height);

        popup.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(0);
        popup.AbsoluteBottom.ShouldBeLessThanOrEqualTo(Height + 0.01f);
        popup.AbsoluteHeight.ShouldBe(Height, 0.01f);
        // The rows are clipped inside the shrunk drop-down, starting with the first row at the top.
        GraphicalUiElement clip = (GraphicalUiElement)comboBox.ListBox.InnerPanel.Parent!;
        clip.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(popup.AbsoluteTop - 0.01f);
        clip.AbsoluteBottom.ShouldBeLessThanOrEqualTo(popup.AbsoluteBottom + 0.01f);
        comboBox.ListBox.ListBoxItems[0].Visual.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 0.01f);
    }
}
