using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Gum.Presentation.Tests;

/// <summary>
/// A circular variable reference must never freeze or overflow the stack in the tool (#6871). Every
/// step runs under a time guard, so a freeze fails the test instead of hanging the run.
/// </summary>
public class ReferenceCycleToolFreezeTests : BaseTestClass
{
    private const int GuardMilliseconds = 5000;
    private readonly VariableReferenceLogic _logic;

    public ReferenceCycleToolFreezeTests()
    {
        GumExpressionService.Initialize();
        StandardElementsManager.Self.Initialize();
        Mock<IWireframeObjectManager> wireframe = new Mock<IWireframeObjectManager>();
        Mock<ICompositeMemberRegistry> registry = new Mock<ICompositeMemberRegistry>();
        registry.Setup(x => x.Descriptors).Returns(new List<CompositeMemberDescriptor>());
        _logic = new VariableReferenceLogic(
            new Mock<IGuiCommands>().Object,
            new Mock<IWireframeCommands>().Object,
            new Mock<IDialogService>().Object,
            new Mock<IFileCommands>().Object,
            registry.Object,
            new Mock<IDispatcher>().Object,
            wireframe.Object);
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
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

    private static GumProjectSave CreateProject()
    {
        GumProjectSave project = new GumProjectSave();
        StandardElementSave standardElement = new StandardElementSave { Name = "Container" };
        standardElement.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!.Clone());
        standardElement.DefaultState!.ParentContainer = standardElement;
        project.StandardElements.Add(standardElement);
        ObjectFinder.Self.GumProjectSave = project;
        return project;
    }

    public static TheoryData<string> Cases() => new TheoryData<string>
    {
        "TwoCycle", "ThreeCycle", "SelfReference", "ElementLevel", "CrossElement", "StandardElement"
    };

    [Theory]
    [MemberData(nameof(Cases))]
    public void EveryToolPath_ACircularReference_NeverFreezes(string scenario)
    {
        GumProjectSave project = CreateProject();
        ComponentSave panel;
        InstanceSave? instance;
        switch (scenario)
        {
            case "TwoCycle":
                panel = AddPanel(project, "Panel", "A", "B");
                AddRows(panel.DefaultState, "A", "Width = B.Width + 1");
                AddRows(panel.DefaultState, "B", "Width = A.Width + 1");
                break;
            case "ThreeCycle":
                panel = AddPanel(project, "Panel", "A", "B", "C");
                AddRows(panel.DefaultState, "A", "Width = B.Width + 1");
                AddRows(panel.DefaultState, "B", "Width = C.Width + 1");
                AddRows(panel.DefaultState, "C", "Width = A.Width + 1");
                break;
            case "SelfReference":
                panel = AddPanel(project, "Panel", "A");
                AddRows(panel.DefaultState, "A", "Width = A.Width + 1");
                break;
            case "ElementLevel":
                panel = AddPanel(project, "Panel", "A");
                AddRows(panel.DefaultState, null, "Width = Height + 1", "Height = Width + 1");
                break;
            case "CrossElement":
                panel = AddPanel(project, "Panel", "A");
                ComponentSave other = AddPanel(project, "Other", "B");
                AddRows(panel.DefaultState, "A", "Width = Components/Other.B.Width + 1");
                AddRows(other.DefaultState, "B", "Width = Components/Panel.A.Width + 1");
                break;
            default:
                panel = AddPanel(project, "Panel", "A");
                AddRows(project.StandardElements[0].DefaultState, null, "Width = Height + 1", "Height = Width + 1");
                break;
        }
        instance = panel.GetInstance("A");
        StateSave state = panel.DefaultState;

        Guard(scenario, "ApplyReferencesToElement", () => _logic.ApplyReferencesToElement(panel, trySave: false));
        Guard(scenario, "DoVariableReferenceReaction (Width)", () =>
            _logic.DoVariableReferenceReaction(panel, instance, "Width", state, "A.Width", trySave: false));
        Guard(scenario, "DoVariableReferenceReaction (VariableReferences)", () =>
            _logic.DoVariableReferenceReaction(panel, instance, "VariableReferences", state, "A.VariableReferences", trySave: false));
        Guard(scenario, "ApplyVariableReferences x3", () =>
        {
            panel.ApplyVariableReferences(state);
            panel.ApplyVariableReferences(state);
            panel.ApplyVariableReferences(state);
        });
        Guard(scenario, "GetElementReferencesToThis", () => ObjectFinder.Self.GetElementReferencesToThis(panel));
        Guard(scenario, "ApplyAllVariableReferences", () => project.ApplyAllVariableReferences());
        Guard(scenario, "HeadlessErrorChecker.GetAllErrors", () =>
            new HeadlessErrorChecker(new DefaultTypeResolver()).GetAllErrors(project));
        Guard(scenario, "ReapplySiblingDependentReferences", () => _logic.ReapplySiblingDependentReferences(panel));
    }

    [Theory]
    [InlineData("TwoCycle")]
    [InlineData("ThreeCycle")]
    [InlineData("SelfReference")]
    public void ApplyingRepeatedly_ACycle_NeverChangesTheStoredValues(string scenario)
    {
        GumProjectSave project = CreateProject();
        string[] names = scenario == "ThreeCycle" ? new[] { "A", "B", "C" } : scenario == "TwoCycle" ? new[] { "A", "B" } : new[] { "A" };
        ComponentSave panel = AddPanel(project, "Panel", names);
        for (int i = 0; i < names.Length; i++)
        {
            string next = names.Length == 1 ? names[0] : names[(i + 1) % names.Length];
            AddRows(panel.DefaultState, names[i], $"Width = {next}.Width + 1");
        }
        string Snapshot() => string.Join(",", names.Select(name => panel.DefaultState.GetValue(name + ".Width")));
        string before = Snapshot();

        for (int i = 0; i < 5; i++)
        {
            panel.ApplyVariableReferences(panel.DefaultState);
            project.ApplyAllVariableReferences();
            _logic.ApplyReferencesToElement(panel, trySave: false);
            Snapshot().ShouldBe(before, $"pass {i}");
        }
    }
}
