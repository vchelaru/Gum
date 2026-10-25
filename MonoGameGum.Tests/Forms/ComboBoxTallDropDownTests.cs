using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A ComboBox drop-down taller than the canvas is moved to the top and shrunk to the canvas height, so
/// its rows scroll; it goes back to its own height once it fits again, also when the canvas is resized
/// while it is open (#6769, the ComboBox side of #6758).
/// </summary>
public class ComboBoxTallDropDownTests : BaseTestClass
{
    public ComboBoxTallDropDownTests()
    {
        // A still cursor away from every control.
        Mock<ICursor> cursor = new();
        cursor.SetupProperty(c => c.VisualOver);
        cursor.SetupProperty(c => c.WindowPushed);
        cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        cursor.Setup(c => c.X).Returns(-1000);
        cursor.Setup(c => c.Y).Returns(-1000);
        cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(-1000);
        cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(-1000);
        FormsUtilities.SetCursor(cursor.Object);
    }

    public override void Dispose()
    {
        ComboBox.FromFileDropDownTemplate = null;
        base.Dispose();
    }

    #region Helpers

    private static void ResizeCanvas(float width, float height)
    {
        GraphicalUiElement.CanvasWidth = width;
        GraphicalUiElement.CanvasHeight = height;
        Gum.GumService.Default.Root.UpdateLayout();
        // GumService.Update sizes the popup root to the canvas each frame, before the roots animate.
        FormsUtilities.SetDimensionsToCanvas(FrameworkElement.PopupRoot);
        FrameworkElement.PopupRoot.UpdateLayout();
    }

    private static void Tick() => Gum.GumService.Default.Root.AnimateSelf(0.016);

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

    private static void AddRows(ComboBox comboBox, int count)
    {
        for (int i = 0; i < count; i++)
        {
            comboBox.Items!.Add("Item " + i);
        }
    }

    private static ComboBox CreateComboBox(float y, float dropDownHeight, int rows)
    {
        ComboBox comboBox = new();
        comboBox.AddToRoot();
        comboBox.Y = y;
        comboBox.ListBox.Visual.HeightUnits = DimensionUnitType.Absolute;
        comboBox.ListBox.Visual.Height = dropDownHeight;
        AddRows(comboBox, rows);
        return comboBox;
    }

    // A from-file ComboBox whose component has no ListBoxInstance child: its drop-down is built from
    // the lazy template on the first open (#6735).
    private static ComboBox CreateLazyComboBox(float y, float dropDownHeight, int rows)
    {
        ComboBox.FromFileDropDownTemplate = new VisualTemplate((_, _) =>
        {
            ListBoxVisual listBoxVisual = new(tryCreateFormsObject: false);
            listBoxVisual.HeightUnits = DimensionUnitType.Absolute;
            listBoxVisual.Height = dropDownHeight;
            return listBoxVisual;
        });
        ComboBoxVisual visual = new(tryCreateFormsObject: false);
        visual.Children.Remove(visual.GetGraphicalUiElementByName("ListBoxInstance")!);
        ComboBox comboBox = new(visual);
        comboBox.AddToRoot();
        comboBox.Y = y;
        AddRows(comboBox, rows);
        return comboBox;
    }

    private static void ShouldBeInView(ListBox listBox, int index)
    {
        GraphicalUiElement row = listBox.ListBoxItems[index].Visual;
        GraphicalUiElement clip = (GraphicalUiElement)listBox.InnerPanel.Parent!;
        row.GetAbsoluteTop().ShouldBeGreaterThanOrEqualTo(clip.AbsoluteTop - 0.01f, $"row {index}");
        row.GetAbsoluteBottom().ShouldBeLessThanOrEqualTo(clip.AbsoluteBottom + 0.01f, $"row {index}");
    }

    #endregion

