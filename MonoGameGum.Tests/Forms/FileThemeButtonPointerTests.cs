using Gum.Automation;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Pointer behavior of Button, CheckBox and RadioButton built from each packaged theme's files:
/// hover and press states, release outside, right click, double click, disabled and hidden.
/// </summary>
public class FileThemeButtonPointerTests : FileThemeInputTestBase
{
    private Button PlaceButton(string theme, out int clicks)
    {
        Button button = Place<Button>(LoadTheme(theme), "Button");
        int count = 0;
        button.Click += (_, _) => count++;
        clicks = 0;
        _counter = () => count;
        return button;
    }

    private System.Func<int> _counter = () => 0;

    [Theory]
    [MemberData(nameof(AllThemes))]
    public void Hover_Button_ShowsHighlightedThenClearsOnLeave(string theme)
    {
        Button button = PlaceButton(theme, out _);
        Driver.MoveTo(CenterX(button.Visual), CenterY(button.Visual));
        AppliedState(button.Visual, "ButtonCategory").ShouldBe("Highlighted", theme);

        Driver.MoveTo(OffX, OffY);
        AppliedState(button.Visual, "ButtonCategory").ShouldBe("Enabled", theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Press_Button_ShowsPushedAndReleaseOutsideDoesNotClick(string theme)
    {
        Button button = PlaceButton(theme, out _);
        Driver.MoveTo(CenterX(button.Visual), CenterY(button.Visual));
        Driver.SetButton(MouseButton.Primary, held: true);
        AppliedState(button.Visual, "ButtonCategory").ShouldBe("Pushed", theme);

        Driver.MoveTo(OffX, OffY);
        Driver.SetButton(MouseButton.Primary, held: false);

        _counter().ShouldBe(0, theme);
        AppliedState(button.Visual, "ButtonCategory").ShouldBe("Enabled", theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_Button_RaisesClickOnceAndReturnsToHighlighted(string theme)
    {
        Button button = PlaceButton(theme, out _);
        ClickCenter(button.Visual);
        _counter().ShouldBe(1, theme);
        AppliedState(button.Visual, "ButtonCategory").ShouldBe("Highlighted", theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void RightClick_Button_DoesNotRaiseClick(string theme)
    {
        Button button = PlaceButton(theme, out _);
        ClickCenter(button.Visual, MouseButton.Secondary);
        _counter().ShouldBe(0, theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void DoubleClick_Button_RaisesClickTwice(string theme)
    {
        Button button = PlaceButton(theme, out _);
        ClickCenter(button.Visual);
        ClickCenter(button.Visual);
        _counter().ShouldBe(2, theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Disabled_Button_IgnoresPointer(string theme)
    {
        Button button = PlaceButton(theme, out _);
        button.IsEnabled = false;
        Driver.Step();
        ClickCenter(button.Visual);
        _counter().ShouldBe(0, theme);
        AppliedState(button.Visual, "ButtonCategory").ShouldBe("Disabled", theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Hidden_Button_IgnoresPointer(string theme)
    {
        Button button = PlaceButton(theme, out _);
        int x = CenterX(button.Visual);
        int y = CenterY(button.Visual);
        button.IsVisible = false;
        Driver.Step();
        Driver.Click(x, y);
        _counter().ShouldBe(0, theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_CheckBox_TogglesAndRightClickDoesNot(string theme)
    {
        CheckBox box = Place<CheckBox>(LoadTheme(theme), "CheckBox");
        ClickCenter(box.Visual, MouseButton.Secondary);
        box.IsChecked.ShouldNotBe(true, theme + " right click toggled");

        ClickCenter(box.Visual);
        box.IsChecked.ShouldBe(true, theme);
        ClickCenter(box.Visual);
        box.IsChecked.ShouldNotBe(true, theme + " second click");
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Disabled_CheckBox_IgnoresPointer(string theme)
    {
        CheckBox box = Place<CheckBox>(LoadTheme(theme), "CheckBox");
        box.IsEnabled = false;
        Driver.Step();
        ClickCenter(box.Visual);
        box.IsChecked.ShouldNotBe(true, theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_RadioButton_ChecksAndStaysCheckedOnSecondClick(string theme)
    {
        RadioButton radio = Place<RadioButton>(LoadTheme(theme), "RadioButton");
        ClickCenter(radio.Visual, MouseButton.Secondary);
        radio.IsChecked.ShouldNotBe(true, theme + " right click");
        ClickCenter(radio.Visual);
        radio.IsChecked.ShouldBe(true, theme);
        ClickCenter(radio.Visual);
        radio.IsChecked.ShouldBe(true, theme + " second click");
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Overlap_TwoButtons_TopmostReceivesClick(string theme)
    {
        GumProjectSaveHolder holder = new GumProjectSaveHolder(LoadTheme(theme));
        Button bottom = Place<Button>(holder.Project, "Button");
        Button top = new Button();
        top.AddToRoot();
        top.X = bottom.Visual.AbsoluteLeft;
        top.Y = bottom.Visual.AbsoluteTop;
        top.Width = bottom.Visual.AbsoluteWidth;
        top.Height = bottom.Visual.AbsoluteHeight;
        Driver.Step(2);
        int bottomClicks = 0;
        int topClicks = 0;
        bottom.Click += (_, _) => bottomClicks++;
        top.Click += (_, _) => topClicks++;

        ClickCenter(bottom.Visual);

        topClicks.ShouldBe(1, theme);
        bottomClicks.ShouldBe(0, theme);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_ButtonScrolledOutsideScrollViewerClip_DoesNotClick(string theme)
    {
        GumProjectSaveHolder holder = new GumProjectSaveHolder(LoadTheme(theme));
        ScrollViewer viewer = Place<ScrollViewer>(holder.Project, "ScrollViewer");
        viewer.Width = 200;
        viewer.Height = 80;
        Button inside = new Button();
        inside.Visual.Y = 300;
        viewer.AddChild(inside);
        Driver.Step(3);
        int clicks = 0;
        inside.Click += (_, _) => clicks++;

        // The button's layout position is below the viewer's 80 pixel clip; the viewer ends at its bottom edge.
        int x = (int)(viewer.Visual.AbsoluteLeft + 20);
        int y = (int)(viewer.Visual.AbsoluteTop + viewer.Visual.AbsoluteHeight + 40);
        Driver.Click(x, y);

        clicks.ShouldBe(0, theme + " click below the clip reached the button; button at " + inside.Visual.AbsoluteTop);
    }

    [Theory]
    [MemberData(nameof(RepresentativeThemes))]
    public void Click_ButtonInsideNestedPanel_RaisesClick(string theme)
    {
        GumProjectSaveHolder holder = new GumProjectSaveHolder(LoadTheme(theme));
        Panel panel = Place<Panel>(holder.Project, "Panel");
        panel.Width = 200;
        panel.Height = 100;
        Button inside = new Button();
        panel.AddChild(inside);
        Driver.Step(3);
        int clicks = 0;
        inside.Click += (_, _) => clicks++;

        ClickCenter(inside.Visual);

        clicks.ShouldBe(1, theme);
    }

    private sealed class GumProjectSaveHolder
    {
        public Gum.DataTypes.GumProjectSave Project { get; }
        public GumProjectSaveHolder(Gum.DataTypes.GumProjectSave project) => Project = project;
    }
}
