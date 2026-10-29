using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Input;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// The Forms controls that react to a visual's RollOn and RollOff keep a correct hover state when the
/// cursor moves straight from one control to another in one frame (#7057). These drive the real
/// GumService.Update walk with a scripted cursor.
/// </summary>
public class RollOnRollOffFormsConsumerTests : BaseTestClass
{
    #region Helpers

    private Mock<ICursor> _cursor = null!;

    // Installs a scripted mouse cursor with no buttons down. Movement is non-zero so hover reacts.
    private void InstallCursor()
    {
        _cursor = new Mock<ICursor>();
        _cursor.SetupProperty(c => c.VisualOver);
        _cursor.SetupProperty(c => c.WindowPushed);
        _cursor.SetupProperty(c => c.CustomCursor);
        _cursor.Setup(c => c.LastInputDevice).Returns(InputDevice.Mouse);
        _cursor.Setup(c => c.XChange).Returns(1);
        _cursor.Setup(c => c.YChange).Returns(0);
        FormsUtilities.SetCursor(_cursor.Object);
    }

    private void MoveCursorTo(float x, float y)
    {
        _cursor.Setup(c => c.X).Returns((int)x);
        _cursor.Setup(c => c.Y).Returns((int)y);
        _cursor.Setup(c => c.XRespectingGumZoomAndBounds()).Returns(x);
        _cursor.Setup(c => c.YRespectingGumZoomAndBounds()).Returns(y);
        Gum.GumService.Default.Update(new Microsoft.Xna.Framework.GameTime());
    }

    private void MoveCursorOver(GraphicalUiElement visual)
    {
        MoveCursorTo(
            visual.GetAbsoluteX() + visual.AbsoluteWidth / 2,
            visual.GetAbsoluteY() + visual.AbsoluteHeight / 2);
    }

    // One primary push on the visual, then the release, then a quiet frame, as the real cursor reports them.
    private void PushAndReleaseOn(GraphicalUiElement visual)
    {
        _cursor.Setup(c => c.LastPrimaryPushTime).Returns(1);
        _cursor.Setup(c => c.PrimaryPush).Returns(true);
        _cursor.Setup(c => c.PrimaryDown).Returns(true);
        MoveCursorOver(visual);
        _cursor.Setup(c => c.PrimaryPush).Returns(false);
        _cursor.Setup(c => c.PrimaryDown).Returns(false);
        _cursor.Setup(c => c.PrimaryClick).Returns(true);
        MoveCursorOver(visual);
        _cursor.Setup(c => c.PrimaryClick).Returns(false);
        MoveCursorOver(visual);
    }

    private static T Place<T>(T control, float x, float width = 100, float height = 30)
        where T : FrameworkElement
    {
        control.AddToRoot();
        control.Visual.XOrigin = RenderingLibrary.Graphics.HorizontalAlignment.Left;
        control.Visual.XUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        control.Visual.YUnits = Gum.Converters.GeneralUnitType.PixelsFromSmall;
        control.Visual.X = x;
        control.Visual.Y = 0;
        control.Visual.WidthUnits = DimensionUnitType.Absolute;
        control.Visual.HeightUnits = DimensionUnitType.Absolute;
        control.Visual.Width = width;
        control.Visual.Height = height;
        control.Visual.UpdateLayout();
        return control;
    }

    // Records "<name> <state>" each time a control applies one of its hover related visual states.
    private static void RecordStates(FrameworkElement control, string name, List<string> log)
    {
        string[] states =
        {
            FrameworkElement.EnabledStateName,
            FrameworkElement.HighlightedStateName,
            FrameworkElement.PushedStateName,
        };
        foreach (string state in states)
        {
            string captured = state;
            control.GetState(captured).Apply = () => log.Add(name + " " + captured);
        }
    }

    #endregion

    #region Button

    [Fact]
    public void Button_ShouldEndHighlightedOnTheNewButtonOnly_WhenCursorMovesBetweenAdjacentButtons()
    {
        InstallCursor();
        Button buttonA = Place(new Button(), x: 0);
        Button buttonB = Place(new Button(), x: 100);
        List<string> states = new();
        RecordStates(buttonA, "A", states);
        RecordStates(buttonB, "B", states);
        MoveCursorOver(buttonA.Visual);
        states.Clear();

        MoveCursorOver(buttonB.Visual);

        _cursor.Object.VisualOver.ShouldBe(buttonB.Visual);
        states.ShouldContain("A " + FrameworkElement.EnabledStateName);
        states.ShouldContain("B " + FrameworkElement.HighlightedStateName);
    }

    [Fact]
    public void Button_ShouldApplyTheNewButtonsStateThenTheOldButtonsState_WhenCursorMovesBetweenAdjacentButtons()
    {
        InstallCursor();
        Button buttonA = Place(new Button(), x: 0);
        Button buttonB = Place(new Button(), x: 100);
        List<string> states = new();
        RecordStates(buttonA, "A", states);
        RecordStates(buttonB, "B", states);
        MoveCursorOver(buttonA.Visual);
        states.Clear();

        MoveCursorOver(buttonB.Visual);

        states.ShouldBe(new[] { "B Highlighted", "A Enabled" });
    }

