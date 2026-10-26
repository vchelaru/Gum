using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using Shouldly;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// A circular variable reference must never freeze or overflow the stack, on any evaluation path of a
/// running game (#6871). Every step runs under a time guard, so a freeze fails the test instead of
/// hanging the run.
/// </summary>
public class ReferenceCycleFreezeTests : BaseTestClass
{
    private const int GuardMilliseconds = 5000;

    public ReferenceCycleFreezeTests()
    {
        StandardElementsManager.Self.Initialize();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
    }

    public override void Dispose()
    {
        ElementSaveExtensions.Reset();
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private sealed class Scene
    {
        public GumProjectSave Project = null!;
        public ScreenSave Screen = null!;
        // The element holding the rows (a screen instance named "P").
        public string PanelName = "P";
        // Instances inside the panel whose Width a step writes, then reads back.
        public string[] SourceNames = Array.Empty<string>();
        public string? StateCategory;
        public string? StateName;
    }

    private static void Guard(string scenario, string step, Action action)
    {
        Task task = Task.Run(action);
        if (!task.Wait(GuardMilliseconds))
        {
            Assert.Fail($"{scenario}: '{step}' did not finish in {GuardMilliseconds} ms (freeze)");
        }
        task.GetAwaiter().GetResult();
    }

    private static GumProjectSave CreateProject()
    {
        GumProjectSave project = new GumProjectSave();
        foreach (string standard in new[] { "Container", "Text" })
        {
            StandardElementSave standardElement = new StandardElementSave { Name = standard };
            standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor(standard)!.Clone());
            standardElement.DefaultState!.ParentContainer = standardElement;
            project.StandardElements.Add(standardElement);
        }
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    private static void AddRows(StateSave state, string? instanceName, params string[] rows)
    {
        VariableListSave<string> list = new VariableListSave<string>
        {
            Type = "string",
            Name = string.IsNullOrEmpty(instanceName) ? "VariableReferences" : instanceName + ".VariableReferences"
        };
        foreach (string row in rows)
        {
            list.Value.Add(row);
        }
        state.VariableLists.Add(list);
    }

    private static ComponentSave AddPanel(GumProjectSave project, string name, params string[] instanceNames)
    {
        ComponentSave panel = new ComponentSave { Name = name, BaseType = "Container" };
        panel.States.Add(new StateSave { Name = "Default", ParentContainer = panel });
        foreach (string instanceName in instanceNames)
        {
            panel.Instances.Add(new InstanceSave { Name = instanceName, BaseType = "Container", ParentContainer = panel });
            panel.DefaultState.Variables.Add(new VariableSave
            {
                Name = instanceName + ".Width", Type = "float", Value = 10f, SetsValue = true
            });
        }
        project.Components.Add(panel);
        return panel;
    }

    private static ScreenSave AddScreen(GumProjectSave project, params (string Instance, string Type)[] instances)
    {
        ScreenSave screen = new ScreenSave { Name = "TestScreen" };
        screen.States.Add(new StateSave { Name = "Default", ParentContainer = screen });
        foreach ((string instance, string type) in instances)
        {
            screen.Instances.Add(new InstanceSave { Name = instance, BaseType = type, ParentContainer = screen });
        }
        project.Screens.Add(screen);
        return screen;
    }

    private static Scene TwoCycle()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B");
        AddRows(panel.DefaultState, "A", "Width = B.Width + 1");
        AddRows(panel.DefaultState, "B", "Width = A.Width + 1");
        return new Scene { Project = project, Screen = AddScreen(project, ("P", "Panel")), SourceNames = new[] { "A", "B" } };
    }

