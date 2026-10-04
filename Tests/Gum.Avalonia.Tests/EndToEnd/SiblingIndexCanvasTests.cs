using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Expressions;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Gum.Wireframe;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// <c>Index</c> against the tool's real wireframe tree, which is what the Variables tab hands the
/// evaluator as its live root. Same conventions as <see cref="CanvasScenarioTests"/>.
/// </summary>
[Trait("Category", "EndToEnd")]
public class SiblingIndexCanvasTests
{
    [SkippableFact]
    public void Index_OfInstancesInTheShownElement_IsThePositionAmongSiblings()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = canvas.Project.AddComponent("Button");
            canvas.AddInstance(button, "Item0", "Container", x: 0, y: 0);
            canvas.AddInstance(button, "Item1", "Container", x: 0, y: 40);
            canvas.AddInstance(button, "Item2", "Container", x: 0, y: 80);
            GraphicalUiElement? liveRoot = canvas.Wireframe.GetRepresentation(button);
            liveRoot.ShouldNotBeNull();

            object?[] indexes = new[] { "Item0", "Item1", "Item2" }
                .Select(name => EvaluatedSyntax.FromSyntaxNode(
                    SyntaxFactory.ParseExpression(name + ".Index"), button.DefaultState!, liveRoot: liveRoot)?.Value)
                .ToArray();