    [Fact]
    public void Button_ShouldReturnToEnabled_WhenCursorLeavesAllButtons()
    {
        InstallCursor();
        Button buttonA = Place(new Button(), x: 0);
        List<string> states = new();
        RecordStates(buttonA, "A", states);
        MoveCursorOver(buttonA.Visual);
        states[^1].ShouldBe("A " + FrameworkElement.HighlightedStateName);

        MoveCursorTo(700, 500);

        states[^1].ShouldBe("A " + FrameworkElement.EnabledStateName);
    }

    #endregion

    #region ListBoxItem

    [Fact]
    public void ListBoxItem_ShouldHighlightOnlyTheNewItem_WhenCursorMovesBetweenAdjacentItems()
    {
        InstallCursor();
        ListBox listBox = Place(new ListBox(), x: 0, width: 200, height: 200);
        listBox.Items!.Add("Zero");
        listBox.Items.Add("One");
        listBox.Visual.UpdateLayout();
        ListBoxItem first = listBox.ListBoxItems[0];
        ListBoxItem second = listBox.ListBoxItems[1];
        MoveCursorOver(first.Visual);
        first.IsHighlighted.ShouldBeTrue();

        MoveCursorOver(second.Visual);

        first.IsHighlighted.ShouldBeFalse();
        second.IsHighlighted.ShouldBeTrue();
    }

    #endregion

    #region MenuItem

    [Fact]
    public void MenuItem_ShouldHighlightOnlyTheNewItem_WhenCursorMovesBetweenAdjacentTopLevelItems()
    {
        InstallCursor();
        (Menu menu, MenuItem file, MenuItem edit) = CreateMenu();
        MoveCursorOver(file.Visual);
        file.IsHighlighted.ShouldBeTrue();

        MoveCursorOver(edit.Visual);

        file.IsHighlighted.ShouldBeFalse();
        edit.IsHighlighted.ShouldBeTrue();
        menu.ShouldNotBeNull();
    }

    [Fact]
    public void MenuItem_ShouldSwitchTheOpenSubmenu_WhenCursorMovesToTheNextTopLevelItemWhileOpen()
    {
        InstallCursor();
        (Menu menu, MenuItem file, MenuItem edit) = CreateMenu();
        PushAndReleaseOn(file.Visual);
        file.IsPopupVisible.ShouldBeTrue("precondition: pushing the item opens its submenu");

        MoveCursorOver(edit.Visual);

        file.IsSelected.ShouldBeFalse();
        file.IsPopupVisible.ShouldBeFalse();
        edit.IsSelected.ShouldBeTrue();
        edit.IsPopupVisible.ShouldBeTrue();
        menu.ShouldNotBeNull();
    }

    private static (Menu Menu, MenuItem File, MenuItem Edit) CreateMenu()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = 400;
        container.Height = 40;
        Gum.GumService.Default.Root.Children!.Add(container);

        Menu menu = new();
        container.Children!.Add(menu.Visual);
        MenuItem file = new() { Header = "File" };
        menu.Items!.Add(file);
        file.Items!.Add(new MenuItem { Header = "New" });
        MenuItem edit = new() { Header = "Edit" };
        menu.Items.Add(edit);
        edit.Items!.Add(new MenuItem { Header = "Cut" });
        container.UpdateLayout();
        return (menu, file, edit);
    }

    #endregion

    #region ComboBox

    [Fact]
    public void ComboBox_ShouldReturnToEnabledAndTheNewButtonHighlight_WhenCursorMovesFromComboBoxToButton()
    {
        InstallCursor();
        ComboBox comboBox = Place(new ComboBox(), x: 0);
        Button button = Place(new Button(), x: 100);
        List<string> states = new();
        RecordStates(comboBox, "Combo", states);
        RecordStates(button, "Button", states);
        MoveCursorOver(comboBox.Visual);
        states[^1].ShouldBe("Combo " + FrameworkElement.HighlightedStateName);
        states.Clear();

        MoveCursorOver(button.Visual);

        states.ShouldContain("Combo " + FrameworkElement.EnabledStateName);
        states.ShouldContain("Button " + FrameworkElement.HighlightedStateName);
    }

    #endregion

    #region Splitter

    [Fact]
    public void Splitter_ShouldApplyItsCursorAndThenRestoreArrow_WhenCursorMovesFromSplitterToButton()
    {
        InstallCursor();
        Splitter splitter = Place(new Splitter(), x: 0, width: 10, height: 100);
        Button button = Place(new Button(), x: 10);
        MoveCursorOver(splitter.Visual);
        _cursor.Object.CustomCursor.ShouldNotBeNull();
        _cursor.Object.CustomCursor.ShouldNotBe(Cursors.Arrow);

        MoveCursorOver(button.Visual);

        _cursor.Object.CustomCursor.ShouldBe(Cursors.Arrow);
    }

    #endregion
}
