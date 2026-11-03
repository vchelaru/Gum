using Gum.DataTypes.Behaviors;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using GumRuntime;
using RenderingLibrary;
using System.Linq;
using Gum.Forms;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Wireframe;
using Shouldly;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Toggling TextWrapping on a live TextBox (what a Preview reload does through the property setter) must
/// leave selection highlights exactly where a TextBox created with that wrapping draws them.
/// </summary>
public class TextBoxWrapToggleSelectionTests : BaseTestClass
{
    private const string LongText =
        "alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau upsilon phi chi psi omega";

    private static TextBox Create(TextWrapping wrapping)
    {
        TextBox box = new();
        box.TextWrapping = wrapping;
        box.Height = 200;
        box.Width = 240;
        box.Visual.X = 100;
        box.Visual.Y = 80;
        box.Text = LongText;
        box.IsFocused = true;
        return box;
    }

    private static string Describe(TextBox box) =>
        string.Join(",", ((TextBoxBaseVisual)box.Visual).ClipContainer.Children.OfType<GraphicalUiElement>()
            .Where(c => c.Name == "SelectionInstance")
            .Select(r => $"{(r.Visible ? "v" : "h")}{r.AbsoluteTop:0.##}:{r.AbsoluteLeft:0.##}+{r.GetAbsoluteWidth():0.##}x{r.GetAbsoluteHeight():0.##}"));

    private static void Select(TextBox box)
    {
        box.SelectionStart = 20;
        box.SelectionLength = 60;
    }

    [Fact]
    public void DescribeSelectionGeometry_AfterSelecting_ListsInstancesLinesCaretClipAndOffset()
    {
        TextBox box = Create(TextWrapping.Wrap);
        System.Collections.Generic.List<string> callers = new();
        box.SelectionVisualsUpdated += callers.Add;

        Select(box);
        System.Collections.Generic.IReadOnlyList<string> lines = box.DescribeSelectionGeometry();

        lines.ShouldContain(l => l.StartsWith("sel[0] vis=True"));
        lines.ShouldContain(l => l.StartsWith("line[0] start=0 len="));
        lines.ShouldContain(l => l.StartsWith("caret x="));
        lines.ShouldContain(l => l.StartsWith("clip w="));
        lines.ShouldContain(l => l.StartsWith("selectionYOffset cached="));
        callers.ShouldNotBeEmpty();
        callers.ShouldAllBe(c => !string.IsNullOrEmpty(c) && c != "?");
    }

    [Fact]
    public void NoWrapToWrap_SelectionMadeAfterToggle_MatchesATextBoxCreatedWithWrap()
    {
        TextBox live = Create(TextWrapping.NoWrap);
        live.TextWrapping = TextWrapping.Wrap;
        TextBox fresh = Create(TextWrapping.Wrap);

        Select(live);
        Select(fresh);

        Describe(live).ShouldBe(Describe(fresh));
    }

    [Fact]
    public void NoWrapToWrap_SelectionExistingWhenToggled_MatchesATextBoxCreatedWithWrap()
    {
        TextBox live = Create(TextWrapping.NoWrap);
        Select(live);
        live.TextWrapping = TextWrapping.Wrap;
        TextBox fresh = Create(TextWrapping.Wrap);
        Select(fresh);

        Describe(live).ShouldBe(Describe(fresh));
    }

    [Fact]
    public void WrapToNoWrapToWrap_SelectionMatchesATextBoxCreatedWithWrap()
    {
        TextBox live = Create(TextWrapping.Wrap);
        Select(live);
        live.TextWrapping = TextWrapping.NoWrap;
        live.TextWrapping = TextWrapping.Wrap;
        live.SelectionLength = 0;
        Select(live);
        TextBox fresh = Create(TextWrapping.Wrap);
        Select(fresh);

        Describe(live).ShouldBe(Describe(fresh));
    }

