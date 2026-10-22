using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Wireframe;
using GumRuntime;
using MonoGameGum.TestsCommon;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Performance;

/// <summary>
/// A live update re-evaluates every row that reads the changed variable, which a game does every time that variable
/// changes. A row whose NaN or infinity the target rejects (#6457) and has already reported must allocate no more than a
/// row that applies.
/// </summary>
public class ReferenceRejectedByTargetAllocationTests : BaseTestClass
{
    public ReferenceRejectedByTargetAllocationTests()
    {
        StandardElementsManager.Self.Initialize();
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ObjectFinder.Self.GumProjectSave = null;
        base.Dispose();
    }

    private const long Tolerance = 16;

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveUpdate_RejectedRowAlreadyReported_AllocatesNoMoreThanAnApplicableRow(bool useExpressions)
    {
        double applicable = MeasureLiveUpdate(useExpressions, 10f, 20f);
        double rejected = MeasureLiveUpdate(useExpressions, float.PositiveInfinity, float.NegativeInfinity);

        rejected.ShouldBeLessThanOrEqualTo(applicable + Tolerance, $"applicable {applicable} bytes per frame, rejected {rejected}");
    }

    // A size bound takes infinity (#6473), so a row that sets one to a non-finite value allocates no more than finite ones.
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void LiveUpdate_MinHeightRowWithNonFiniteValues_AllocatesNoMoreThanFiniteValues(bool useExpressions)
    {
        double finite = MeasureLiveUpdate(useExpressions, 10f, 20f, "MinHeight");
        double nonFinite = MeasureLiveUpdate(useExpressions, float.PositiveInfinity, float.NegativeInfinity, "MinHeight");

        nonFinite.ShouldBeLessThanOrEqualTo(finite + Tolerance, $"finite {finite} bytes per frame, non-finite {nonFinite}");
    }

    private static double MeasureLiveUpdate(bool useExpressions, float first, float second, string target = "Width")
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        if (useExpressions) GumExpressionService.Initialize();
        ComponentSave element = new ComponentSave { Name = "Pair", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        element.Instances.Add(new InstanceSave { Name = "Source", BaseType = "Container", ParentContainer = element });
        element.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = element });
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        list.Value.Add($"{target} = Source.StackSpacing");
        state.VariableLists.Add(list);
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(element);
        StandardElementSave container = new StandardElementSave { Name = "Container" };
        container.States.Add(StandardElementsManager.Self.GetDefaultStateFor("Container")!);
        container.DefaultState!.ParentContainer = container;
        project.StandardElements.Add(container);
        ObjectFinder.Self.GumProjectSave = project;
        GraphicalUiElement gue = element.ToGraphicalUiElement();
        GraphicalUiElement source = gue.GetGraphicalUiElementByName("Source")!;
        bool toggle = false;

        AllocationResult result = AllocationMeasurer.MeasureMinimum(
            () =>
            {
                source.SetProperty("StackSpacing", toggle ? first : second);
                toggle = !toggle;
            },
            attempts: 5,
            warmupIterations: 100,
            measuredIterations: 500);

        return result.BytesPerIteration;
    }
}
