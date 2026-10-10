using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Services.Dialogs;
using Gum.Avalonia.Tests.VariableGrid;
using Shouldly;

namespace Gum.Avalonia.Tests.EndToEnd;

/// <summary>
/// Acceptance scenario for <c>@</c>, <c>%</c> and <c>Index</c> in variable references (#5629): a
/// ruler whose notches get their height, label and width from one row each, typed once into the
/// first notch and then copied. Every step goes through the tool's own gestures (Variables tab,
/// Ctrl+C / Ctrl+V, Alt+Up, Delete, Ctrl+Z).
/// </summary>
[Trait("Category", "EndToEnd")]
public class VariableReferenceRulerScenarioTests
{
    private const string HeightRow = "Height = @Index % 4 == 0 ? 30 : 15";
    private const string LabelRow = "Text = @Index * 5";
    private const string WidthRow = "Width = @Height / 3";

    [AvaloniaFact]
    public void Ruler_NotchesDriveHeightLabelAndWidthFromTheirPosition_ThroughCopyReorderDeleteAndUndo()
    {
        using ProjectTreeHarness tree = new ProjectTreeHarness();
        ComponentSave ruler = tree.Project.AddComponent("Ruler");
        InstanceSave first = tree.Project.AddInstance(ruler, "Notch0", "Text");
        tree.Click(tree.NodeFor(ruler));
        tree.Grid.PressToggle("ChildrenLayout", ChildrenLayout.LeftToRightStack);

        // The rows are typed once, on the first notch.
        tree.Click(tree.NodeFor(first));
        tree.Grid.TypeLinesAndApply("VariableReferences", HeightRow, LabelRow, WidthRow);
        AssertNotch(Ruler(tree), "Notch0", position: 0);

        // Copying the last notch five times makes a six-notch ruler. A copy lands right after its source,
        // carries the rows verbatim and is evaluated at its own position.
        for (int i = 1; i < 6; i++)
        {
            tree.Click(tree.NodeFor(Ruler(tree).Instances.Last()));
            tree.Press(Key.C, PhysicalKey.C, RawInputModifiers.Control);
            tree.Press(Key.V, PhysicalKey.V, RawInputModifiers.Control);
        }

        Ruler(tree).Instances.Select(instance => instance.Name)
            .ShouldBe(new[] { "Notch0", "Notch1", "Notch2", "Notch3", "Notch4", "Notch5" });
        AssertRuler(Ruler(tree));
        foreach (InstanceSave notch in Ruler(tree).Instances)
        {
            ReferenceRows(Ruler(tree), notch.Name).ShouldBe(new[] { HeightRow, LabelRow, WidthRow });
        }

        // Moving a notch up one place swaps its position with its neighbour, so both are re-evaluated.
        tree.Click(tree.NodeFor(Ruler(tree).Instances.Last()));
        tree.Press(Key.Up, PhysicalKey.ArrowUp, RawInputModifiers.Alt);
        Ruler(tree).Instances.Select(instance => instance.Name)
            .ShouldBe(new[] { "Notch0", "Notch1", "Notch2", "Notch3", "Notch5", "Notch4" });
        AssertRuler(Ruler(tree));

        // Deleting the first notch shifts every other notch down one place.
        tree.Click(tree.NodeFor(Ruler(tree).Instances.First()));
        tree.Dialogs.AnswerNext<DeleteOptionsDialogViewModel>(_ => true);
        tree.Press(Key.Delete, PhysicalKey.Delete);
        Ruler(tree).Instances.Select(instance => instance.Name)
            .ShouldBe(new[] { "Notch1", "Notch2", "Notch3", "Notch5", "Notch4" });
        AssertRuler(Ruler(tree));
        AssertRuler(tree.Grid.ReadSaved(Ruler(tree)));

        tree.Undo();
        Ruler(tree).Instances.Count.ShouldBe(6);
        AssertRuler(Ruler(tree));

        tree.AssertOracles();
    }

    /// <summary>Every notch of <paramref name="ruler"/> holds the values its row gives at its position.</summary>
    private static void AssertRuler(ComponentSave ruler)
    {
        for (int position = 0; position < ruler.Instances.Count; position++)
        {
            AssertNotch(ruler, ruler.Instances[position].Name, position);
        }
    }

    private static void AssertNotch(ComponentSave ruler, string name, int position)
    {
        float expectedHeight = position % 4 == 0 ? 30f : 15f;
        ruler.Instances[position].Name.ShouldBe(name);
        VariableGridHarness.StoredValue(ruler, name + ".Height").ShouldBe(expectedHeight, $"{name} at position {position}");
        VariableGridHarness.StoredValue(ruler, name + ".Text").ShouldBe((position * 5).ToString(), $"{name} at position {position}");
        VariableGridHarness.StoredValue(ruler, name + ".Width").ShouldBe(expectedHeight / 3f, $"{name} at position {position}");
    }

    private static List<string> ReferenceRows(ElementSave element, string instanceName) =>
        element.GetDefaultStateOrThrow().GetVariableListSave(instanceName + ".VariableReferences")?.ValueAsIList?.Cast<string>()
            .Select(line => string.Join(" = ", line.Split('=', 2).Select(side => side.Trim())))
            .ToList() ?? new List<string>();

    private static ComponentSave Ruler(ProjectTreeHarness tree) =>
        tree.Project.Project.Components.Single(component => component.Name == "Ruler");
}
