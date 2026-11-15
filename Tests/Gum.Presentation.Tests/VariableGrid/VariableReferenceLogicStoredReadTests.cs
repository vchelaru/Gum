using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Wireframe;
using GumRuntime;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests.VariableGrid;

/// <summary>
/// What the tool's VariableReferenceLogic.ApplyReferencesToElement reads for a reference: the stored state, never the
/// live canvas, except for Absolute* names which only the live representation can answer. Pins the current answers;
/// see the 8024 model doc.
/// </summary>
public class VariableReferenceLogicStoredReadTests : BaseTestClass
{
    private readonly Mock<IWireframeObjectManager> _wireframe = new();
    private readonly VariableReferenceLogic _sut;

    public VariableReferenceLogicStoredReadTests()
    {
        _sut = new VariableReferenceLogic(
            Mock.Of<IGuiCommands>(),
            Mock.Of<IWireframeCommands>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IFileCommands>(),
            Mock.Of<ICompositeMemberRegistry>(),
            Mock.Of<IDispatcher>(),
            _wireframe.Object);
    }

    public override void Dispose()
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        ElementSaveExtensions.VariableChangedThroughReference = null;
        base.Dispose();
    }

    private static ComponentSave Build(bool useExpressions, string row, params (string Name, float Value)[] stored)
    {
        ElementSaveExtensions.CustomEvaluateExpression = null;
        ElementSaveExtensions.CustomEvaluateExpressionAllBranches = null;
        if (useExpressions)
        {
            GumExpressionService.Initialize();
        }
        GumProjectSave project = new GumProjectSave();
        ObjectFinder.Self.GumProjectSave = project;
        ComponentSave component = new ComponentSave { Name = "Panel" };
        StateSave state = new StateSave { Name = "Default", ParentContainer = component };
        component.States.Add(state);
        component.Instances.Add(new InstanceSave { Name = "Source", BaseType = "Container", ParentContainer = component });
        component.Instances.Add(new InstanceSave { Name = "Target", BaseType = "Container", ParentContainer = component });
        foreach ((string name, float value) in stored)
        {
            state.Variables.Add(new VariableSave { Name = name, Type = "float", Value = value, SetsValue = true });
        }
        VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = "Target.VariableReferences" };
        list.Value.Add(row);
        state.VariableLists.Add(list);
        project.Components.Add(component);
        return component;
    }

    private static GraphicalUiElement LiveTree(float sourceWidth)
    {
        GraphicalUiElement root = new GraphicalUiElement(new global::RenderingLibrary.Graphics.InvisibleRenderable());
        GraphicalUiElement source = new GraphicalUiElement(new global::RenderingLibrary.Graphics.InvisibleRenderable()) { Name = "Source", Width = sourceWidth, WidthUnits = DimensionUnitType.Absolute };
        source.Parent = root;
        root.UpdateLayout();
        return root;
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reaction_SourceStored_TargetStateGetsTheStoredValue(bool useExpressions)
    {
        ComponentSave component = Build(useExpressions, "Width = Source.Width", ("Source.Width", 50f), ("Target.Width", 10f));

        _sut.DoVariableReferenceReaction(component, null, "Width", component.DefaultState!, "Source.Width", trySave: false);

        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reaction_LiveCanvasDiffersFromStoredValue_TargetStateStillGetsTheStoredValue(bool useExpressions)
    {
        ComponentSave component = Build(useExpressions, "Width = Source.Width", ("Source.Width", 50f), ("Target.Width", 10f));
        _wireframe.Setup(x => x.GetRepresentation(component)).Returns(LiveTree(77));

        _sut.DoVariableReferenceReaction(component, null, "Width", component.DefaultState!, "Source.Width", trySave: false);

        // pins current behavior; see model doc, inconsistency 1
        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(50f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reaction_SourceNeverStored_TargetStateKeepsItsValue(bool useExpressions)
    {
        ComponentSave component = Build(useExpressions, "Width = Source.Width", ("Target.Width", 10f));

        _sut.DoVariableReferenceReaction(component, null, "Width", component.DefaultState!, "Source.Width", trySave: false);

        // pins current behavior: with no Container standard in the project there is nothing to fall back to
        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(10f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Reaction_ForwardedDottedNameStoredLikeAFormsProperty_TargetStateGetsTheStoredValue(bool useExpressions)
    {
        ComponentSave component = Build(useExpressions, "Width = Source.Minimum", ("Source.Minimum", 12f), ("Target.Width", 10f));

        _sut.DoVariableReferenceReaction(component, null, "Width", component.DefaultState!, "Source.Width", trySave: false);

        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(12f);
    }

    [Fact]
    public void Reaction_AbsoluteWidthWithExpressionsAndLiveCanvas_TargetStateGetsTheLiveValue()
    {
        ComponentSave component = Build(true, "Width = Source.AbsoluteWidth", ("Source.Width", 50f), ("Target.Width", 10f));
        _wireframe.Setup(x => x.GetRepresentation(component)).Returns(LiveTree(77));

        _sut.DoVariableReferenceReaction(component, null, "Width", component.DefaultState!, "Source.Width", trySave: false);

        // pins current behavior; see model doc, inconsistency 2 (the tool reads the live canvas for Absolute*)
        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(77f);
    }

    [Fact]
    public void Reaction_AbsoluteWidthWithoutALiveCanvas_TargetStateKeepsItsValue()
    {
        ComponentSave component = Build(true, "Width = Source.AbsoluteWidth", ("Source.Width", 50f), ("Target.Width", 10f));

        _sut.DoVariableReferenceReaction(component, null, "Width", component.DefaultState!, "Source.Width", trySave: false);

        // pins current behavior; see model doc, inconsistency 2
        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(10f);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ApplyReferencesToElement_RowReadsOnlyTheElementsOwnInstances_IsNotApplied(bool useExpressions)
    {
        ComponentSave component = Build(useExpressions, "Width = Source.Width", ("Source.Width", 50f), ("Target.Width", 10f));

        _sut.ApplyReferencesToElement(component, trySave: false);

        // pins current behavior: this entry point re-applies the states that read the element from outside
        component.DefaultState!.GetValueRecursive("Target.Width").ShouldBe(10f);
    }
}
