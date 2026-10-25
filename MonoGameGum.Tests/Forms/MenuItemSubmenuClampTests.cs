using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A submenu taller than the canvas is moved to the top, shrunk to the canvas height and scrolls; it
/// goes back to sizing itself to its rows once it fits again (#6758).
/// </summary>
public class MenuItemSubmenuClampTests : BaseTestClass
{
    private readonly Mock<ICursor> _cursor = new();

    public MenuItemSubmenuClampTests()
    {
        // A still cursor away from every control unless a test moves it.
        _cursor.SetupProperty(c => c.VisualOver);
        _cursor.SetupProperty(c => c.WindowPushed);
        _cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        MoveCursor(-1000, -1000);
        Gum.Forms.FormsUtilities.SetCursor(_cursor.Object);
    }

    private void MoveCursor(float x, float y)
    {
        _cursor.Setup(c => c.X).Returns((int)x);
        _cursor.Setup(c => c.Y).Returns((int)y);
        _cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        _cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
    }

    // Runs one frame of the real focus dispatch with only this key down (pushed, typed and held).
    private static void Press(Keys key)
    {
        Mock<IInputReceiverKeyboardMonoGame> keyboard = new();
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyDown(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyPushed(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeyTyped(key)).Returns(true);
        keyboard.As<IInputReceiverKeyboard>().Setup(k => k.KeysTyped).Returns(new List<Keys> { key });
        FrameworkElement.KeyboardsForUiControl.Add(keyboard.Object);
        try
        {
            Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
        }
        finally
        {
            FrameworkElement.KeyboardsForUiControl.Remove(keyboard.Object);
        }
    }

    private static void ShouldBeInView(ScrollViewer popup, MenuItem row)
    {
        row.IsHighlighted.ShouldBeTrue($"{row.Header} should be highlighted");
        GraphicalUiElement clip = (GraphicalUiElement)popup.InnerPanel.Parent!;
        row.Visual.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 0.01f, row.Header);
        row.Visual.GetAbsoluteBottom().ShouldBeLessThanOrEqualTo(clip.AbsoluteBottom + 0.01f, row.Header);
    }
    private static MenuItem CreateMenu(float canvasWidth, float canvasHeight, float x = 0)
    {
        ResizeCanvas(canvasWidth, canvasHeight);
        ContainerRuntime container = new()
        {
            X = x,
            Width = 400,
            WidthUnits = DimensionUnitType.Absolute,
            Height = 40,
            HeightUnits = DimensionUnitType.Absolute
        };
        Gum.GumService.Default.Root.Children!.Add(container);
        Menu menu = new();
        container.Children!.Add(menu.Visual);
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        container.UpdateLayout();
        return file;
    }

