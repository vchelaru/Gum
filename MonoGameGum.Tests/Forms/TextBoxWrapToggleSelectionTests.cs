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

    private static GumProjectSave LoadCopy(string theme, TextWrapping? wrapping)
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
        Describe(live).ShouldBe(expected);
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

        Describe(live).ShouldBe(expected);
    }
}