            indexes.ShouldBe([0, 1, 2]);
        });
    }

    private static ComponentSave BuildThreeIndexDrivenItems(CanvasHarness canvas, out InstanceSave[] items)
    {
        ComponentSave button = canvas.Project.AddComponent("Button");
        TestAppBuilder.Services.GetRequiredService<ISelectedState>().SelectedElement = button;
        items =
        [
            canvas.AddInstance(button, "Item0", "Container", x: 0, y: 0),
            canvas.AddInstance(button, "Item1", "Container", x: 0, y: 40),
            canvas.AddInstance(button, "Item2", "Container", x: 0, y: 80),
        ];
        IVariableReferenceLogic logic = TestAppBuilder.Services.GetRequiredService<IVariableReferenceLogic>();
        foreach (InstanceSave item in items)
        {
            VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = item.Name + ".VariableReferences" };
            list.Value.Add("Y=@Index * 40");
            button.DefaultState!.VariableLists.Add(list);
            logic.DoVariableReferenceReaction(button, item, "VariableReferences", button.DefaultState,
                item.Name + ".VariableReferences", trySave: false);
        }
        return button;
    }

    [SkippableFact]
    public void Reorder_OfAnIndexDrivenSibling_ReappliesTheirIndexRows()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = BuildThreeIndexDrivenItems(canvas, out InstanceSave[] items);
            ISelectedState selected = TestAppBuilder.Services.GetRequiredService<ISelectedState>();
            selected.SelectedInstance = items[0];

            TestAppBuilder.Services.GetRequiredService<Gum.Logic.IReorderLogic>().MoveSelectedInstanceToFront();

            // order is now Item1, Item2, Item0
            button.DefaultState!.GetValue("Item1.Y").ShouldBe(0f);
            button.DefaultState.GetValue("Item2.Y").ShouldBe(40f);
            button.DefaultState.GetValue("Item0.Y").ShouldBe(80f);
        });
    }

    [SkippableFact]
    public void Delete_OfAnIndexDrivenSibling_ReappliesTheRemainingIndexRows()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = BuildThreeIndexDrivenItems(canvas, out InstanceSave[] items);

            TestAppBuilder.Services.GetRequiredService<Gum.Managers.IDeleteLogic>().RemoveInstance(items[0], button);

            button.DefaultState!.GetValue("Item1.Y").ShouldBe(0f);
            button.DefaultState.GetValue("Item2.Y").ShouldBe(40f);
        });
    }

    [SkippableFact]
    public void ParentChange_OfAnIndexDrivenSibling_ReappliesTheRemainingSiblingsIndexRows()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = BuildThreeIndexDrivenItems(canvas, out InstanceSave[] items);
            canvas.AddInstance(button, "Holder", "Container", x: 200, y: 0);

            button.DefaultState!.SetValue("Item0.Parent", "Holder", "string");
            TestAppBuilder.Services.GetRequiredService<ISetVariableLogic>()
                .PropertyValueChanged("Parent", null, items[0], button.DefaultState);

            // root siblings are now Item1, Item2, Holder
            button.DefaultState.GetValue("Item1.Y").ShouldBe(0f);
            button.DefaultState.GetValue("Item2.Y").ShouldBe(40f);
        });
    }

    [SkippableFact]
    public void Paste_OfAnIndexDrivenInstance_EvaluatesThePastedInstanceAtItsNewPosition()
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ComponentSave button = BuildThreeIndexDrivenItems(canvas, out InstanceSave[] items);
            ISelectedState selected = TestAppBuilder.Services.GetRequiredService<ISelectedState>();
            selected.SelectedInstance = items[0];
            Gum.Logic.ICopyPasteLogic copyPaste = TestAppBuilder.Services.GetRequiredService<Gum.Logic.ICopyPasteLogic>();

            copyPaste.OnCopy(Gum.Logic.CopyType.InstanceOrElement);
            copyPaste.OnPaste(Gum.Logic.CopyType.InstanceOrElement);

            InstanceSave pasted = button.Instances.Last();
            button.Instances.Count.ShouldBe(4);
            button.DefaultState!.GetValue(pasted.Name + ".Y").ShouldBe(120f, "the pasted copy is the fourth sibling");
            button.DefaultState.GetValue("Item0.Y").ShouldBe(0f);
        });
    }

    [SkippableTheory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void AtIndexRow_AppliedThroughTheVariableReferenceReaction_ResolvesAgainstTheShownElement(bool inScreen, bool inHolder)
    {
        Skip.IfNot(CanvasHarness.CanRun, CanvasHarness.SkipReason);
        CanvasHarness.OnUiThread(() =>
        {
            using CanvasHarness canvas = new CanvasHarness();
            ElementSave button = inScreen ? canvas.Project.AddScreen("Menu") : canvas.Project.AddComponent("Button");
            TestAppBuilder.Services.GetRequiredService<ISelectedState>().SelectedElement = button;
            if (inHolder)
            {
                canvas.AddInstance(button, "Holder", "Container", x: 0, y: 0);
            }
            InstanceSave[] items =
            [
                canvas.AddInstance(button, "Item0", "Container", x: 0, y: 0),
                canvas.AddInstance(button, "Item1", "Container", x: 0, y: 40),
                canvas.AddInstance(button, "Item2", "Container", x: 0, y: 80),
            ];
            if (inHolder)
            {
                foreach (InstanceSave item in items)
                {
                    button.DefaultState!.SetValue(item.Name + ".Parent", "Holder", "string");
                }
                canvas.Wireframe.RefreshAll(forceLayout: true);
            }
            IVariableReferenceLogic logic = TestAppBuilder.Services.GetRequiredService<IVariableReferenceLogic>();

            foreach (InstanceSave item in items)
            {
                VariableListSave<string> list = new VariableListSave<string> { Type = "string", Name = item.Name + ".VariableReferences" };
                list.Value.Add("Y=@Index * 40");
                button.DefaultState!.VariableLists.Add(list);

                logic.DoVariableReferenceReaction(button, item, "VariableReferences", button.DefaultState,
                    item.Name + ".VariableReferences", trySave: false);

                list.Value[0].ShouldBe("Y=@Index * 40", "the row must not be commented out as invalid");
            }

            button.DefaultState!.GetValue("Item0.Y").ShouldBe(0f);
            button.DefaultState.GetValue("Item1.Y").ShouldBe(40f);
            button.DefaultState.GetValue("Item2.Y").ShouldBe(80f);
        });
    }
}