    [Fact]
    public void WrapToNoWrap_SelectionMatchesATextBoxCreatedWithNoWrap()
    {
        TextBox live = Create(TextWrapping.Wrap);
        Select(live);
        live.TextWrapping = TextWrapping.NoWrap;
        TextBox fresh = Create(TextWrapping.NoWrap);
        Select(fresh);

        Describe(live).ShouldBe(Describe(fresh));
    }
}

/// <summary>
/// The same toggle through the real reload path: a TextBox instance of a theme whose TextWrapping the
/// tool edited. The selection must be drawn as a freshly loaded control draws it.
/// </summary>
public class TextBoxWrapToggleReloadSelectionTests : FileThemeInputTestBase
{
    private const string LongText =
        "alpha beta gamma delta epsilon zeta eta theta iota kappa lambda mu nu xi omicron pi rho sigma tau upsilon phi chi psi omega";

    private static GumProjectSave LoadCopy(string theme, TextWrapping? wrapping, string? lineMode = null, bool? acceptsReturn = null)
    {
        string gumx = System.IO.Path.Combine(RepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsThemes", theme, "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        project.ApplyAllVariableReferences();
        ComponentSave component = project.Components.First(c => c.Behaviors.Any(b => b.BehaviorName == StandardFormsBehaviorNames.TextBoxBehaviorName));
        ScreenSave screen = new ScreenSave { Name = "WrapScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Ctl1", BaseType = component.Name, ParentContainer = screen });
        state.Variables.Add(new VariableSave { Name = "Ctl1.Height", Type = "float", Value = 200f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Ctl1.HeightUnits", Type = "DimensionUnitType", Value = Gum.DataTypes.DimensionUnitType.Absolute, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Ctl1.Width", Type = "float", Value = 240f, SetsValue = true });
        state.Variables.Add(new VariableSave { Name = "Ctl1.WidthUnits", Type = "DimensionUnitType", Value = Gum.DataTypes.DimensionUnitType.Absolute, SetsValue = true });
        if (wrapping != null)
        {
            state.Variables.Add(new VariableSave { Name = "Ctl1.TextWrapping", Type = "TextWrapping", Value = wrapping.Value, SetsValue = true });
        }
        if (acceptsReturn != null)
        {
            state.Variables.Add(new VariableSave { Name = "Ctl1.AcceptsReturn", Type = "bool", Value = acceptsReturn.Value, SetsValue = true });
        }
        if (lineMode != null)
        {
            state.Variables.Add(new VariableSave { Name = "Ctl1.LineModeCategoryState", Type = "LineModeCategory", Value = lineMode, SetsValue = true });
        }
        project.Screens.Add(screen);
        project.Initialize();
        return project;
    }

    private static string RepoRoot()
    {
        string? directory = System.AppContext.BaseDirectory;
        while (directory != null && !System.IO.File.Exists(System.IO.Path.Combine(directory, "GumFull.sln")))
        {
            directory = System.IO.Path.GetDirectoryName(directory);
        }
        return directory!;
    }

    private static TextBox Build(GumProjectSave project, out GraphicalUiElement screen)
    {
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        StandardRuntimeRegistrations.Register();
        screen = project.Screens.Single(s => s.Name == "WrapScreen").ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        TextBox box = (TextBox)((InteractiveGue)screen.GetGraphicalUiElementByName("Ctl1")!).FormsControlAsObject!;
        box.Text = LongText;
        box.IsFocused = true;
        return box;
    }

    private static string Describe(TextBox box) =>
        string.Join(",", box.Visual.GetGraphicalUiElementByName("ClipContainer")!.Children.OfType<GraphicalUiElement>()
            .Where(c => c.Name == "SelectionInstance")
            .Select(r => $"{(r.Visible ? "v" : "h")}{r.Y:0.##}:{r.X:0.##}+{r.Width:0.##}x{r.Height:0.##}"));

