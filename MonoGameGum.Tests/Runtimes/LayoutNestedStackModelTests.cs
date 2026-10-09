using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Shouldly;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Pins the geometry of chains and trees of nested content-sized stacks against an independent model, at
/// depths the existing two-level tests do not reach. The model knows only absolute leaves, content-sized
/// stacks and content-sized Regular containers, so each expected number is plain arithmetic.
/// </summary>
public class LayoutNestedStackModelTests : BaseTestClass
{
    #region Spec, build and model

    public enum Kind
    {
        Leaf,
        TopToBottom,
        LeftToRight,
        Regular
    }

    private sealed class Spec
    {
        public Kind Kind;
        public float Width;
        public float Height;
        public float Spacing;
        public bool Visible = true;
        public bool Ignored;
        public List<Spec> Children = new();
        public ContainerRuntime Runtime = null!;

        // Filled in by the model.
        public float RelativeX;
        public float RelativeY;
        public float ExpectedLeft;
        public float ExpectedTop;
        public float ExpectedWidth;
        public float ExpectedHeight;
    }

    private static Spec Leaf(float width, float height, bool visible = true, bool ignored = false) =>
        new Spec { Kind = Kind.Leaf, Width = width, Height = height, Visible = visible, Ignored = ignored };

    private static Spec Container(Kind kind, float spacing, params Spec[] children)
    {
        Spec spec = new Spec { Kind = kind, Spacing = spacing };
        spec.Children.AddRange(children);
        return spec;
    }

    private static ContainerRuntime CreateRuntime(Spec spec)
    {
        ContainerRuntime runtime = new();
        if (spec.Kind == Kind.Leaf)
        {
            runtime.WidthUnits = DimensionUnitType.Absolute;
            runtime.HeightUnits = DimensionUnitType.Absolute;
            runtime.Width = spec.Width;
            runtime.Height = spec.Height;
        }
        else
        {
            runtime.WidthUnits = DimensionUnitType.RelativeToChildren;
            runtime.HeightUnits = DimensionUnitType.RelativeToChildren;
            runtime.Width = 0;
            runtime.Height = 0;
            runtime.ChildrenLayout = spec.Kind switch
            {
                Kind.TopToBottom => ChildrenLayout.TopToBottomStack,
                Kind.LeftToRight => ChildrenLayout.LeftToRightStack,
                _ => ChildrenLayout.Regular
            };
            runtime.StackSpacing = spec.Spacing;
        }
        runtime.Visible = spec.Visible;
        runtime.IgnoredByParentSize = spec.Ignored;
        spec.Runtime = runtime;
        return runtime;
    }

    private static void BuildInto(Spec spec)
    {
        CreateRuntime(spec);
        foreach (Spec child in spec.Children)
        {
            BuildInto(child);
            spec.Runtime.AddChild(child.Runtime);
        }
    }