    [Fact]
    public void Open_ShouldKeepPlacementAndHeight_WhenDropDownFitsBelow()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);

        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(comboBox.Visual.AbsoluteBottom, 0.01f);
        popup.AbsoluteHeight.ShouldBe(300, 0.01f);
        popup.AbsoluteWidth.ShouldBe(comboBox.Visual.AbsoluteWidth, 0.01f);
    }

    [Fact]
    public void Open_ShouldMoveWithoutShrinking_WhenDropDownFitsTheCanvasButNotBelow()
    {
        ResizeCanvas(800, 350);
        ComboBox comboBox = CreateComboBox(y: 200, dropDownHeight: 300, rows: 30);

        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteBottom.ShouldBe(350, 0.01f);
        popup.AbsoluteHeight.ShouldBe(300, 0.01f);
    }

    [Fact]
    public void Open_ShouldShrinkDropDownToCanvas_WhenTallerThanCanvas()
    {
        ResizeCanvas(800, 200);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);

        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(0, 0.01f);
        popup.AbsoluteHeight.ShouldBe(200, 0.01f);
        popup.AbsoluteWidth.ShouldBe(comboBox.Visual.AbsoluteWidth, 0.01f);
    }

    [Fact]
    public void Open_ShouldShrinkLazyDropDownToCanvas_WhenTallerThanCanvas()
    {
        ResizeCanvas(800, 200);
        ComboBox comboBox = CreateLazyComboBox(y: 20, dropDownHeight: 300, rows: 30);

        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(0, 0.01f);
        popup.AbsoluteHeight.ShouldBe(200, 0.01f);
    }

    [Fact]
    public void CanvasShrinksWhileOpen_ShouldShrinkDropDownToCanvas()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);
        comboBox.IsDropDownOpen = true;

        ResizeCanvas(800, 200);
        Tick();

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(0, 0.01f);
        popup.AbsoluteHeight.ShouldBe(200, 0.01f);
    }

    [Fact]
    public void CanvasShrinksWhileOpen_ShouldMoveDropDownInside_WhenItStillFitsTheCanvas()
    {
        ResizeCanvas(800, 600);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);
        comboBox.IsDropDownOpen = true;

        ResizeCanvas(800, 310);
        Tick();

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteBottom.ShouldBe(310, 0.01f);
        popup.AbsoluteHeight.ShouldBe(300, 0.01f);
    }

    [Fact]
    public void CanvasGrowsWhileOpen_ShouldRestoreHeightAndPlacement_WhenDropDownFitsAgain()
    {
        ResizeCanvas(800, 200);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);
        comboBox.IsDropDownOpen = true;
        comboBox.ListBox.VerticalScrollBarValue = 100;
        float offsetWhenShrunk = FirstRowOffset(comboBox.ListBox);

        ResizeCanvas(800, 600);
        Tick();

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(comboBox.Visual.AbsoluteBottom, 0.01f);
        popup.AbsoluteHeight.ShouldBe(300, 0.01f);
        // Unlike a submenu, the drop-down still scrolls at its own height, so it keeps its scroll position.
        comboBox.ListBox.VerticalScrollBarValue.ShouldBe(100, 0.01f);
        FirstRowOffset(comboBox.ListBox).ShouldBe(offsetWhenShrunk, 0.01f);
    }

    private static float FirstRowOffset(ListBox listBox) =>
        listBox.ListBoxItems[0].Visual.GetAbsoluteTop() - ((GraphicalUiElement)listBox.InnerPanel.Parent!).GetAbsoluteTop();

    [Fact]
    public void Reopen_ShouldUseThemeHeight_AfterBeingShrunk()
    {
        ResizeCanvas(800, 200);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);
        comboBox.IsDropDownOpen = true;
        comboBox.IsDropDownOpen = false;

        ResizeCanvas(800, 600);
        comboBox.IsDropDownOpen = true;

        GraphicalUiElement popup = comboBox.ListBox.Visual;
        popup.AbsoluteTop.ShouldBe(comboBox.Visual.AbsoluteBottom, 0.01f);
        popup.AbsoluteHeight.ShouldBe(300, 0.01f);
    }

    [Fact]
    public void Shrunk_ShouldScrollToEveryRow()
    {
        ResizeCanvas(800, 200);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);
        comboBox.IsDropDownOpen = true;

        comboBox.ListBox.VerticalScrollBarValue = float.MaxValue;

        ShouldBeInView(comboBox.ListBox, 29);
    }

    [Fact]
    public void Arrows_ShouldScrollSelectedRowIntoView_WhenDropDownIsShrunk()
    {
        ResizeCanvas(800, 200);
        ComboBox comboBox = CreateComboBox(y: 20, dropDownHeight: 300, rows: 30);
        comboBox.SelectedIndex = 0;
        comboBox.IsFocused = true;
        Press(Keys.Enter);
        comboBox.IsDropDownOpen.ShouldBeTrue();
        comboBox.ListBox.Visual.AbsoluteHeight.ShouldBe(200, 0.01f);

        for (int i = 0; i < 25; i++)
        {
            Press(Keys.Down);
        }

        comboBox.ListBox.SelectedIndex.ShouldBe(25);
        ShouldBeInView(comboBox.ListBox, 25);
    }
}
