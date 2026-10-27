using Gum.Managers;
using Gum;
using Gum.DataTypes;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep B: default-theme Forms controls created in code under a tool-authored screen. A reload
/// has to leave what they hold (text, caret, selection, items, values, checked) alone, and the
/// control's next state change has to keep working afterwards.
/// </summary>
public class HotReloadSweepBCodeControlsTests : BaseTestClass
{
    private static readonly HotReloadFormsProjects.Look First = HotReloadFormsProjects.First;
    private static readonly HotReloadFormsProjects.Look Second = HotReloadFormsProjects.Second;

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static GraphicalUiElement LoadScreen() =>
        HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(First));

    private static T AddToScreen<T>(GraphicalUiElement screen, T control) where T : FrameworkElement
    {
        control.Visual.Parent = screen;
        return control;
    }

    private static void Reload(GraphicalUiElement screen, HotReloadFormsProjects.Look look) =>
        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(look));

    private static void Hover(FrameworkElement control, bool over)
    {
        FrameworkElement.MainCursor.VisualOver = over ? (InteractiveGue)control.Visual : null;
        control.UpdateState();
    }

    // ---- TextBox ----

    [Fact]
    public void ApplyDiff_TextBoxWithTextCaretAndSelection_AllSurvive()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = AddToScreen(screen, new TextBox());
        textBox.Text = "hello world";
        textBox.IsFocused = true;
        textBox.CaretIndex = 6;
        textBox.SelectionLength = 3;

        Reload(screen, Second);

        textBox.Text.ShouldBe("hello world");
        textBox.CaretIndex.ShouldBe(6);
        textBox.SelectionLength.ShouldBe(3);
    }

    [Fact]
    public void ApplyDiff_TextBoxFocused_StaysFocused()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = AddToScreen(screen, new TextBox());
        textBox.IsFocused = true;

        Reload(screen, Second);

        textBox.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void ApplyDiff_TextBoxThenTypingContinues_InsertsAtTheSavedCaret()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = AddToScreen(screen, new TextBox());
        textBox.Text = "abcd";
        textBox.IsFocused = true;
        textBox.CaretIndex = 2;

        Reload(screen, Second);
        textBox.HandleCharEntered('X');

        textBox.Text.ShouldBe("abXcd");
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void ApplyDiff_TextBoxReloadedTwice_KeepsTextAndCaret()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = AddToScreen(screen, new TextBox());
        textBox.Text = "twice";
        textBox.IsFocused = true;
        textBox.CaretIndex = 3;

        Reload(screen, Second);
        Reload(screen, First);

        textBox.Text.ShouldBe("twice");
        textBox.CaretIndex.ShouldBe(3);
    }

    [Fact]
    public void ApplyDiff_TextBoxPlaceholderAndEmptyText_KeepsThePlaceholder()
    {
        GraphicalUiElement screen = LoadScreen();
        TextBox textBox = AddToScreen(screen, new TextBox());
        textBox.Placeholder = "type here";

        Reload(screen, Second);

        textBox.Text.ShouldBeNullOrEmpty();
        textBox.Placeholder.ShouldBe("type here");
    }

    // ---- ListBox ----

    private static ListBox NewListBox(GraphicalUiElement screen, int itemCount)
    {
        ListBox listBox = AddToScreen(screen, new ListBox());
        for (int i = 0; i < itemCount; i++)
        {
            listBox.Items!.Add("Item" + i);
        }
        return listBox;
    }

    [Fact]
    public void ApplyDiff_ListBoxWithItems_ItemsAreNotDuplicatedOrLost()
    {
        GraphicalUiElement screen = LoadScreen();
        ListBox listBox = NewListBox(screen, 4);
        int visualCount = listBox.InnerPanel.Children.Count;

        Reload(screen, Second);

        listBox.Items!.Count.ShouldBe(4);
        listBox.InnerPanel.Children.Count.ShouldBe(visualCount);
    }

    [Fact]
    public void ApplyDiff_ListBoxWithSelection_KeepsTheSelection()
    {
        GraphicalUiElement screen = LoadScreen();
        ListBox listBox = NewListBox(screen, 4);
        listBox.SelectedIndex = 2;

        Reload(screen, Second);

        listBox.SelectedIndex.ShouldBe(2);
        listBox.SelectedObject.ShouldBe("Item2");
    }

    [Fact]
    public void ApplyDiff_ListBoxThenItemAddedAfterReload_ItemIsCreatedFromTheTemplate()
    {
        GraphicalUiElement screen = LoadScreen();
        ListBox listBox = NewListBox(screen, 2);
        int before = listBox.InnerPanel.Children.Count;

        Reload(screen, Second);
        listBox.Items!.Add("Late");

        listBox.Items.Count.ShouldBe(3);
        listBox.InnerPanel.Children.Count.ShouldBe(before + 1);
    }

    [Fact]
    public void ApplyDiff_ListBoxThenSelectionChangesAfterReload_SelectionFollows()
    {
        GraphicalUiElement screen = LoadScreen();
        ListBox listBox = NewListBox(screen, 3);
        listBox.SelectedIndex = 0;

        Reload(screen, Second);
        listBox.SelectedIndex = 1;

        listBox.SelectedObject.ShouldBe("Item1");
    }

    // ---- ComboBox, Slider, Menu, ScrollViewer ----

    [Fact]
    public void ApplyDiff_ComboBoxWithSelection_KeepsItemsAndSelection()
    {
        GraphicalUiElement screen = LoadScreen();
        ComboBox combo = AddToScreen(screen, new ComboBox());
        combo.Items!.Add("A");
        combo.Items.Add("B");
        combo.SelectedIndex = 1;

        Reload(screen, Second);

        combo.Items.Count.ShouldBe(2);
        combo.SelectedIndex.ShouldBe(1);
        combo.SelectedObject.ShouldBe("B");
    }

    [Fact]
    public void ApplyDiff_SliderValue_SurvivesAndStillMoves()
    {
        GraphicalUiElement screen = LoadScreen();
        Slider slider = AddToScreen(screen, new Slider());
        slider.Minimum = 0;
        slider.Maximum = 100;
        slider.Value = 35;

        Reload(screen, Second);
        slider.Value.ShouldBe(35);
        slider.Value = 80;

        slider.Value.ShouldBe(80);
    }

    [Fact]
    public void ApplyDiff_MenuWithItems_ItemsSurviveWithoutDuplicates()
    {
        GraphicalUiElement screen = LoadScreen();
        Menu menu = AddToScreen(screen, new Menu());
        menu.Items!.Add(new MenuItem { Header = "File" });
        menu.Items.Add(new MenuItem { Header = "Edit" });
        int visualCount = menu.Visual.Children.Count;

        Reload(screen, Second);

        menu.Items.Count.ShouldBe(2);
        menu.Visual.Children.Count.ShouldBe(visualCount);
    }

    [Fact]
    public void ApplyDiff_ScrollViewerScrolled_KeepsTheOffset()
    {
        GraphicalUiElement screen = LoadScreen();
        ScrollViewer viewer = AddToScreen(screen, new ScrollViewer());
        viewer.Width = 100;
        viewer.Height = 100;
        for (int i = 0; i < 10; i++)
        {
            Button child = new Button();
            child.Height = 50;
            viewer.AddChild(child);
        }
        viewer.VerticalScrollBarValue = 120;
        double before = viewer.VerticalScrollBarValue;
        before.ShouldBeGreaterThan(0, "sanity");

        Reload(screen, Second);

        viewer.VerticalScrollBarValue.ShouldBe(before);
    }

    // ---- Checked controls ----

    [Fact]
    public void ApplyDiff_CheckBoxCheckedInCode_StaysCheckedAndTogglesAfterReload()
    {
        GraphicalUiElement screen = LoadScreen();
        CheckBox check = AddToScreen(screen, new CheckBox());
        check.IsChecked = true;

        Reload(screen, Second);
        check.IsChecked.ShouldBe(true);
        check.IsChecked = false;

        check.IsChecked.ShouldBe(false);
    }

    [Fact]
    public void ApplyDiff_RadioButtonChecked_StaysChecked()
    {
        GraphicalUiElement screen = LoadScreen();
        RadioButton radio = AddToScreen(screen, new RadioButton());
        radio.IsChecked = true;

        Reload(screen, Second);

        radio.IsChecked.ShouldBe(true);
    }

    [Fact]
    public void ApplyDiff_ToggleButtonChecked_StaysChecked()
    {
        GraphicalUiElement screen = LoadScreen();
        ToggleButton toggle = AddToScreen(screen, new ToggleButton());
        toggle.IsChecked = true;

        Reload(screen, Second);

        toggle.IsChecked.ShouldBe(true);
    }

    // ---- Button states after a reload ----

    [Fact]
    public void ApplyDiff_CodeButtonDisabled_ReEnableReturnsToTheEnabledLook()
    {
        GraphicalUiElement screen = LoadScreen();
        Button button = AddToScreen(screen, new Button());
        ButtonVisual visual = (ButtonVisual)button.Visual;
        var enabledColor = visual.Background.Color;
        button.IsEnabled = false;
        var disabledColor = visual.Background.Color;
        disabledColor.ShouldNotBe(enabledColor, "sanity");

        Reload(screen, Second);
        visual.Background.Color.ShouldBe(disabledColor);
        button.IsEnabled = true;

        visual.Background.Color.ShouldBe(enabledColor);
    }

    [Fact]
    public void ApplyDiff_CodeButtonHoveredAfterReload_HoverLooksTheSameAndLeavingRestores()
    {
        GraphicalUiElement screen = LoadScreen();
        Button button = AddToScreen(screen, new Button());
        ButtonVisual visual = (ButtonVisual)button.Visual;
        var enabledColor = visual.Background.Color;
        Hover(button, true);
        var hoverColor = visual.Background.Color;
        hoverColor.ShouldNotBe(enabledColor, "sanity");
        Hover(button, false);

        Reload(screen, Second);
        Hover(button, true);
        visual.Background.Color.ShouldBe(hoverColor);
        Hover(button, false);

        visual.Background.Color.ShouldBe(enabledColor);
    }

    [Fact]
    public void ApplyDiff_CodeButtonHoveredWhenReloaded_StillLooksHoveredThenReleases()
    {
        GraphicalUiElement screen = LoadScreen();
        Button button = AddToScreen(screen, new Button());
        ButtonVisual visual = (ButtonVisual)button.Visual;
        var enabledColor = visual.Background.Color;
        Hover(button, true);
        var hoverColor = visual.Background.Color;

        Reload(screen, Second);
        visual.Background.Color.ShouldBe(hoverColor);
        Hover(button, false);

        visual.Background.Color.ShouldBe(enabledColor);
    }

    [Fact]
    public void ApplyDiff_CodeButtonFocusedWhenReloaded_StaysFocused()
    {
        GraphicalUiElement screen = LoadScreen();
        Button button = AddToScreen(screen, new Button());
        button.IsFocused = true;

        Reload(screen, Second);

        button.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void ApplyDiff_CodeButtonDisabledAndFocusedWhenReloaded_StaysDisabledAndFocused()
    {
        GraphicalUiElement screen = LoadScreen();
        Button button = AddToScreen(screen, new Button());
        button.AllowFocusWhenDisabled = true;
        button.IsFocused = true;
        button.IsEnabled = false;
        ButtonVisual visual = (ButtonVisual)button.Visual;
        var look = visual.Background.Color;

        Reload(screen, Second);

        button.IsEnabled.ShouldBeFalse();
        button.IsFocused.ShouldBeTrue();
        visual.Background.Color.ShouldBe(look);
    }
}
