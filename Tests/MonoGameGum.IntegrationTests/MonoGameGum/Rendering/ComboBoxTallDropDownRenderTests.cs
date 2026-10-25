using System;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Managers;
using Gum.Wireframe;
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
            DrawTallDropDown(new ComboBox(), "6769-codeonly.png");
        });
    }

    [Fact]
    public void Draw_ShouldKeepTallDropDownInsideCanvas_WithFromFileTheme()
    {
        WithCanvas(() => MenuFocusRenderTests.WithToolProject("FormsTemplate", mutateProject: null, () =>
        {
            InteractiveGue visual = (InteractiveGue)ObjectFinder.Self.GetComponent("Controls/ComboBox")!
                .ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
            DrawTallDropDown((ComboBox)visual.FormsControlAsObject!, "6769-fromfile.png");
        }));
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

    private static void DrawTallDropDown(ComboBox comboBox, string pngName)
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
        // Taller than the canvas, as a theme's drop-down can be on a small screen.
        comboBox.ListBox.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        comboBox.ListBox.Visual.Height = Height * 2;
        for (int i = 0; i < 40; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        MenuFocusRenderTests.Capture(SystemManagers.Default.Renderer.GraphicsDevice!, new XnaColor(32, 32, 32), pngName, Width, Height);

        popup.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(0);
        popup.AbsoluteBottom.ShouldBeLessThanOrEqualTo(Height + 0.01f);
    }
}