    /// <summary>Builds the whole tree with layout suspended, then lays it out once from the root.</summary>
    private static void BuildSuspended(Spec root)
    {
        GraphicalUiElement.IsAllLayoutSuspended = true;
        try
        {
            BuildInto(root);
        }
        finally
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
        }
        root.Runtime.UpdateLayout();
    }

    /// <summary>Builds the tree top-down with layout live, so every AddChild lays out as it goes.</summary>
    private static void BuildLive(Spec root)
    {
        CreateRuntime(root);
        AddChildrenLive(root);
    }

    private static void AddChildrenLive(Spec parent)
    {
        foreach (Spec child in parent.Children)
        {
            CreateRuntime(child);
            parent.Runtime.AddChild(child.Runtime);
            AddChildrenLive(child);
        }
    }

    private static IEnumerable<Spec> Descendants(Spec spec)
    {
        yield return spec;
        foreach (Spec child in spec.Children)
        {
            foreach (Spec descendant in Descendants(child))
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Computes every node's expected rectangle. Stacks place every visible child, ignored ones included;
    /// a container's size counts only its visible, non-ignored children.
    /// </summary>
    private static void Model(Spec root)
    {
        Measure(root);
        Place(root, 0, 0);
    }

    private static void Measure(Spec spec)
    {
        if (spec.Kind == Kind.Leaf)
        {
            spec.ExpectedWidth = spec.Width;
            spec.ExpectedHeight = spec.Height;
            return;
        }

        float cursor = 0;
        float width = 0;
        float height = 0;
        int countedChildren = 0;
        foreach (Spec child in spec.Children)
        {
            if (!child.Visible)
            {
                continue;
            }

            Measure(child);
            child.RelativeX = 0;
            child.RelativeY = 0;
            if (spec.Kind == Kind.TopToBottom)
            {
                child.RelativeY = cursor;
                cursor += child.ExpectedHeight + spec.Spacing;
            }
            else if (spec.Kind == Kind.LeftToRight)
            {
                child.RelativeX = cursor;
                cursor += child.ExpectedWidth + spec.Spacing;
            }

            if (child.Ignored)
            {
                continue;
            }

            // Along the stacking axis the size is the counted children added up with spacing between
            // them, so an ignored child takes room in the stack but not in the size.
            if (spec.Kind == Kind.TopToBottom)
            {
                height += child.ExpectedHeight + (countedChildren > 0 ? spec.Spacing : 0);
                width = System.Math.Max(width, child.ExpectedWidth);
            }
            else if (spec.Kind == Kind.LeftToRight)
            {
                width += child.ExpectedWidth + (countedChildren > 0 ? spec.Spacing : 0);
                height = System.Math.Max(height, child.ExpectedHeight);
            }
            else
            {
                width = System.Math.Max(width, child.ExpectedWidth);
                height = System.Math.Max(height, child.ExpectedHeight);
            }
            countedChildren++;
        }

        spec.ExpectedWidth = width;
        spec.ExpectedHeight = height;
    }

    private static void Place(Spec spec, float left, float top)
    {
        spec.ExpectedLeft = left;
        spec.ExpectedTop = top;
        foreach (Spec child in spec.Children.Where(c => c.Visible))
        {
            Place(child, left + child.RelativeX, top + child.RelativeY);
        }
    }

    private static void AssertMatchesModel(Spec root)
    {
        Model(root);
        foreach (Spec spec in Descendants(root))
        {
            if (!IsEffectivelyVisible(root, spec))
            {
                continue;
            }

            string name = $"{spec.Kind} at depth {Depth(root, spec)}";
            spec.Runtime.AbsoluteLeft.ShouldBe(spec.ExpectedLeft, name + " left");
            spec.Runtime.AbsoluteTop.ShouldBe(spec.ExpectedTop, name + " top");
            spec.Runtime.AbsoluteWidth.ShouldBe(spec.ExpectedWidth, name + " width");
            spec.Runtime.AbsoluteHeight.ShouldBe(spec.ExpectedHeight, name + " height");
        }
    }

    private static bool IsEffectivelyVisible(Spec root, Spec target)
    {
        bool Walk(Spec current)
        {
            if (!current.Visible)
            {
                return false;
            }
            if (current == target)
            {
                return true;
            }
            return current.Children.Any(Walk);
        }
        return Walk(root);
    }

    private static int Depth(Spec root, Spec target)
    {
        int Walk(Spec current, int depth)
        {
            if (current == target)
            {
                return depth;
            }
            foreach (Spec child in current.Children)
            {
                int found = Walk(child, depth + 1);
                if (found >= 0)
                {
                    return found;
                }
            }
            return -1;
        }
        return Walk(root, 0);
    }

    /// <summary>
    /// A chain of content-sized stacks <paramref name="depth"/> levels deep. Every level holds two leaves
    /// before the next level and one after it, so the trailing leaf is only placed correctly if the nested
    /// level's final size reached the stack.
    /// </summary>
    private static Spec Chain(int depth, Kind[] kindByLevel, float spacing)
    {
        Spec? next = null;
        for (int level = depth - 1; level >= 0; level--)
        {
            Kind kind = kindByLevel[level % kindByLevel.Length];
            Spec current = Container(kind, spacing,
                Leaf(10 + level, 5 + level),
                Leaf(7 + level, 9 + level));
            if (next != null)
            {
                current.Children.Add(next);
            }
            current.Children.Add(Leaf(3 + level, 4 + level));
            next = current;
        }
        return next!;
    }

    #endregion

    #region Chains

    public static IEnumerable<object[]> ChainCases()
    {
        Kind[][] patterns =
        {
            new[] { Kind.TopToBottom },
            new[] { Kind.LeftToRight },
            new[] { Kind.TopToBottom, Kind.LeftToRight },
            new[] { Kind.LeftToRight, Kind.TopToBottom },
            new[] { Kind.TopToBottom, Kind.Regular },
        };
        foreach (Kind[] pattern in patterns)
        {
            foreach (int depth in new[] { 1, 2, 3, 4, 6, 8 })
            {
                foreach (float spacing in new[] { 0f, 2f })
                {
                    yield return new object[] { string.Join(",", pattern), depth, spacing };
                }
            }
        }
    }

    private static Kind[] ParsePattern(string pattern) =>
        pattern.Split(',').Select(p => System.Enum.Parse<Kind>(p)).ToArray();

    [Theory]
    [MemberData(nameof(ChainCases))]
    public void ContentSizedChain_ShouldPlaceAndSizeEveryLevel_WhenBuiltSuspended(string pattern, int depth, float spacing)
    {
        Spec root = Chain(depth, ParsePattern(pattern), spacing);

        BuildSuspended(root);

        AssertMatchesModel(root);
    }

    [Theory]
    [MemberData(nameof(ChainCases))]
    public void ContentSizedChain_ShouldPlaceAndSizeEveryLevel_WhenBuiltLive(string pattern, int depth, float spacing)
    {
        Spec root = Chain(depth, ParsePattern(pattern), spacing);

        BuildLive(root);

        AssertMatchesModel(root);
    }

    [Theory]
    [MemberData(nameof(ChainCases))]
    public void ContentSizedChain_ShouldBeStable_WhenLaidOutAgain(string pattern, int depth, float spacing)
    {
        Spec root = Chain(depth, ParsePattern(pattern), spacing);
        BuildSuspended(root);

        root.Runtime.UpdateLayout();
        root.Runtime.UpdateLayout();

        AssertMatchesModel(root);
    }

    [Fact]
    public void ContentSizedTree_ShouldPlaceEveryBranch_WhenEachLevelHasTwoNestedStacks()
    {
        Spec Level(int depth, Kind kind)
        {
            Kind inner = kind == Kind.TopToBottom ? Kind.LeftToRight : Kind.TopToBottom;
            Spec spec = Container(kind, 1, Leaf(6 + depth, 4 + depth));
            if (depth > 0)
            {
                spec.Children.Add(Level(depth - 1, inner));
                spec.Children.Add(Leaf(2 + depth, 3 + depth));
                spec.Children.Add(Level(depth - 1, inner));
            }
            spec.Children.Add(Leaf(5, 5));
            return spec;
        }
        Spec root = Level(4, Kind.TopToBottom);

        BuildSuspended(root);

        AssertMatchesModel(root);
    }

    #endregion

    #region Ignored and invisible children

    [Theory]
    [InlineData(Kind.TopToBottom)]
    [InlineData(Kind.LeftToRight)]
    public void ContentSizedChain_ShouldStackIgnoredLeaf_ButNotCountItInSize(Kind kind)
    {
        // The ignored leaf is the largest thing at every level, so it would dominate the size if counted.
        Spec Level(int depth)
        {
            Spec spec = Container(kind, 0, Leaf(8, 8), Leaf(50, 50, ignored: true));
            if (depth > 0)
            {
                spec.Children.Add(Level(depth - 1));
            }
            spec.Children.Add(Leaf(4, 4));
            return spec;
        }
        Spec root = Level(4);

        BuildSuspended(root);

        AssertMatchesModel(root);
    }

    [Theory]
    [InlineData(Kind.TopToBottom)]
    [InlineData(Kind.LeftToRight)]
    public void ContentSizedChain_ShouldSkipInvisibleLeaves_AtEveryLevel(Kind kind)
    {
        Spec Level(int depth)
        {
            Spec spec = Container(kind, 2, Leaf(40, 40, visible: false), Leaf(8, 8));
            if (depth > 0)
            {
                spec.Children.Add(Level(depth - 1));
            }
            spec.Children.Add(Leaf(30, 30, visible: false));
            spec.Children.Add(Leaf(4, 4));
            return spec;
        }
        Spec root = Level(4);

        BuildSuspended(root);

        AssertMatchesModel(root);
    }

    #endregion

    #region Edits after layout

    private static Spec EditableChain(out Spec deepestLeaf, out Spec middleLevel)
    {
        Spec root = Chain(6, new[] { Kind.TopToBottom, Kind.LeftToRight }, 2);
        BuildSuspended(root);
        AssertMatchesModel(root);

        Spec deepest = root;
        while (deepest.Children.Any(c => c.Kind != Kind.Leaf))
        {
            deepest = deepest.Children.First(c => c.Kind != Kind.Leaf);
        }
        deepestLeaf = deepest.Children[0];
        middleLevel = Descendants(root).Where(s => s.Kind != Kind.Leaf).ElementAt(3);
        return root;
    }

    [Fact]
    public void DeepLeaf_ShouldReflowEveryAncestorAndLaterSibling_WhenResizedLargerSmallerAndBack()
    {
        Spec root = EditableChain(out Spec deepLeaf, out _);

        deepLeaf.Height = 60;
        deepLeaf.Width = 70;
        deepLeaf.Runtime.Height = 60;
        deepLeaf.Runtime.Width = 70;
        AssertMatchesModel(root);

        deepLeaf.Height = 1;
        deepLeaf.Width = 1;
        deepLeaf.Runtime.Height = 1;
        deepLeaf.Runtime.Width = 1;
        AssertMatchesModel(root);

        deepLeaf.Height = 5;
        deepLeaf.Width = 10;
        deepLeaf.Runtime.Height = 5;
        deepLeaf.Runtime.Width = 10;
        AssertMatchesModel(root);
    }

    [Fact]
    public void DeepLeaf_ShouldReflowEveryAncestor_WhenHiddenAndShown()
    {
        Spec root = EditableChain(out Spec deepLeaf, out _);

        deepLeaf.Visible = false;
        deepLeaf.Runtime.Visible = false;
        AssertMatchesModel(root);

        deepLeaf.Visible = true;
        deepLeaf.Runtime.Visible = true;
        AssertMatchesModel(root);
    }

    [Fact]
    public void MiddleLevel_ShouldReflowEveryAncestorAndLaterSibling_WhenHiddenAndShown()
    {
        Spec root = EditableChain(out _, out Spec middle);

        middle.Visible = false;
        middle.Runtime.Visible = false;
        AssertMatchesModel(root);

        middle.Visible = true;
        middle.Runtime.Visible = true;
        AssertMatchesModel(root);
    }

    [Fact]
    public void MiddleLevel_ShouldReflow_WhenSpacingChanges()
    {
        Spec root = EditableChain(out _, out Spec middle);

        middle.Spacing = 9;
        middle.Runtime.StackSpacing = 9;
        AssertMatchesModel(root);

        middle.Spacing = 0;
        middle.Runtime.StackSpacing = 0;
        AssertMatchesModel(root);
    }

    [Fact]
    public void MiddleLevel_ShouldReflow_WhenALeafIsAddedAndRemoved()
    {
        Spec root = EditableChain(out _, out Spec middle);

        Spec added = Leaf(25, 35);
        middle.Children.Insert(1, added);
        CreateRuntime(added);
        middle.Runtime.Children.Insert(1, added.Runtime);
        AssertMatchesModel(root);

        middle.Children.Remove(added);
        middle.Runtime.RemoveChild(added.Runtime);
        AssertMatchesModel(root);
    }

    [Fact]
    public void MiddleLevel_ShouldReflow_WhenLeafIsMarkedIgnoredAndUnignored()
    {
        Spec root = EditableChain(out _, out Spec middle);
        Spec leaf = middle.Children[0];

        leaf.Ignored = true;
        leaf.Runtime.IgnoredByParentSize = true;
        AssertMatchesModel(root);

        leaf.Ignored = false;
        leaf.Runtime.IgnoredByParentSize = false;
        AssertMatchesModel(root);
    }

    [Fact]
    public void Chain_ShouldMatchUnsuspendedResult_WhenEditsAreMadeUnderSuspension()
    {
        Spec root = EditableChain(out Spec deepLeaf, out Spec middle);

        root.Runtime.SuspendLayout(recursive: true);
        deepLeaf.Height = 44;
        deepLeaf.Runtime.Height = 44;
        middle.Visible = false;
        middle.Runtime.Visible = false;
        middle.Spacing = 6;
        middle.Runtime.StackSpacing = 6;
        root.Runtime.ResumeLayout(recursive: true);

        AssertMatchesModel(root);
    }

    #endregion
}
