using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;
using XnaColor = Microsoft.Xna.Framework.Color;

namespace MonoGameGum.IntegrationTests.MonoGameGum.Rendering;

/// <summary>
/// A V3 submenu taller than the canvas draws inside it, shrunk to the canvas height (#6758). Set
/// GUM_SCREENSHOT_DIR to also write tall-submenu.png for visual review.
/// </summary>
[Collection(SharedGame.CollectionName)]
public class MenuItemSubmenuClampRenderTests : BaseTestClass
{
    [Fact]
    public void Draw_ShouldKeepTallSubmenuInsideCanvas_WhenItHasMoreRowsThanFit()
    {
        const int width = 320;
        const int height = 240;
        float previousCanvasWidth = GraphicalUiElement.CanvasWidth;
        float previousCanvasHeight = GraphicalUiElement.CanvasHeight;
        using MenuFocusRenderTests.MenuLease game = new(gumxPath: null);
        try
        {
            GraphicalUiElement.CanvasWidth = width;
            GraphicalUiElement.CanvasHeight = height;
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

            Menu menu = new();
            menu.AddToRoot();
            MenuItem file = new() { Header = "File" };
            menu.Items!.Add(file);
            for (int i = 0; i < 40; i++)
            {
                file.Items!.Add("Item " + i);
            }
            file.IsSelected = true;

            GraphicalUiElement popup = FrameworkElement.PopupRoot.Children![^1];
            MenuFocusRenderTests.Capture(game.GraphicsDevice, new XnaColor(32, 32, 32), "tall-submenu.png", width, height);

            popup.AbsoluteTop.ShouldBeGreaterThanOrEqualTo(0);
            popup.AbsoluteBottom.ShouldBeLessThanOrEqualTo(height + 0.01f);
        }
        finally
        {
            GraphicalUiElement.CanvasWidth = previousCanvasWidth;
            GraphicalUiElement.CanvasHeight = previousCanvasHeight;
        }
    }
}
