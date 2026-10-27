using Gum.Managers;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// #6878 sweep B, "what the runtime does next": after a reload has been applied, the control's next
/// state change, a second reload, code-added children and values a game set in code. The rule asserted
/// for code values: a value set in code and not authored in the file is not overwritten by a reload
/// (no explicit documented rule for plain visuals beyond "variables the file sets are re-applied").
/// </summary>
public class HotReloadSweepBNextStateTests : BaseTestClass
{
    private static readonly HotReloadFormsProjects.Look First = HotReloadFormsProjects.First;
    private static readonly HotReloadFormsProjects.Look Second = HotReloadFormsProjects.Second;
    private static readonly HotReloadFormsProjects.Look Third = HotReloadFormsProjects.First with
    {
        Default = 180, Highlighted = 190, Disabled = 200, Pushed = 210, Focused = 220, Enabled = 230, CheckedOn = 240
    };

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private static ICursor Cursor => FrameworkElement.MainCursor;

    private static GraphicalUiElement Load(HotReloadFormsProjects.Look look) =>
        HotReloadFormsProjects.Load(HotReloadFormsProjects.Build(look));

    private static void Reload(GraphicalUiElement screen, HotReloadFormsProjects.Look look) =>
        HotReloadFormsProjects.Reload(screen, HotReloadFormsProjects.Build(look));

    private static void Enter(string state, GraphicalUiElement buttonVisual)
    {
        Button button = HotReloadFormsProjects.Control<Button>(buttonVisual);
        InteractiveGue visual = (InteractiveGue)buttonVisual;
        switch (state)
        {
            case "Disabled":
                button.IsEnabled = false;
                break;
            case "Highlighted":
                Cursor.VisualOver = visual;
                button.UpdateState();
                break;
            case "Pushed":
                Cursor.VisualOver = visual;
                Cursor.WindowPushed = visual;
                Mock.Get(Cursor).Setup(c => c.PrimaryDown).Returns(true);
                button.UpdateState();
                break;
            case "Focused":
                button.IsFocused = true;
                break;
        }
    }

    private static void Leave(string state, GraphicalUiElement buttonVisual)
    {
        Button button = HotReloadFormsProjects.Control<Button>(buttonVisual);
        switch (state)
        {
            case "Disabled":
                button.IsEnabled = true;
                break;
            case "Highlighted":
                Cursor.VisualOver = null;
                button.UpdateState();
                break;
            case "Pushed":
                Mock.Get(Cursor).Setup(c => c.PrimaryDown).Returns(false);
                Cursor.WindowPushed = null;
                Cursor.VisualOver = null;
                button.UpdateState();
                break;
            case "Focused":
                button.IsFocused = false;
                break;
        }
    }

    private static int ExpectedRed(string state, HotReloadFormsProjects.Look look) => state switch
    {
        "Disabled" => look.Disabled,
        "Highlighted" => look.Highlighted,
        "Pushed" => look.Pushed,
        "Focused" => look.Focused,
        _ => look.Enabled
    };

    public static IEnumerable<object[]> NonDefaultStates() =>
        new[] { "Disabled", "Highlighted", "Pushed", "Focused" }.Select(s => new object[] { s });