    private static void ResizeCanvas(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        Gum.Forms.FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    private static void Tick()
    {
        Gum.GumService.Default.Root.AnimateSelf(0.016);
        FrameworkElement.PopupRoot.AnimateSelf(0.016);
    }

    private static ScrollViewer PopupOf(MenuItem owner)
    {
        foreach (GraphicalUiElement popup in FrameworkElement.PopupRoot.Children!)
        {
            if (popup is InteractiveGue gue && gue.FormsControlAsObject is ScrollViewer sv &&
                sv.InnerPanel.Children!.Count > 0 &&
                (sv.InnerPanel.Children[0] as InteractiveGue)?.FormsControlAsObject is MenuItem m &&
                m.ParentMenuItem == owner)
            {
                return sv;
            }
        }
        throw new System.InvalidOperationException("No open popup for " + owner.Header);
    }

    private static void AddRows(MenuItem owner, int count)
    {
        for (int i = 0; i < count; i++)
        {
            owner.Items!.Add("Item " + i);
        }
    }

    // The popup fills the canvas height, its rows are not cut off at the side, and scrolling to the end
    // shows the last row inside the clipped area.
    private static void ShouldBeClampedAndScrollable(ScrollViewer popup, float canvasWidth, float canvasHeight)
    {
        popup.Visual.AbsoluteTop.ShouldBe(0, 0.01f);
        popup.Visual.AbsoluteBottom.ShouldBe(canvasHeight, 0.01f);
        popup.Visual.AbsoluteRight.ShouldBeLessThanOrEqualTo(canvasWidth + 0.01f);
        popup.VerticalScrollBar!.IsVisible.ShouldBeTrue();

        GraphicalUiElement clip = (GraphicalUiElement)popup.InnerPanel.Parent!;
        clip.AbsoluteWidth.ShouldBeGreaterThanOrEqualTo(popup.InnerPanel.AbsoluteWidth - 0.01f);
        clip.AbsoluteBottom.ShouldBeLessThanOrEqualTo(canvasHeight + 0.01f);

        popup.VerticalScrollBarValue = popup.VerticalScrollBarMaximum;
        popup.VerticalScrollBarMaximum.ShouldBeGreaterThan(0);
        GraphicalUiElement lastRow = popup.InnerPanel.Children![^1];
        lastRow.GetAbsoluteBottom().ShouldBeLessThanOrEqualTo(clip.AbsoluteBottom + 0.01f);
        lastRow.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 0.01f);
    }

    private static void ShouldBeSizedToRows(ScrollViewer popup)
    {
        popup.Visual.HeightUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
        popup.Visual.WidthUnits.ShouldBe(DimensionUnitType.RelativeToChildren);
        popup.VerticalScrollBar!.IsVisible.ShouldBeFalse();
        popup.VerticalScrollBarValue.ShouldBe(0);
        popup.Visual.AbsoluteHeight.ShouldBeGreaterThanOrEqualTo(popup.InnerPanel.AbsoluteHeight);
    }

