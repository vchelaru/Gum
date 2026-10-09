using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// Setting a variable that no reference reads must cost nothing, even on a component that has references
/// (ADR 0022): SetProperty runs on every state change of every element.
/// </summary>
public class ReferenceReevaluationAllocationTests : BaseTestClass
{
    public ReferenceReevaluationAllocationTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    public override void Dispose()
    {
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    [Fact]
    public void SetProperty_VariableNoReferenceReads_IsZeroAllocation()
    {
        ComponentSave element = new ComponentSave { Name = "Wave", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "VariableReferences" };
        list.Value.Add("Y = Progress * 10");
        state.VariableLists.Add(list);
        element.Instances.Add(new InstanceSave { Name = "Child", BaseType = "Container", ParentContainer = element });
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container"));
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        GraphicalUiElement child = gue.GetGraphicalUiElementByName("Child")!;
        object rotation = 15f;
        bool toggle = false;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                gue.SetProperty("Rotation", rotation);
                child.SetProperty("Rotation", rotation);
                // Typed properties report without boxing when nothing reads them.
                gue.Width = toggle ? 10 : 20;
                child.Rotation = toggle ? 10 : 20;
                toggle = !toggle;
            },
            attempts: 3,
            warmupIterations: 50,
            measuredIterations: 500);

        result.TotalBytes.ShouldBe(0);
    }
}