    private static GraphicalUiElement Background(GraphicalUiElement screen, string button = "Button1") =>
        HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, button), "Background");

    // ---- Leaving the state after the reload ----

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_ReloadedInAStateThenLeftTheState_ShowsTheNewEnabledValues(string state)
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        Enter(state, button);

        Reload(screen, Second);
        Leave(state, button);

        HotReloadFormsProjects.Red(Background(screen)).ShouldBe(Second.Enabled);
    }

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_ReloadedTwiceInAState_ShowsTheLatestValuesOfThatState(string state)
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        Enter(state, button);

        Reload(screen, Second);
        Reload(screen, Third);

        HotReloadFormsProjects.Red(Background(screen)).ShouldBe(ExpectedRed(state, Third));
    }

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_TwoReloadsThenTheStateIsEntered_ShowsTheLatestValuesOfThatState(string state)
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");

        Reload(screen, Second);
        Reload(screen, Third);
        Enter(state, button);

        HotReloadFormsProjects.Red(Background(screen)).ShouldBe(ExpectedRed(state, Third));
    }

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_StateEnteredLeftAndReloadedAgain_ShowsTheEnabledValues(string state)
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        Reload(screen, Second);
        Enter(state, button);
        Leave(state, button);

        Reload(screen, Third);

        HotReloadFormsProjects.Red(Background(screen)).ShouldBe(Third.Enabled);
    }

    [Fact]
    public void ApplyDiff_DisabledButtonReloadedThenReEnabledThenHovered_UsesTheNewHighlightedValue()
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        Enter("Disabled", button);

        Reload(screen, Second);
        Leave("Disabled", button);
        Enter("Highlighted", button);

        HotReloadFormsProjects.Red(Background(screen)).ShouldBe(Second.Highlighted);
    }

    [Fact]
    public void ApplyDiff_CheckBoxUncheckedAfterReload_ShowsTheNewUncheckedValue()
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Check1");
        CheckBox check = HotReloadFormsProjects.Control<CheckBox>(visual);
        check.IsChecked = true;

        Reload(screen, Second);
        check.IsChecked = false;

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(visual, "Background")).ShouldBe(Second.Enabled);
    }

    [Fact]
    public void ApplyDiff_CheckBoxDisabledWhileCheckedThenReloaded_ShowsTheNewDisabledCheckedValue()
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Check1");
        CheckBox check = HotReloadFormsProjects.Control<CheckBox>(visual);
        check.IsChecked = true;
        check.IsEnabled = false;

        Reload(screen, Second);

        check.IsChecked.ShouldBe(true);
        check.IsEnabled.ShouldBeFalse();
        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(visual, "Background")).ShouldBe(Second.Disabled);
    }

    // ---- Nested components ----

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_NestedButtonInAStateThenLeftTheState_ShowsTheNewEnabledValues(string state)
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement nested = HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(screen, "Panel1"), "Button1");
        Enter(state, nested);

        Reload(screen, Second);
        Leave(state, nested);

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(nested, "Background")).ShouldBe(Second.Enabled);
    }

    // ---- Runtime-added children and values set in code ----

    [Fact]
    public void ApplyDiff_ButtonAddedInCodeUnderAToolAuthoredPanel_StaysAndTheAuthoredButtonUpdates()
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement panel = HotReloadFormsProjects.Child(screen, "Panel1");
        Button added = new Button();
        added.Visual.Parent = panel;
        int childCount = panel.Children.Count;

        Reload(screen, Second);

        added.Visual.Parent.ShouldBe(panel);
        panel.Children.Count.ShouldBe(childCount);
        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(HotReloadFormsProjects.Child(panel, "Button1"), "Background"))
            .ShouldBe(Second.Enabled);
    }

    [Fact]
    public void ApplyDiff_ButtonAddedInCodeUnderAPanelThenReloadedTwice_IsNotDuplicated()
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement panel = HotReloadFormsProjects.Child(screen, "Panel1");
        Button added = new Button();
        added.Visual.Parent = panel;
        int childCount = panel.Children.Count;

        Reload(screen, Second);
        Reload(screen, Third);

        panel.Children.Count.ShouldBe(childCount);
        panel.Children.Count(c => c == added.Visual).ShouldBe(1);
    }

    [Fact]
    public void ApplyDiff_ButtonAddedInCodeThenHoveredAfterReload_HoverStillWorks()
    {
        GraphicalUiElement screen = Load(First);
        Button added = new Button();
        added.Visual.Parent = screen;
        var enabledColor = ((Gum.Forms.DefaultVisuals.V3.ButtonVisual)added.Visual).Background.Color;
        Cursor.VisualOver = (InteractiveGue)added.Visual;
        added.UpdateState();
        var hoverColor = ((Gum.Forms.DefaultVisuals.V3.ButtonVisual)added.Visual).Background.Color;

        Reload(screen, Second);
        Cursor.VisualOver = null;
        added.UpdateState();

        ((Gum.Forms.DefaultVisuals.V3.ButtonVisual)added.Visual).Background.Color.ShouldBe(enabledColor);
        hoverColor.ShouldNotBe(enabledColor);
    }

    [Fact]
    public void ApplyDiff_UnrelatedVariableSetInCodeOnAPlainVisual_IsNotOverwritten()
    {
        // No documented rule for a variable the file never sets; chosen: code keeps it.
        GraphicalUiElement screen = Load(First);
#pragma warning disable CS0618 // the fixture instantiates the legacy ColoredRectangle standard
        Gum.GueDeriving.ColoredRectangleRuntime plain = (Gum.GueDeriving.ColoredRectangleRuntime)HotReloadFormsProjects.Child(screen, "Plain1");
#pragma warning restore CS0618
        plain.Alpha = 77;
        plain.Width = 123;

        Reload(screen, Second);

        plain.Alpha.ShouldBe(77);
        plain.Width.ShouldBe(123);
        HotReloadFormsProjects.Red(plain).ShouldBe(Second.Default);
    }

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_TextSetInCodeWhileInAStateAndReloadedTwice_KeepsTheCodeText(string state)
    {
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement visual = HotReloadFormsProjects.Child(screen, "Button1");
        Button button = HotReloadFormsProjects.Control<Button>(visual);
        button.Text = "FromCode";
        Enter(state, visual);

        Reload(screen, Second);
        Reload(screen, Third);

        button.Text.ShouldBe("FromCode");
    }

    // ---- Preview: a selected screen state applied after the reload ----

    [Theory]
    [MemberData(nameof(NonDefaultStates))]
    public void ApplyDiff_ScreenStateAppliedAfterTheReloadLikeThePreviewDoes_DoesNotDisturbTheButton(string state)
    {
        // Tool/GumPreview/Game1.cs ApplySelectedState is private and has no test seam; this mirrors it
        // through the shared GraphicalUiElement.ApplyState it calls.
        GraphicalUiElement screen = Load(First);
        GraphicalUiElement button = HotReloadFormsProjects.Child(screen, "Button1");
        Enter(state, button);
        GumProjectSave project = HotReloadFormsProjects.Build(Second);
        HotReloadFormsProjects.Reload(screen, project);
        StateSave selected = project.Screens.Single(s => s.Name == "TestScreen")
            .Categories.Single(c => c.Name == "MyCategory").States.Single(s => s.Name == "B");

        screen.ApplyState(selected);

        HotReloadFormsProjects.Red(HotReloadFormsProjects.Child(screen, "Plain1")).ShouldBe(Second.Highlighted);
        HotReloadFormsProjects.Red(Background(screen)).ShouldBe(ExpectedRed(state, Second));
    }
}
