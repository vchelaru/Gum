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

    // ---- Slider, Menu, ScrollViewer ----

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

    // ---- Code-only visual with states set up in code ----

    private static GraphicalUiElement NewStatefulVisual(GraphicalUiElement parent)
    {
        Gum.GueDeriving.ContainerRuntime visual = new Gum.GueDeriving.ContainerRuntime();
        Gum.DataTypes.Variables.StateSaveCategory category = new Gum.DataTypes.Variables.StateSaveCategory { Name = "Cat" };
        foreach ((string name, float width) in new[] { ("Off", 20f), ("On", 77f) })
        {
            Gum.DataTypes.Variables.StateSave state = new Gum.DataTypes.Variables.StateSave { Name = name };
            state.Variables.Add(new Gum.DataTypes.Variables.VariableSave
            {
                Name = "Width", Type = "float", Value = width, SetsValue = true
            });
            category.States.Add(state);
        }
        visual.Categories["Cat"] = category;
        foreach (Gum.DataTypes.Variables.StateSave state in category.States)
        {
            visual.States[state.Name] = state;
        }
        visual.Parent = parent;
        return visual;
    }

    [Fact]
    public void ApplyDiff_CodeOnlyVisualInACodeDefinedState_StaysInThatState()
    {
        GraphicalUiElement screen = LoadScreen();
        GraphicalUiElement visual = NewStatefulVisual(screen);
        visual.ApplyState("Cat", "On");

        Reload(screen, Second);

        visual.Width.ShouldBe(77f);
    }

    [Fact]
    public void ApplyDiff_CodeOnlyVisualChangesStateAfterReload_NewStateApplies()
    {
        GraphicalUiElement screen = LoadScreen();
        GraphicalUiElement visual = NewStatefulVisual(screen);
        visual.ApplyState("Cat", "On");

        Reload(screen, Second);
        visual.ApplyState("Cat", "Off");

        visual.Width.ShouldBe(20f);
    }

    [Fact]
    public void ApplyDiff_CodeOnlyVisualUnderAToolAuthoredPanelReloadedTwice_StaysInItsState()
    {
        GraphicalUiElement screen = LoadScreen();
        GraphicalUiElement panel = HotReloadFormsProjects.Child(screen, "Panel1");
        GraphicalUiElement visual = NewStatefulVisual(panel);
        visual.ApplyState("Cat", "On");

        Reload(screen, Second);
        Reload(screen, First);

        visual.Parent.ShouldBe(panel);
        visual.Width.ShouldBe(77f);
        visual.States.ContainsKey("On").ShouldBeTrue();
    }

    [Fact]
    public void ApplyDiff_CodeOnlyVisualStateDefinitionsAndCategory_AreNotRemovedByTheReload()
    {
        GraphicalUiElement screen = LoadScreen();
        GraphicalUiElement visual = NewStatefulVisual(screen);

        Reload(screen, Second);

        visual.Categories.ContainsKey("Cat").ShouldBeTrue();
        visual.States.ContainsKey("Off").ShouldBeTrue();
    }
}
