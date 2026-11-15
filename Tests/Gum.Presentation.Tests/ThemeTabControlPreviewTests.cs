using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Gum.Wireframe;
using GumRuntime;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// A TabControl authored from a shipped theme's TabControl, TabItem and TabPage components previews only the
/// selected tab's page in the tool (#6408): each tab and page reads the control's SelectedIndex through its
/// behavior's FindAncestor line, and the page's own Unselected state hides it.
/// </summary>
public class ThemeTabControlPreviewTests : BaseTestClass
{
    private readonly Func<string, ISystemManagers, IRenderable?>? _previousRenderableFunc;

    public ThemeTabControlPreviewTests()
    {
        GumExpressionService.Initialize();
        _previousRenderableFunc = ElementSaveExtensions.CustomCreateGraphicalComponentFunc;
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = (name, _) =>
            ObjectFinder.Self.GumProjectSave?.StandardElements.Any(standard => standard.Name == name) == true
                ? new StateSelectionCanvasHarness.RecordingRenderable()
                : null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomCreateGraphicalComponentFunc = _previousRenderableFunc;
        base.Dispose();
    }

    [Theory]
    [InlineData("FormsTemplate", "Controls/", 1)]
    [InlineData("FormsTemplate", "Controls/", null)]
    [InlineData("FormsThemes/Bubblegum", "Bubblegum/", 2)]
    [InlineData("FormsThemes/Bubblegum", "Bubblegum/", null)]
    [InlineData("FormsThemes/DarkPro", "DarkPro/", 2)]
    [InlineData("FormsThemes/DarkPro", "DarkPro/", null)]
    [InlineData("FormsThemes/Meadow", "Meadow/", 2)]
    [InlineData("FormsThemes/Meadow", "Meadow/", null)]
    [InlineData("FormsThemes/Editor", "Editor/", 2)]
    [InlineData("FormsThemes/Editor", "Editor/", null)]
    [InlineData("FormsThemes/Retro95", "Retro95/", 2)]
    [InlineData("FormsThemes/Retro95", "Retro95/", null)]
    [InlineData("FormsThemes/ForestGlade", "ForestGlade/", 2)]
    [InlineData("FormsThemes/ForestGlade", "ForestGlade/", null)]
    [InlineData("FormsThemes/Hazard", "Hazard/", 2)]
    [InlineData("FormsThemes/Hazard", "Hazard/", null)]
    [InlineData("FormsThemes/Neon", "Neon/", 2)]
    [InlineData("FormsThemes/Neon", "Neon/", null)]
    public void Wireframe_ThemeTabControl_ShowsOnlyTheSelectedPage(string template, string prefix, int? selectedIndex)
    {
        StandardElementsManager.Self.Initialize();
        ProjectLoadResult result = new ProjectLoader().Load(Path.Combine(FindTemplatesDirectory(), template, "GumProject.gumx"));
        result.Success.ShouldBeTrue(result.ErrorMessage);
        GumProjectSave project = result.Project!;
        ObjectFinder.Self.GumProjectSave = project;

        ScreenSave screen = new ScreenSave { Name = "TabScreen" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = screen };
        screen.States.Add(state);
        screen.Instances.Add(new InstanceSave { Name = "Tabs", BaseType = prefix + "TabControl", ParentContainer = screen });
        if (selectedIndex != null)
        {
            state.Variables.Add(new VariableSave { Type = "int", Name = "Tabs.SelectedIndex", Value = selectedIndex, SetsValue = true });
        }
        for (int i = 0; i < 3; i++)
        {
            screen.Instances.Add(new InstanceSave { Name = "Tab" + i, BaseType = prefix + "TabItem", ParentContainer = screen });
            state.Variables.Add(new VariableSave { Type = "string", Name = $"Tab{i}.Parent", Value = "Tabs.InnerPanelInstance", SetsValue = true });
            screen.Instances.Add(new InstanceSave { Name = "Page" + i, BaseType = prefix + "TabPage", ParentContainer = screen });
            state.Variables.Add(new VariableSave { Type = "string", Name = $"Page{i}.Parent", Value = "Tabs.ContentPanelInstance", SetsValue = true });
        }
        project.Screens.Add(screen);

        BehaviorToolOnlyReferencesApplier.Apply(screen, state);
        GraphicalUiElement root = StateSelectionCanvasHarness.Show(screen, state);

        int selected = selectedIndex ?? 0;
        string[] expectedStates = Enumerable.Range(0, 3).Select(i => i == selected ? "Selected" : "Unselected").ToArray();
        Enumerable.Range(0, 3).Select(i => state.GetValue($"Tab{i}.TabItemCategoryState") as string).ShouldBe(expectedStates);
        Enumerable.Range(0, 3).Select(i => state.GetValue($"Page{i}.TabPageCategoryState") as string).ShouldBe(expectedStates);
        Enumerable.Range(0, 3).Select(i => root.GetGraphicalUiElementByName($"Page{i}")!.Visible)
            .ShouldBe(Enumerable.Range(0, 3).Select(i => i == selected).ToArray());
    }

    private static string FindTemplatesDirectory()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 12; i++)
        {
            string candidate = Path.Combine(current, "Tools", "Gum.ProjectServices", "Templates");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }
            string? parent = Path.GetDirectoryName(current);
            if (string.IsNullOrEmpty(parent) || parent == current)
            {
                break;
            }
            current = parent;
        }
        throw new InvalidOperationException("could not locate the templates directory from " + AppContext.BaseDirectory);
    }
}