    private static Scene ThreeCycle()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B", "C");
        AddRows(panel.DefaultState, "A", "Width = B.Width + 1");
        AddRows(panel.DefaultState, "B", "Width = C.Width + 1");
        AddRows(panel.DefaultState, "C", "Width = A.Width + 1");
        return new Scene { Project = project, Screen = AddScreen(project, ("P", "Panel")), SourceNames = new[] { "A", "B", "C" } };
    }

    private static Scene SelfReference()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A");
        AddRows(panel.DefaultState, "A", "Width = A.Width + 1");
        return new Scene { Project = project, Screen = AddScreen(project, ("P", "Panel")), SourceNames = new[] { "A" } };
    }

    private static Scene ElementLevelCycle()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A");
        AddRows(panel.DefaultState, null, "Width = Height + 1", "Height = Width + 1");
        return new Scene { Project = project, Screen = AddScreen(project, ("P", "Panel")), SourceNames = new[] { "A" } };
    }

    private static Scene CrossComponentCycle()
    {
        GumProjectSave project = CreateProject();
        ComponentSave first = AddPanel(project, "Panel", "A");
        ComponentSave second = AddPanel(project, "Other", "B");
        AddRows(first.DefaultState, "A", "Width = Components/Other.B.Width + 1");
        AddRows(second.DefaultState, "B", "Width = Components/Panel.A.Width + 1");
        return new Scene
        {
            Project = project,
            Screen = AddScreen(project, ("P", "Panel"), ("Q", "Other")),
            SourceNames = new[] { "A" }
        };
    }

    private static Scene StandardElementCycle()
    {
        GumProjectSave project = CreateProject();
        StandardElementSave container = project.StandardElements.Find(item => item.Name == "Container")!;
        AddRows(container.DefaultState, null, "Width = Height + 1", "Height = Width + 1");
        ComponentSave panel = AddPanel(project, "Panel", "A");
        return new Scene { Project = project, Screen = AddScreen(project, ("P", "Panel")), SourceNames = new[] { "A" } };
    }

    private static Scene CycleThroughStateVariable()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B");
        AddRows(panel.DefaultState, "A", "Width = B.Width + 1");
        AddRows(panel.DefaultState, "B", "Width = A.Width + 1");
        StateSave on = new StateSave { Name = "On", ParentContainer = panel };
        on.Variables.Add(new VariableSave { Name = "A.Width", Type = "float", Value = 50f, SetsValue = true });
        on.Variables.Add(new VariableSave { Name = "B.Width", Type = "float", Value = 60f, SetsValue = true });
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(on);
        panel.Categories.Add(category);
        return new Scene
        {
            Project = project,
            Screen = AddScreen(project, ("P", "Panel")),
            SourceNames = new[] { "A", "B" },
            StateCategory = "Cat",
            StateName = "On"
        };
    }

    private static Scene CycleInNonDefaultState()
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B");
        StateSave alt = new StateSave { Name = "Alt", ParentContainer = panel };
        AddRows(alt, "A", "Width = B.Width + 1");
        AddRows(alt, "B", "Width = A.Width + 1");
        StateSaveCategory category = new StateSaveCategory { Name = "Cat" };
        category.States.Add(alt);
        panel.Categories.Add(category);
        return new Scene
        {
            Project = project,
            Screen = AddScreen(project, ("P", "Panel")),
            SourceNames = new[] { "A", "B" },
            StateCategory = "Cat",
            StateName = "Alt"
        };
    }

    public static TheoryData<string, bool> Cases()
    {
        TheoryData<string, bool> data = new TheoryData<string, bool>();
        foreach (string name in new[]
        {
            nameof(TwoCycle), nameof(ThreeCycle), nameof(SelfReference), nameof(ElementLevelCycle),
            nameof(CrossComponentCycle), nameof(StandardElementCycle), nameof(CycleThroughStateVariable),
            nameof(CycleInNonDefaultState)
        })
        {
            data.Add(name, false);
            data.Add(name, true);
        }
        return data;
    }

    private static Scene Build(string name) => name switch
    {
        nameof(TwoCycle) => TwoCycle(),
        nameof(ThreeCycle) => ThreeCycle(),
        nameof(SelfReference) => SelfReference(),
        nameof(ElementLevelCycle) => ElementLevelCycle(),
        nameof(CrossComponentCycle) => CrossComponentCycle(),
        nameof(StandardElementCycle) => StandardElementCycle(),
        nameof(CycleThroughStateVariable) => CycleThroughStateVariable(),
        nameof(CycleInNonDefaultState) => CycleInNonDefaultState(),
        _ => throw new ArgumentException(name)
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryRuntimePath_ACircularReference_NeverFreezes(string scenarioName, bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        string label = $"{scenarioName} (expressions {useExpressions})";
        Scene scene = Build(scenarioName);
        GraphicalUiElement? root = null;

        Guard(label, "ToGraphicalUiElement", () => root = scene.Screen.ToGraphicalUiElement());
        GraphicalUiElement panel = root!.GetGraphicalUiElementByName(scene.PanelName)!;

        foreach (string sourceName in scene.SourceNames)
        {
            GraphicalUiElement source = panel.GetGraphicalUiElementByName(sourceName)!;
            Guard(label, $"{sourceName}.SetProperty", () => source.SetProperty("Width", 7f));
            Guard(label, $"{sourceName}.Width typed setter", () => source.Width = 9f);
        }
        Guard(label, "panel.Width typed setter", () => panel.Width = 33f);
        Guard(label, "panel.SetProperty", () => panel.SetProperty("Width", 44f));

        if (scene.StateName != null)
        {
            Guard(label, "ApplyState", () => panel.ApplyState(scene.StateCategory!, scene.StateName!));
            Guard(label, "ApplyState again", () => panel.ApplyState(scene.StateCategory!, scene.StateName!));
            StateSave state = scene.Project.Components[0].Categories[0].States[0];
            Guard(label, "ApplyState(StateSave)", () => panel.ApplyState(state));
        }

        Guard(label, "RefreshStyles", () => root!.RefreshStyles());
        Guard(label, "ApplyAllVariableReferences", () => scene.Project.ApplyAllVariableReferences());
        Guard(label, "create again after ApplyAll", () => root = scene.Screen.ToGraphicalUiElement());
    }

    // The rule for a cycle: its rows are skipped and keep their stored values, on every path, so applying
    // again changes nothing.
    [Fact]
    public void Creation_TwoRowsReadingEachOther_KeepTheStoredValues()
    {
        GumExpressionService.Initialize();
        Scene scene = TwoCycle();

        GraphicalUiElement panel = scene.Screen.ToGraphicalUiElement().GetGraphicalUiElementByName("P")!;

        panel.GetGraphicalUiElementByName("A")!.Width.ShouldBe(10f);
        panel.GetGraphicalUiElementByName("B")!.Width.ShouldBe(10f);
    }

    [Fact]
    public void Creation_RowReadingItsOwnResult_KeepsTheStoredValue()
    {
        GumExpressionService.Initialize();
        Scene scene = SelfReference();

        GraphicalUiElement panel = scene.Screen.ToGraphicalUiElement().GetGraphicalUiElementByName("P")!;

        panel.GetGraphicalUiElementByName("A")!.Width.ShouldBe(10f);
    }

    [Fact]
    public void ChangingAVariableTheCycleReads_DoesNotRunTheCycle()
    {
        GumExpressionService.Initialize();
        Scene scene = TwoCycle();
        GraphicalUiElement panel = scene.Screen.ToGraphicalUiElement().GetGraphicalUiElementByName("P")!;
        GraphicalUiElement a = panel.GetGraphicalUiElementByName("A")!;
        GraphicalUiElement b = panel.GetGraphicalUiElementByName("B")!;

        a.Width = 100f;

        a.Width.ShouldBe(100f);
        b.Width.ShouldBe(10f);
    }

    [Theory]
    [InlineData("TwoCycle", false)]
    [InlineData("TwoCycle", true)]
    [InlineData("ThreeCycle", false)]
    [InlineData("ThreeCycle", true)]
    [InlineData("SelfReference", true)]
    public void ApplyingRepeatedly_ACycle_NeverChangesTheStoredValuesOrTheVisuals(string scenarioName, bool useExpressions)
    {
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        Scene scene = Build(scenarioName);
        ComponentSave panelComponent = scene.Project.Components[0];
        string Snapshot(GraphicalUiElement panel) =>
            string.Join(",", scene.SourceNames.Select(name => name + "=" + panelComponent.DefaultState.GetValue(name + ".Width") + "/" + panel.GetGraphicalUiElementByName(name)!.Width));

        GraphicalUiElement root = scene.Screen.ToGraphicalUiElement();
        string first = Snapshot(root.GetGraphicalUiElementByName("P")!);
        for (int i = 0; i < 5; i++)
        {
            scene.Project.ApplyAllVariableReferences();
            root.RefreshStyles();
            root = scene.Screen.ToGraphicalUiElement();
            Snapshot(root.GetGraphicalUiElementByName("P")!).ShouldBe(first, $"pass {i}");
        }
    }

    [Fact]
    public void Creation_ChainDownstreamOfNothingCyclic_IsUnchanged()
    {
        GumExpressionService.Initialize();
        GumProjectSave project = CreateProject();
        ComponentSave panel = AddPanel(project, "Panel", "A", "B", "C");
        AddRows(panel.DefaultState, "C", "Width = B.Width + 1");
        AddRows(panel.DefaultState, "B", "Width = A.Width + 1");
        ScreenSave screen = AddScreen(project, ("P", "Panel"));

        GraphicalUiElement p = screen.ToGraphicalUiElement().GetGraphicalUiElementByName("P")!;

        p.GetGraphicalUiElementByName("B")!.Width.ShouldBe(11f);
        p.GetGraphicalUiElementByName("C")!.Width.ShouldBe(12f);
    }

    [Theory]
    [InlineData("TwoCycle")]
    [InlineData("ThreeCycle")]
    [InlineData("SelfReference")]
    [InlineData("ElementLevelCycle")]
    [InlineData("CrossComponentCycle")]
    public void HotReload_ACycle_NeverFreezesAndNeverChangesTheValues(string scenarioName)
    {
        GumExpressionService.Initialize();
        Scene scene = Build(scenarioName);
        GraphicalUiElement root = scene.Screen.ToGraphicalUiElement();
        string Snapshot() => string.Join(",", scene.SourceNames.Select(name =>
            root.GetGraphicalUiElementByName(scene.PanelName)!.GetGraphicalUiElementByName(name)!.Width));
        string first = Snapshot();

        for (int i = 0; i < 4; i++)
        {
            Scene reloaded = Build(scenarioName);
            Guard(scenarioName, "ApplyAllVariableReferences before reload", () => reloaded.Project.ApplyAllVariableReferences());
            Guard(scenarioName, "ApplyDiff", () =>
            {
                ObjectFinder.Self.GumProjectSave = reloaded.Project;
                global::Gum.GumHotReloadManager.ApplyDiff(new[] { root }, reloaded.Project, RenderingLibrary.SystemManagers.Default);
            });
            Snapshot().ShouldBe(first, $"reload {i}");
        }
    }
}
