using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// <c>ResumeLayout(recursive: true)</c> lays each element out about once (#7239), and ends with the layout
/// the same changes give when nothing was suspended.
/// </summary>
public class ResumeLayoutRecursiveTests : BaseTestClass
{
    private static (ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner) MakeList(int rows)
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.Absolute;
        parent.Width = 500;
        parent.HeightUnits = DimensionUnitType.Absolute;
        parent.Height = 300;

        ContainerRuntime clip = new();
        clip.Name = "Clip";
        clip.WidthUnits = DimensionUnitType.RelativeToParent;
        clip.Width = 0;
        clip.HeightUnits = DimensionUnitType.RelativeToParent;
        clip.Height = 0;
        parent.Children.Add(clip);

        ContainerRuntime inner = new();
        inner.WidthUnits = DimensionUnitType.RelativeToParent;
        inner.Width = 0;
        inner.HeightUnits = DimensionUnitType.RelativeToChildren;
        inner.Height = 0;
        clip.Children.Add(inner);

        for (int i = 0; i < rows; i++)
        {
            inner.Children.Add(MakeRow());
        }

        parent.UpdateLayout();
        return (parent, clip, inner);
    }

    private static ContainerRuntime MakeRow()
    {
        ContainerRuntime row = new();
        row.WidthUnits = DimensionUnitType.Absolute;
        row.Width = 48;
        row.HeightUnits = DimensionUnitType.Absolute;
        row.Height = 32;
        return row;
    }

    private static int Count(Action action)
    {
        int before = GraphicalUiElement.UpdateLayoutCallCount;
        action();
        return GraphicalUiElement.UpdateLayoutCallCount - before;
    }

    private static int CountElements(GraphicalUiElement element)
    {
        int count = 1;
        foreach (GraphicalUiElement child in element.Children)
        {
            count += CountElements(child);
        }
        return count;
    }

    [Theory]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(40)]
    public void ResumeRecursive_ShouldLayOutEachElementAboutOnce(int rows)
    {
        (ContainerRuntime parent, ContainerRuntime clip, _) = MakeList(rows);

        int calls = Count(() =>
        {
            parent.SuspendLayout(recursive: true);
            clip.Width -= 1;
            parent.ResumeLayout(recursive: true);
        });

        // 67, 232 and 862 for 10, 20 and 40 rows before #7239.
        calls.ShouldBeLessThanOrEqualTo(2 * CountElements(parent));
    }

    private static ContainerRuntime MakeChain(int depth)
    {
        ContainerRuntime root = new();
        root.WidthUnits = DimensionUnitType.Absolute;
        root.Width = 800;
        root.HeightUnits = DimensionUnitType.Absolute;
        root.Height = 600;

        ContainerRuntime parent = root;
        for (int i = 0; i < depth; i++)
        {
            ContainerRuntime level = new();
            level.WidthUnits = DimensionUnitType.RelativeToChildren;
            level.Width = 0;
            level.HeightUnits = DimensionUnitType.RelativeToChildren;
            level.Height = 0;
            parent.Children.Add(level);

            TextRuntime text = new();
            text.Text = "Text";
            level.Children.Add(text);

            parent = level;
        }

        root.UpdateLayout();
        return root;
    }

    [Theory]
    [InlineData(5)]
    [InlineData(40)]
    public void ResumeRecursive_ShouldCheckEachElementFontAboutOnce(int depth)
    {
        ContainerRuntime root = MakeChain(depth);
        int elements = CountElements(root);

        int before = GraphicalUiElement.FontCheckCount;
        root.SuspendLayout(recursive: true);
        root.ResumeLayout(recursive: true);
        int checks = GraphicalUiElement.FontCheckCount - before;

        // 46 and 1761 for depths 5 and 40 before #7732: each element was checked once per ancestor.
        checks.ShouldBeLessThanOrEqualTo(2 * elements);
    }

    [Fact]
    public void ResumeRecursive_ShouldLoadDirtyDescendantFontsBeforeLayingOutParent()
    {
        ContainerRuntime root = MakeChain(3);
        GraphicalUiElement level = root.Children[0];
        TextRuntime text = (TextRuntime)level.Children[0];

        root.SuspendLayout(recursive: true);
        text.FontSize = 40;
        text.IsFontDirty.ShouldBeTrue();

        int calls = Count(() => root.ResumeLayout(recursive: true));

        text.IsFontDirty.ShouldBeFalse();
        // Pinned on main before the change: the font loads first, so the sized parents lay out once each
        // rather than again when the font arrives.
        calls.ShouldBeLessThanOrEqualTo(CountElements(root));
    }

    [Theory]
    [InlineData(10)]
    [InlineData(40)]
    public void ApplyState_ShouldLayOutEachElementAboutOnce(int rows)
    {
        (ContainerRuntime parent, _, _) = MakeList(rows);
        StateSave narrow = new StateSave { Name = "Narrow" };
        narrow.Variables.Add(new VariableSave { Name = "Clip.Width", Type = "float", Value = -1f, SetsValue = true });

        int calls = Count(() => parent.ApplyState(narrow));

        calls.ShouldBeLessThanOrEqualTo(2 * CountElements(parent));
    }

    [Fact]
    public void ResumeRecursive_ShouldKeepHiddenChildLayoutPendingUntilShown()
    {
        (ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner) = MakeList(5);
        GraphicalUiElement hidden = inner.Children[2];
        hidden.Visible = false;

        parent.SuspendLayout(recursive: true);
        clip.Width -= 20;
        hidden.Width = 77;
        parent.ResumeLayout(recursive: true);

        hidden.Visible = true;
        hidden.AbsoluteWidth.ShouldBe(77);
        inner.AbsoluteWidth.ShouldBe(480);
    }

    [Fact]
    public void ResumeRecursive_ShouldKeepSuspensionTheUserSetOnChildOutsideTheResume()
    {
        (ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner) = MakeList(3);
        GraphicalUiElement row = inner.Children[1];

        row.SuspendLayout();
        parent.SuspendLayout(recursive: true);
        clip.Width -= 20;
        parent.ResumeLayout(recursive: true);

        // A recursive resume ends every suspension under it, the user's included.
        row.Width = 90;
        row.AbsoluteWidth.ShouldBe(90);
    }

    [Theory]
    [InlineData(10)]
    [InlineData(40)]
    public void ScrollViewerBarShowing_ShouldLayOutEachElementAboutOnce(int rows)
    {
        Gum.Forms.Controls.ScrollViewer viewer = new();
        foreach (StateSave state in viewer.Visual.Categories[Gum.Forms.Controls.ScrollViewer.ScrollBarVisibilityCategoryName].States)
        {
            bool vertical = state.Name == "VerticalScrollVisible" || state.Name == "BothScrollVisible";
            state.Apply = null;
            state.Variables.Add(new VariableSave { Name = "ClipContainerContainer.Width", Type = "float", Value = vertical ? -24f : 0f, SetsValue = true });
        }
        viewer.Width = 300;
        viewer.Height = 4000;
        for (int i = 0; i < rows; i++)
        {
            viewer.InnerPanel.Children.Add(MakeRow());
        }
        viewer.Visual.UpdateLayout();

        int calls = Count(() => viewer.VerticalScrollBarVisibility = Gum.Forms.Controls.ScrollBarVisibility.Visible);

        // 86 and 881 for 10 and 40 rows before #7239.
        calls.ShouldBeLessThanOrEqualTo(2 * CountElements(viewer.Visual));
    }

    #region Differential against the same changes made unsuspended

    private sealed class Scenario
    {
        public required int Rows { get; init; }
        public required bool Stacked { get; init; }
        public required bool Wraps { get; init; }
        public required bool InnerHeightFromChildren { get; init; }
        public required bool Dock { get; init; }
        public required int HiddenRow { get; init; }
        public required int Edit { get; init; }
        public required bool ApplyStateInsteadOfSet { get; init; }
        public required int Seed { get; init; }
    }

    private static Scenario MakeScenario(int seed)
    {
        Random random = new(seed);
        return new Scenario
        {
            Rows = random.Next(1, 9),
            Stacked = random.Next(2) == 0,
            Wraps = random.Next(3) == 0,
            InnerHeightFromChildren = random.Next(2) == 0,
            Dock = random.Next(4) == 0,
            HiddenRow = random.Next(3) == 0 ? random.Next(1, 9) : -1,
            Edit = random.Next(5),
            ApplyStateInsteadOfSet = random.Next(4) == 0,
            Seed = seed
        };
    }

    private static (ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner) Build(Scenario scenario)
    {
        (ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner) = MakeList(scenario.Rows);
        inner.ChildrenLayout = scenario.Stacked ? global::Gum.Managers.ChildrenLayout.TopToBottomStack : global::Gum.Managers.ChildrenLayout.Regular;
        if (scenario.Wraps)
        {
            inner.ChildrenLayout = global::Gum.Managers.ChildrenLayout.LeftToRightStack;
            inner.WrapsChildren = true;
        }
        if (!scenario.InnerHeightFromChildren)
        {
            inner.HeightUnits = DimensionUnitType.RelativeToParent;
        }
        if (scenario.Dock)
        {
            inner.Dock(Gum.Wireframe.Dock.Fill);
        }
        if (scenario.HiddenRow >= 0 && scenario.HiddenRow < inner.Children.Count)
        {
            inner.Children[scenario.HiddenRow].Visible = false;
        }
        parent.UpdateLayout();
        return (parent, clip, inner);
    }

    private static void ApplyEdit(Scenario scenario, ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner)
    {
        clip.Width -= 7;
        switch (scenario.Edit)
        {
            case 0:
                inner.Children.Add(MakeRow());
                break;
            case 1:
                if (inner.Children.Count > 0)
                {
                    inner.Children[0].Height += 5;
                }
                break;
            case 2:
                inner.StackSpacing = 3;
                break;
            case 3:
                parent.Height += 11;
                break;
            default:
                break;
        }
    }

    private static string Dump(GraphicalUiElement element)
    {
        StringBuilder builder = new();
        Dump(element, builder);
        return builder.ToString();
    }

    private static void Dump(GraphicalUiElement element, StringBuilder builder)
    {
        builder.Append(element.AbsoluteLeft).Append(',').Append(element.AbsoluteTop).Append(',')
            .Append(element.AbsoluteWidth).Append(',').Append(element.AbsoluteHeight).Append(';');
        foreach (GraphicalUiElement child in element.Children)
        {
            Dump(child, builder);
        }
    }

    [Fact]
    public void ResumeRecursive_ShouldMatchUnsuspendedLayout_ForRandomTrees()
    {
        int cases = SweepScale.Cases(300);
        for (int seed = 0; seed < cases; seed++)
        {
            Scenario scenario = MakeScenario(seed);

            (ContainerRuntime refParent, ContainerRuntime refClip, ContainerRuntime refInner) = Build(scenario);
            ApplyEdit(scenario, refParent, refClip, refInner);
            refParent.UpdateLayout();

            (ContainerRuntime parent, ContainerRuntime clip, ContainerRuntime inner) = Build(scenario);
            parent.SuspendLayout(recursive: true);
            ApplyEdit(scenario, parent, clip, inner);
            parent.ResumeLayout(recursive: true);

            Dump(parent).ShouldBe(Dump(refParent), "seed " + seed);
        }
    }

    [Fact]
    public void ResumeRecursive_ShouldMatchUnsuspendedLayout_ForChildrenAddedWhileSuspended()
    {
        for (int rows = 0; rows < 6; rows++)
        {
            (ContainerRuntime refParent, _, ContainerRuntime refInner) = MakeList(2);
            for (int i = 0; i < rows; i++) refInner.Children.Add(MakeRow());
            refParent.UpdateLayout();

            (ContainerRuntime parent, _, ContainerRuntime inner) = MakeList(2);
            parent.SuspendLayout(recursive: true);
            for (int i = 0; i < rows; i++) inner.Children.Add(MakeRow());
            parent.ResumeLayout(recursive: true);

            Dump(parent).ShouldBe(Dump(refParent), "rows " + rows);
        }
    }

    #endregion
}