    [Fact]
    public void Open_ShouldClampToCanvasAndScroll_WhenSubmenuIsTallerThanTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 200);

        file.IsSelected = true;

        ShouldBeClampedAndScrollable(PopupOf(file), 800, 600);
    }

    [Fact]
    public void ItemsAdd_ShouldClampToCanvasAndScroll_WhenAppendsMakeTheOpenSubmenuTallerThanTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        file.Items!.Add("Start");
        file.IsSelected = true;

        AddRows(file, 200);

        ShouldBeClampedAndScrollable(PopupOf(file), 800, 600);
    }

    [Fact]
    public void Open_ShouldClampToCanvasAndScroll_WhenNestedSubmenuIsTallerThanTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        MenuItem recent = new() { Header = "Recent" };
        file.Items!.Add(recent);
        AddRows(recent, 200);
        file.IsSelected = true;

        recent.IsSelected = true;

        ShouldBeClampedAndScrollable(PopupOf(recent), 800, 600);
        PopupOf(recent).Visual.AbsoluteLeft.ShouldBeGreaterThanOrEqualTo(PopupOf(file).Visual.AbsoluteLeft);
    }

    [Fact]
    public void Tick_ShouldClampOpenSubmenu_WhenCanvasShrinksBelowItsHeight()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float naturalHeight = popup.Visual.AbsoluteHeight;
        naturalHeight.ShouldBeLessThan(600);

        ResizeCanvas(800, naturalHeight / 2);
        Tick();

        ShouldBeClampedAndScrollable(popup, 800, naturalHeight / 2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Tick_ShouldKeepOpenSubmenuInsideCanvas_WhenCanvasNarrows(bool appendAfterResize)
    {
        MenuItem file = CreateMenu(800, 600, x: 300);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float width = popup.Visual.AbsoluteWidth;
        popup.Visual.AbsoluteLeft.ShouldBe(300, 0.01f);

        ResizeCanvas(300 + width / 2, 600);
        Tick();
        if (appendAfterResize)
        {
            file.Items!.Add("Item 0");
        }

        popup.Visual.AbsoluteRight.ShouldBe(300 + width / 2, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(width, 0.01f);
    }

    [Fact]
    public void Tick_ShouldSizeSubmenuToRowsAgain_WhenCanvasGrowsPastItsHeight()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float naturalHeight = popup.Visual.AbsoluteHeight;
        float naturalWidth = popup.Visual.AbsoluteWidth;
        ResizeCanvas(800, naturalHeight / 2);
        Tick();

        ResizeCanvas(800, 600);
        Tick();

        ShouldBeSizedToRows(popup);
        popup.Visual.AbsoluteHeight.ShouldBe(naturalHeight, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(naturalWidth, 0.01f);
    }

    [Fact]
    public void ItemsRemove_ShouldSizeSubmenuToRowsAgain_WhenRemovalsMakeItFit()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        float naturalHeight = popup.Visual.AbsoluteHeight;
        float naturalWidth = popup.Visual.AbsoluteWidth;
        AddRows(file, 200);

        while (file.Items!.Count > 10)
        {
            file.Items.RemoveAt(file.Items.Count - 1);
        }

        ShouldBeSizedToRows(popup);
        popup.Visual.AbsoluteHeight.ShouldBe(naturalHeight, 0.01f);
        popup.Visual.AbsoluteWidth.ShouldBe(naturalWidth, 0.01f);
    }

    [Fact]
    public void Open_ShouldSizeSubmenuToRows_WhenItFitsTheCanvas()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 10);

        file.IsSelected = true;

        ScrollViewer popup = PopupOf(file);
        ShouldBeSizedToRows(popup);
        popup.Visual.AbsoluteHeight.ShouldBeLessThan(600);
        popup.VerticalScrollBarVisibility.ShouldBe(ScrollBarVisibility.Hidden);
    }

    [Fact]
    public void Arrows_ShouldScrollHighlightedRowIntoView_WhenSubmenuIsShrunk()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 200);
        ((Menu)file.ParentFrameworkElement!).IsFocused = true;
        Press(Keys.Down);
        ScrollViewer popup = PopupOf(file);
        IReadOnlyList<MenuItem> rows = file.OpenSubItems;
        ShouldBeInView(popup, rows[0]);

        // Up from the first row wraps to the last, far below the view.
        Press(Keys.Up);
        ShouldBeInView(popup, rows[^1]);

        // Down from the last row wraps to the first, above the view.
        Press(Keys.Down);
        ShouldBeInView(popup, rows[0]);
        popup.VerticalScrollBarValue.ShouldBe(0);

        for (int i = 0; i < 40; i++)
        {
            Press(Keys.Down);
        }
        ShouldBeInView(popup, rows[40]);
        popup.VerticalScrollBarValue.ShouldBeGreaterThan(0);

        for (int i = 0; i < 40; i++)
        {
            Press(Keys.Up);
        }
        ShouldBeInView(popup, rows[0]);
    }

    [Fact]
    public void MouseWheel_ShouldScrollShrunkSubmenu_WhenCursorIsOverIt()
    {
        MenuItem file = CreateMenu(800, 600);
        AddRows(file, 200);
        file.IsSelected = true;
        ScrollViewer popup = PopupOf(file);
        popup.VerticalScrollBarValue.ShouldBe(0);
        float firstRowTop = popup.InnerPanel.Children![0].GetAbsoluteTop();

        MoveCursor(popup.Visual.AbsoluteLeft + 5, popup.Visual.AbsoluteTop + 100);
        _cursor.Setup(c => c.ScrollWheelChange).Returns(-1);
        _cursor.Setup(c => c.ZVelocity).Returns(-1);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());

        popup.VerticalScrollBarValue.ShouldBeGreaterThan(0);
        popup.InnerPanel.Children![0].GetAbsoluteTop().ShouldBeLessThan(firstRowTop);
        file.IsPopupVisible.ShouldBeTrue();
    }
}
