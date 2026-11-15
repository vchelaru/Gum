using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;
using Keys = Gum.Forms.Input.Keys;

namespace MonoGameGum.Tests.Forms;

/// <summary>Focus lifecycle on themed controls: hidden or removed, disabled, modal trap, ScrollViewer keys.</summary>
public class ThemeFocusLifecycleKeyboardTests : FileThemeInputTestBase
{
    private static FrameworkElement Named(GraphicalUiElement screen, string name)
    {
        List<FrameworkElement> all = new();
        CollectControls(screen, all);
        return all.First(c => c.Visual.Name == name);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void FocusedControl_Hidden_LosesFocus(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        FrameworkElement button = Named(screen, "DetectResolutionsButton");
        button.IsFocused = true;
        Driver.Step();

        button.Visual.Visible = false;
        Driver.Step();

        button.IsFocused.ShouldBeFalse(Label(theme));
        InteractiveGue.CurrentInputReceiver.ShouldBeNull(Label(theme));
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void FocusedControl_RemovedFromRoot_LosesFocus(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        FrameworkElement button = Named(screen, "DetectResolutionsButton");
        button.IsFocused = true;
        Driver.Step();

        screen.RemoveFromRoot();
        Driver.Step();

        button.IsFocused.ShouldBeFalse(Label(theme));
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Tab_SkipsDisabledControl(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        FrameworkElement check = Named(screen, "FullScreenCheckbox");
        FrameworkElement slider = Named(screen, "MusicSlider");
        check.IsEnabled = false;
        Named(screen, "DetectResolutionsButton").IsFocused = true;
        Driver.Step();

        Press(Keys.Tab);

        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(slider, Label(theme));
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Tab_VisitsDisabledControl_WhenItAllowsFocusWhenDisabled(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        FrameworkElement check = Named(screen, "FullScreenCheckbox");
        check.AllowFocusWhenDisabled = true;
        check.IsEnabled = false;
        Named(screen, "DetectResolutionsButton").IsFocused = true;
        Driver.Step();

        Press(Keys.Tab);

        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(check, Label(theme));
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Tab_TopMostModal_TrapsFocusAmongItsOwnControls(string theme)
    {
        GraphicalUiElement screen = LoadDemoScreen(theme);
        Button first = PlaceAlone<Button>(theme, "/ButtonStandard|/Button");
        Button second = PlaceAlone<Button>(theme, "/ButtonStandard|/Button");
        StackPanel modal = new();
        first.Visual.Parent = null;
        second.Visual.Parent = null;
        modal.AddChild(first);
        modal.AddChild(second);
        Gum.GumService.Default.ModalRoot.Children.Add(modal.Visual);
        Driver.Step(2);
        second.IsFocused = true;
        Driver.Step();

        Press(Keys.Tab);

        InteractiveGue.CurrentInputReceiver.ShouldBeSameAs(first, $"{Label(theme)}: Tab left the modal");
    }
}
