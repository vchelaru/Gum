using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A TabControl authored from a theme's components shows its first tab and page when the screen does not
/// set SelectedIndex, as the tool's preview does (#6408): the behavior declares SelectedIndex 0, which has
/// to survive the screen parenting its tabs after the control is built.
/// </summary>
public class ThemeTabControlDefaultSelectionTests : FileThemeInputTestBase
{
    private (TabControl Tabs, GraphicalUiElement Screen) BuildScreen(string theme, int? authoredIndex)
    {
        GumProjectSave project = LoadTheme(theme);
        string tabControl = ComponentNamed(project, "/TabControl");
        string tabItem = ComponentNamed(project, "/TabItem");
        string tabPage = ComponentNamed(project, "/TabPage");

        ScreenSave screen = new ScreenSave { Name = "TabScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Tabs", BaseType = tabControl, ParentContainer = screen });
        if (authoredIndex != null)
        {
            state.Variables.Add(new VariableSave { Type = "int", Name = "Tabs.SelectedIndex", Value = authoredIndex, SetsValue = true });
        }
        for (int i = 0; i < 3; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = "Tab" + i, BaseType = tabItem, ParentContainer = screen });
            state.Variables.Add(new VariableSave { Type = "string", Name = $"Tab{i}.Parent", Value = "Tabs.InnerPanelInstance", SetsValue = true });
            screen.Instances.Add(new InstanceSave { Name = "Page" + i, BaseType = tabPage, ParentContainer = screen });
            state.Variables.Add(new VariableSave { Type = "string", Name = $"Page{i}.Parent", Value = "Tabs.ContentPanelInstance", SetsValue = true });
        }
        project.Screens.Add(screen);

        GraphicalUiElement screenVisual = screen.ToGraphicalUiElement();
        screenVisual.AddToRoot();
        Driver.Step(2);
        TabControl tabs = (TabControl)((InteractiveGue)screenVisual.GetGraphicalUiElementByName("Tabs")!).FormsControlAsObject!;
        return (tabs, screenVisual);
    }

    private static bool[] PageVisibility(TabControl tabs) =>
        tabs.TabItems.Select(tab => tab.Page!.Visual.AbsoluteVisible).ToArray();

    [Theory]
    [InlineData("")]
    [InlineData("Bubblegum")]
    public void SelectedIndexNotAuthored_SelectsFirstTabAndShowsOnlyItsPage(string theme)
    {
        (TabControl tabs, _) = BuildScreen(theme, authoredIndex: null);

        tabs.SelectedIndex.ShouldBe(0);
        PageVisibility(tabs).ShouldBe(new[] { true, false, false });
    }

    [Fact]
    public void SelectedIndexAuthored_KeepsTheAuthoredTab()
    {
        (TabControl tabs, _) = BuildScreen("", authoredIndex: 2);

        tabs.SelectedIndex.ShouldBe(2);
        PageVisibility(tabs).ShouldBe(new[] { false, false, true });
    }

    [Fact]
    public void SelectedIndexAuthoredAsNone_KeepsNoSelection()
    {
        (TabControl tabs, _) = BuildScreen("", authoredIndex: -1);

        tabs.SelectedIndex.ShouldBe(-1);
    }

    [Fact]
    public void CodeCreatedTabControl_StillStartsWithNoSelection()
    {
        TabControl tabs = new TabControl();
        tabs.AddTab("A");
        tabs.AddTab("B");

        tabs.SelectedIndex.ShouldBe(-1);
    }
}
