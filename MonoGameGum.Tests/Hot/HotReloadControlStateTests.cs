using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.Tests.Forms;
using RenderingLibrary;
using Shouldly;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Hot;

/// <summary>
/// MenuItem, Expander and ListBoxItem after a reload (ToolTip, IsEnabled edited and removed) look like ones a restart builds, and an Expander enabled again in code looks like a fresh one (#7473).
/// </summary>
public class HotReloadControlStateTests : FileThemeInputTestBase
{
    private static string RepoRoot()
    {
        string? directory = System.AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "GumFull.sln")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        return directory!;
    }

    private static GumProjectSave LoadCopy(string? toolTip, string behavior = StandardFormsBehaviorNames.MenuItemBehaviorName, bool? isEnabled = null)
    {
        string gumx = Path.Combine(RepoRoot(), "Tools", "Gum.ProjectServices", "Templates", "FormsTemplate", "GumProject.gumx");
        GumProjectSave project = GumProjectSave.Load(gumx, out _)!;
        project.Initialize();
        ObjectFinder.Self.GumProjectSave = project;
        project.ApplyAllVariableReferences();
        ComponentSave component = project.Components.First(c =>
            c.Behaviors.Any(b => b.BehaviorName == behavior));
        ScreenSave screen = new ScreenSave { Name = "ItemScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Ctl1", BaseType = component.Name, ParentContainer = screen });
        if (toolTip != null)
        {
            state.Variables.Add(new VariableSave { Name = "Ctl1.ToolTip", Type = "string", Value = toolTip, SetsValue = true });
        }
        if (isEnabled != null)
        {
            state.Variables.Add(new VariableSave { Name = "Ctl1.IsEnabled", Type = "bool", Value = isEnabled.Value, SetsValue = true });
        }
        project.Screens.Add(screen);
        project.Initialize();
        return project;
    }

    private static GraphicalUiElement Build(GumProjectSave project)
    {
        ObjectFinder.Self.GumProjectSave = project;
        Gum.Forms.FormsUtilities.RegisterFromFileFormRuntimeDefaults();
        StandardRuntimeRegistrations.Register();
        return project.Screens.Single(s => s.Name == "ItemScreen").ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
    }

    private static string Describe(GraphicalUiElement element, int depth = 0)
    {
        StringBuilder text = new StringBuilder();
        text.Append(' ', depth * 2).Append(element.Name).Append(" vis=").Append(element.Visible);
        if (element is Gum.GueDeriving.TextRuntime t)
        {
            text.Append(" rgb=").Append(t.Red).Append(',').Append(t.Green).Append(',').Append(t.Blue);
        }
        else if (element is Gum.GueDeriving.ColoredRectangleRuntime c)
        {
            text.Append(" rgb=").Append(c.Red).Append(',').Append(c.Green).Append(',').Append(c.Blue);
        }
        text.AppendLine();
        foreach (GraphicalUiElement child in element.ContainedElements.Concat(element.Children).Distinct())
        {
            text.Append(Describe(child, depth + 1));
        }
        return text.ToString();
    }

    [Fact]
    public void ApplyDiff_UnrelatedEdit_MenuItemMatchesAFreshLoad()
    {
        string expected = Describe(Build(LoadCopy("Edited")));

        GraphicalUiElement screen = Build(LoadCopy(null));
        GumProjectSave reloaded = LoadCopy("Edited");
        ObjectFinder.Self.GumProjectSave = reloaded;
        GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);

        Describe(screen).ShouldBe(expected);
    }

    [Fact]
    public void Build_TwiceFromTheSameProject_GivesTheSameTree()
    {
        GumProjectSave project = LoadCopy(null);
        string first = Describe(Build(project));
        string second = Describe(Build(project));

        second.ShouldBe(first);
    }

    [Fact]
    public void ApplyDiff_EditedThenSetBack_MenuItemMatchesAFreshLoad()
    {
        string expected = Describe(Build(LoadCopy(null)));

        GraphicalUiElement screen = Build(LoadCopy(null));
        foreach (string? toolTip in new[] { "Edited", null })
        {
            GumProjectSave reloaded = LoadCopy(toolTip);
            ObjectFinder.Self.GumProjectSave = reloaded;
            GumHotReloadManager.ApplyDiff(new[] { screen }, reloaded, SystemManagers.Default);
        }

        Describe(screen).ShouldBe(expected);
    }

    [Theory]
    [InlineData(StandardFormsBehaviorNames.MenuItemBehaviorName)]
    [InlineData(StandardFormsBehaviorNames.ExpanderBehaviorName)]
    [InlineData(StandardFormsBehaviorNames.ListBoxItemBehaviorName)]
    public void ApplyDiff_IsEnabledEditedThenRemoved_ControlMatchesAFreshLoad(string behavior)
    {
        string expectedDisabled = Describe(Build(LoadCopy(null, behavior, false)));
        string expectedEnabled = Describe(Build(LoadCopy(null, behavior)));

        GraphicalUiElement screen = Build(LoadCopy(null, behavior));
        GumProjectSave disabled = LoadCopy(null, behavior, false);
        ObjectFinder.Self.GumProjectSave = disabled;
        GumHotReloadManager.ApplyDiff(new[] { screen }, disabled, SystemManagers.Default);
        Describe(screen).ShouldBe(expectedDisabled, "after editing");

        GumProjectSave enabled = LoadCopy(null, behavior);
        ObjectFinder.Self.GumProjectSave = enabled;
        GumHotReloadManager.ApplyDiff(new[] { screen }, enabled, SystemManagers.Default);
        Describe(screen).ShouldBe(expectedEnabled, "after removing");
    }

    [Fact]
    public void Expander_DisabledThenEnabledInCode_LooksLikeAFreshOne()
    {
        GumProjectSave project = LoadCopy(null, StandardFormsBehaviorNames.ExpanderBehaviorName);
        string expected = Describe(Build(project));
        GraphicalUiElement screen = Build(project);
        Expander expander = (Expander)((InteractiveGue)screen.GetGraphicalUiElementByName("Ctl1")!).FormsControlAsObject!;

        expander.IsEnabled = false;
        expander.IsEnabled = true;

        Describe(screen).ShouldBe(expected);
    }
}
