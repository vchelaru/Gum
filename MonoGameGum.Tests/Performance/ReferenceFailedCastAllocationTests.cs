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
/// changes. With Gum.Expressions an applicable row and an already-reported refused row (#6463) must allocate no more
/// than the row did before refused rows were reported.
/// </summary>
public class ReferenceFailedCastAllocationTests : BaseTestClass
{
    public ReferenceFailedCastAllocationTests()
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

    // Bytes per frame measured on main, before refused rows were reported (the Width and Visible updates themselves
    // allocate most of it). A refused row evaluated once then too, and the tolerance absorbs measurement noise only.
    // The expression and plain figures include 24 bytes for VariableSave.ShaderParameter: each frame allocates three
    // VariableSaves, and the new reference field makes every one 8 bytes larger.
    private const long ExpressionRowBytesPerFrame = 5140;
    private const long PlainRowBytesPerFrame = 4836;
    private const long RefusedRowBytesPerFrame = 3484;
    private const long Tolerance = 16;

    [Fact]
    public void LiveUpdate_ApplicableRowWithGumExpressions_AllocatesNoMoreThanBefore()
    {
        double bytes = MeasureLiveUpdate("Width = Source.Width * 2");

        bytes.ShouldBeLessThanOrEqualTo(ExpressionRowBytesPerFrame + Tolerance, $"measured {bytes} bytes per frame");
    }

    [Fact]
    public void LiveUpdate_PlainApplicableRowWithGumExpressions_AllocatesNoMoreThanBefore()
    {
        double bytes = MeasureLiveUpdate("Width = Source.Width");

        bytes.ShouldBeLessThanOrEqualTo(PlainRowBytesPerFrame + Tolerance);
    }

    [Fact]
    public void LiveUpdate_RefusedRowAlreadyReportedWithGumExpressions_AllocatesNoMoreThanBefore()
    {
        double bytes = MeasureLiveUpdate("Width = Source.Visible");

        bytes.ShouldBeLessThanOrEqualTo(RefusedRowBytesPerFrame + Tolerance);
    }

    private static double MeasureLiveUpdate(string row, bool useExpressions = true)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        if (useExpressions) GumExpressionService.Initialize();
        ComponentSave element = new ComponentSave { Name = "Pair", BaseType = "Container" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = element };
        element.States.Add(state);
        element.Instances.Add(new InstanceSave { Name = "Source", BaseType = "Container", ParentContainer = element });
        element.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = element });
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        list.Value.Add(row);
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
                source.Width = toggle ? 10 : 20;
                source.Visible = toggle;
                toggle = !toggle;
            },
            attempts: 5,
            warmupIterations: 100,
            measuredIterations: 500);

        return result.BytesPerIteration;
    }
}