    [Theory]
    [InlineData(TextWrapping.NoWrap, TextWrapping.Wrap)]
    [InlineData(TextWrapping.Wrap, TextWrapping.NoWrap)]
    public void Reload_TextWrappingToggled_SelectionMatchesAFreshLoad(TextWrapping from, TextWrapping to)
    {
        TextBox fresh = Build(LoadCopy("DarkPro", to), out _);
        fresh.SelectionStart = 20;
        fresh.SelectionLength = 60;
        string expected = Describe(fresh);

        TextBox live = Build(LoadCopy("DarkPro", from), out GraphicalUiElement screen);
        live.SelectionStart = 20;
        live.SelectionLength = 60;
        GumProjectSave reloaded = LoadCopy("DarkPro", to);
        ObjectFinder.Self.GumProjectSave = reloaded;
        GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);

        live.SelectionLength.ShouldBe(60);
        // Hidden leftovers of the old wrap mode are not compared: see the follow-up issue.
        VisibleOnly(Describe(live)).ShouldBe(VisibleOnly(expected));
    }

    [Theory]
    [InlineData(TextWrapping.NoWrap, TextWrapping.Wrap)]
    [InlineData(TextWrapping.Wrap, TextWrapping.NoWrap)]
    public void Reload_TextWrappingToggled_SelectionMadeAfterwardMatchesAFreshLoad(TextWrapping from, TextWrapping to)
    {
        TextBox fresh = Build(LoadCopy("DarkPro", to), out _);
        fresh.SelectionStart = 20;
        fresh.SelectionLength = 60;
        string expected = Describe(fresh);

        TextBox live = Build(LoadCopy("DarkPro", from), out GraphicalUiElement screen);
        GumProjectSave reloaded = LoadCopy("DarkPro", to);
        ObjectFinder.Self.GumProjectSave = reloaded;
        GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);
        live.SelectionStart = 20;
        live.SelectionLength = 60;

        // Hidden leftovers of the old wrap mode are not compared: see the follow-up issue.
        VisibleOnly(Describe(live)).ShouldBe(VisibleOnly(expected));
    }

    [Theory]
    [InlineData(TextWrapping.NoWrap, TextWrapping.Wrap, 1)]
    [InlineData(TextWrapping.NoWrap, TextWrapping.Wrap, 2)]
    [InlineData(TextWrapping.Wrap, TextWrapping.NoWrap, 1)]
    [InlineData(TextWrapping.Wrap, TextWrapping.NoWrap, 2)]
    public void Reload_ToggleThenSameReloadsAgain_SelectionStillMatchesAFreshLoad(TextWrapping from, TextWrapping to, int extraReloads)
    {
        TextBox fresh = Build(LoadCopy("DarkPro", to), out _);
        fresh.SelectionStart = 20;
        fresh.SelectionLength = 60;
        string expected = Describe(fresh);

        TextBox live = Build(LoadCopy("DarkPro", from), out GraphicalUiElement screen);
        live.SelectionStart = 20;
        live.SelectionLength = 60;
        for (int reload = 0; reload <= extraReloads; reload++)
        {
            GumProjectSave reloaded = LoadCopy("DarkPro", to);
            ObjectFinder.Self.GumProjectSave = reloaded;
            GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);
        }

        VisibleOnly(Describe(live)).ShouldBe(VisibleOnly(expected));
    }

    /// <summary>
    /// #7497: the tool saves a single-line box as Multi + Wrap; the selection made afterward (Shift+Up twice)
    /// must be drawn as a freshly loaded Multi + Wrap box draws it.
    /// </summary>
    [Theory]
    [InlineData(44, 30)]
    [InlineData(14, 60)]
    public void Reload_SingleLineToMultiLineWrap_SelectionMatchesAFreshLoad(int start, int length)
    {
        TextBox fresh = Build(LoadCopy("DarkPro", TextWrapping.Wrap, "Multi"), out _);
        TextBox live = Build(LoadCopy("DarkPro", null), out GraphicalUiElement screen);
        GumProjectSave reloaded = LoadCopy("DarkPro", TextWrapping.Wrap, "Multi");
        ObjectFinder.Self.GumProjectSave = reloaded;
        GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);

        foreach (TextBox box in new[] { fresh, live })
        {
            box.SelectionStart = 44;
            box.SelectionLength = 30;
            box.SelectionStart = start;
            box.SelectionLength = length;
        }

        VisibleOnly(Describe(live)).ShouldBe(VisibleOnly(Describe(fresh)));
    }

    /// <summary>
    /// #7497, absolute rects: after a real layout pass the highlights a reloaded-from-Single box computed
    /// (what is drawn) match a fresh Multi + Wrap load, not only the Y/Height properties.
    /// </summary>
    [Fact]
    public void Reload_SingleLineToMultiLineWrap_AbsoluteRectsMatchAFreshLoadAfterLayout()
    {
        TextBox fresh = Build(LoadCopy("DarkPro", TextWrapping.Wrap, "Multi"), out GraphicalUiElement freshScreen);
        TextBox live = Build(LoadCopy("DarkPro", null), out GraphicalUiElement screen);
        GumProjectSave reloaded = LoadCopy("DarkPro", TextWrapping.Wrap, "Multi");
        ObjectFinder.Self.GumProjectSave = reloaded;
        GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);

        foreach (TextBox box in new[] { fresh, live })
        {
            box.SelectionStart = 44;
            box.SelectionLength = 30;
            box.SelectionStart = 14;
            box.SelectionLength = 60;
        }
        freshScreen.UpdateLayout();
        screen.UpdateLayout();

        static string Rects(TextBox box) =>
            string.Join(",", box.Visual.GetGraphicalUiElementByName("ClipContainer")!.Children.OfType<GraphicalUiElement>()
                .Where(c => c.Name == "SelectionInstance" && c.Visible)
                .Select(r => $"{r.AbsoluteLeft:0.##},{r.AbsoluteTop:0.##},{r.GetAbsoluteWidth():0.##},{r.GetAbsoluteHeight():0.##}"));

        Rects(live).ShouldBe(Rects(fresh), string.Join(" | ", live.DescribeSelectionGeometry().Where(l => l.StartsWith("sel"))));
    }

    /// <summary>
    /// #7497 round trip: whatever line-mode states a box goes through by reload, the caret and the visible
    /// selection end up exactly where a box freshly loaded from the final file puts them. The general
    /// condition (caret): a placement only one line mode writes leaves its value behind on the next mode.
    /// General condition (state): a reload applies the Forms properties after the visual's variables, where a
    /// fresh load applies them before, so a property setter that derives a state (NoWrap -> Single) beat the
    /// file's explicit state (Multi) on reload but not on a restart. A step is "Wrapping:LineMode:Return".
    /// </summary>
    [Theory]
    [InlineData("DarkPro", "Wrap:Multi>NoWrap:")]
    [InlineData("DarkPro", "Wrap:Multi>NoWrap:Single")]
    [InlineData("DarkPro", "Wrap:Multi>NoWrap:Multi")]
    [InlineData("DarkPro", "NoWrap:Multi>:")]
    [InlineData("DarkPro", "NoWrap:MultiNoWrap>Wrap:Multi")]
    [InlineData("DarkPro", "Wrap:Multi>NoWrap:MultiNoWrap>Wrap:Multi")]
    [InlineData("DarkPro", ":Multi:Return>:Single:NoReturn")]
    [InlineData("DarkPro", ":Single:NoReturn>:Multi:Return")]
    [InlineData("DarkPro", "Wrap:Multi:Return>NoWrap:Multi:NoReturn")]
    [InlineData("Meadow", "Wrap:Multi>NoWrap:")]
    [InlineData("Meadow", "Wrap:Multi>NoWrap:Multi")]
    [InlineData("Meadow", "NoWrap:MultiNoWrap>Wrap:Multi")]
    [InlineData("Meadow", ":Multi:Return>:Single:NoReturn")]
    public void Reload_LineModeRoundTrip_CaretAndSelectionMatchAFreshLoadOfTheFinalFile(string theme, string chain)
    {
        static (TextWrapping? Wrapping, string? Mode, bool? Return) Parse(string step)
        {
            string[] parts = step.Split(':');
            return (parts[0].Length == 0 ? null : System.Enum.Parse<TextWrapping>(parts[0]),
                parts[1].Length == 0 ? null : parts[1],
                parts.Length < 3 || parts[2].Length == 0 ? null : parts[2] == "Return");
        }
        (TextWrapping? Wrapping, string? Mode, bool? Return)[] steps = chain.Split('>').Select(Parse).ToArray();
        (TextWrapping? Wrapping, string? Mode, bool? Return) last = steps[^1];

        TextBox fresh = Build(LoadCopy(theme, last.Wrapping, last.Mode, last.Return), out GraphicalUiElement freshScreen);
        TextBox live = Build(LoadCopy(theme, null), out GraphicalUiElement screen);
        foreach ((TextWrapping? wrapping, string? mode, bool? acceptsReturn) in steps)
        {
            GumProjectSave reloaded = LoadCopy(theme, wrapping, mode, acceptsReturn);
            ObjectFinder.Self.GumProjectSave = reloaded;
            GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);
        }

        foreach (TextBox box in new[] { fresh, live })
        {
            box.CaretIndex = 3;
            box.SelectionStart = 14;
            box.SelectionLength = 60;
        }
        freshScreen.UpdateLayout();
        screen.UpdateLayout();

        static string Describe(TextBox box)
        {
            GraphicalUiElement caret = box.Visual.GetGraphicalUiElementByName("CaretInstance")!;
            string selections = string.Join(";", box.Visual.GetGraphicalUiElementByName("ClipContainer")!.Children.OfType<GraphicalUiElement>()
                .Where(c => c.Name == "SelectionInstance" && c.Visible)
                .Select(r => $"{r.AbsoluteLeft:0.##},{r.AbsoluteTop:0.##},{r.GetAbsoluteWidth():0.##},{r.GetAbsoluteHeight():0.##} {r.YUnits}/{r.HeightUnits}"));
            return $"caret {caret.AbsoluteLeft:0.##},{caret.AbsoluteTop:0.##},{caret.GetAbsoluteHeight():0.##} | {selections}";
        }

        static string Units(TextBox box)
        {
            GraphicalUiElement caret = box.Visual.GetGraphicalUiElementByName("CaretInstance")!;
            GraphicalUiElement text = box.Visual.GetGraphicalUiElementByName("TextInstance")!;
            string Prop(string name) => typeof(TextBoxBase).GetProperty(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)?.GetValue(box)?.ToString() ?? "?";
            string Field(string name) => typeof(TextBoxBase).GetField(name, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(box)?.ToString() ?? "?";
            string internals = $"VAO={Prop("VerticalAlignmentOffset")} coreH={Prop("CoreTextObjectHeight")} top={Prop("UnrotatedTextTop")} lineH={Prop("ScaledLineHeightInPixels")} wrappedH={(text.RenderableComponent as RenderingLibrary.Graphics.Text)?.WrappedTextHeight} textAbsBottom={text.GetAbsoluteBottom()} textAbsTop={text.GetAbsoluteTop()} textH={text.GetAbsoluteHeight()} resting={Field("_textRestingY")} ";
            return internals + $"caret y={caret.Y} yu={caret.YUnits} yo={caret.YOrigin} parentH={caret.EffectiveParentGue?.AbsoluteHeight}; text y={text.Y} yu={text.YUnits} yo={text.YOrigin} h={text.Height} hu={text.HeightUnits} abs top={text.AbsoluteTop} va={(text.RenderableComponent as RenderingLibrary.Graphics.Text)?.VerticalAlignment}";
        }

        Describe(live).ShouldBe(Describe(fresh), "LIVE " + Units(live) + " || FRESH " + Units(fresh));
    }

    private static string VisibleOnly(string description) =>
        string.Join(",", description.Split(',').Where(part => part.StartsWith("v")));
}
