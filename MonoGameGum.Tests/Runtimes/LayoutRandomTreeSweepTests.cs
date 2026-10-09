using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Xunit;
using Xunit.Abstractions;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Seeded random element trees, laid out and then edited, with two kinds of check. The golden file pins the
/// engine's exact output for every seed, so any change to layout code that moves a single element anywhere
/// in the sweep fails here. The invariant tests check properties that must hold regardless of the engine's
/// current output (a second layout changes nothing; an edited tree matches a freshly built one).
/// Set <c>GUM_UPDATE_LAYOUT_SWEEP=1</c> to rewrite the golden file and review the diff; set
/// <c>GUM_DUMP_LAYOUT_SWEEP=&lt;folder&gt;</c> to write every case's full geometry for diffing two builds.
/// </summary>
public class LayoutRandomTreeSweepTests : BaseTestClass
{
    private const int SeedCount = 300;
    private const string GoldenFileName = "LayoutSweepGolden.txt";

    private readonly ITestOutputHelper _output;

    public LayoutRandomTreeSweepTests(ITestOutputHelper output)
    {
        _output = output;
    }

    #region Random source

    /// <summary>A tiny deterministic generator so the sweep does not depend on the runtime's Random.</summary>
    private sealed class Rng
    {
        private uint _state;

        public Rng(int seed)
        {
            _state = (uint)seed * 2654435761u + 12345u;
            Next();
            Next();
        }

        public uint Next()
        {
            _state ^= _state << 13;
            _state ^= _state >> 17;
            _state ^= _state << 5;
            return _state;
        }

        public int Int(int minInclusive, int maxExclusive) => minInclusive + (int)(Next() % (uint)(maxExclusive - minInclusive));

        public bool Chance(int percent) => Int(0, 100) < percent;

        public T Pick<T>(params T[] values) => values[Int(0, values.Length)];
    }

    #endregion

    #region Spec

    private sealed class Node
    {
        public bool IsText;
        public string Text = "";
        public DimensionUnitType WidthUnits;
        public DimensionUnitType HeightUnits;
        public float Width;
        public float Height;
        public GeneralUnitType XUnits;
        public GeneralUnitType YUnits;
        public float X;
        public float Y;
        public HorizontalAlignment XOrigin;
        public VerticalAlignment YOrigin;
        public ChildrenLayout Layout;
        public float Spacing;
        public bool Wraps;
        public float? MaxWidth;
        public float? MaxHeight;
        public int GridCells;
        public bool UseFixedSize;
        public bool Visible = true;
        public bool Ignored;
        public List<Node> Children = new();
        public GraphicalUiElement Runtime = null!;
        public int SizeEvents;
        public int PositionEvents;
    }

    private static readonly DimensionUnitType[] WidthChoices =
    {
        DimensionUnitType.Absolute, DimensionUnitType.Absolute, DimensionUnitType.Absolute,
        DimensionUnitType.RelativeToChildren, DimensionUnitType.RelativeToChildren,
        DimensionUnitType.PercentageOfParent, DimensionUnitType.RelativeToParent,
        DimensionUnitType.RelativeToMaxParentOrChildren, DimensionUnitType.Ratio,
        DimensionUnitType.PercentageOfOtherDimension
    };

    private static Node Generate(Rng rng, int depth, ref int budget, bool wellFormed, ChildrenLayout parentLayout,
        DimensionUnitType parentWidthUnits, DimensionUnitType parentHeightUnits)
    {
        Node node = new Node();
        budget--;

        node.IsText = depth > 0 && rng.Chance(8);
        if (node.IsText)
        {
            node.Text = rng.Pick("Hi", "Hello world", "A longer line of text for wrapping", "x");
        }

        bool parentStacks = parentLayout == ChildrenLayout.TopToBottomStack || parentLayout == ChildrenLayout.LeftToRightStack;
        DimensionUnitType PickUnits(DimensionUnitType parentAxisUnits)
        {
            // A well-formed tree never sizes a child from a parent that is itself sized from its children.
            bool parentSizedByChildren = parentAxisUnits == DimensionUnitType.RelativeToChildren;
            while (true)
            {
                DimensionUnitType units = rng.Pick(WidthChoices);
                if (wellFormed && (units == DimensionUnitType.RelativeToMaxParentOrChildren ||
                    units == DimensionUnitType.PercentageOfOtherDimension ||
                    (units == DimensionUnitType.Ratio && (!parentStacks || parentSizedByChildren)) ||
                    (parentSizedByChildren && (units == DimensionUnitType.PercentageOfParent || units == DimensionUnitType.RelativeToParent))))
                {
                    continue;
                }
                return units;
            }
        }

        DimensionUnitType PickTextWidthUnits()
        {
            DimensionUnitType units = rng.Pick(DimensionUnitType.RelativeToChildren, DimensionUnitType.Absolute, DimensionUnitType.PercentageOfParent);
            if (wellFormed && units == DimensionUnitType.PercentageOfParent && parentWidthUnits == DimensionUnitType.RelativeToChildren)
            {
                units = DimensionUnitType.Absolute;
            }
            return units;
        }

        node.WidthUnits = node.IsText ? PickTextWidthUnits() : PickUnits(parentWidthUnits);
        do
        {
            node.HeightUnits = node.IsText
                ? rng.Pick(DimensionUnitType.RelativeToChildren, DimensionUnitType.Absolute)
                : PickUnits(parentHeightUnits);
        }
        while (node.WidthUnits == DimensionUnitType.PercentageOfOtherDimension && node.HeightUnits == DimensionUnitType.PercentageOfOtherDimension);

        node.Width = node.WidthUnits switch
        {
            DimensionUnitType.PercentageOfParent => rng.Pick(25, 50, 100),
            DimensionUnitType.PercentageOfOtherDimension => rng.Pick(50, 100, 200),
            DimensionUnitType.Ratio => rng.Pick(1, 2),
            DimensionUnitType.RelativeToParent => rng.Pick(-20, -5, 0),
            DimensionUnitType.RelativeToChildren => rng.Pick(0, 0, 6),
            DimensionUnitType.RelativeToMaxParentOrChildren => rng.Pick(0, 4),
            _ => rng.Int(1, 60)
        };
        node.Height = node.HeightUnits switch
        {
            DimensionUnitType.PercentageOfParent => rng.Pick(25, 50, 100),
            DimensionUnitType.PercentageOfOtherDimension => rng.Pick(50, 100, 200),
            DimensionUnitType.Ratio => rng.Pick(1, 2),
            DimensionUnitType.RelativeToParent => rng.Pick(-20, -5, 0),
            DimensionUnitType.RelativeToChildren => rng.Pick(0, 0, 6),
            DimensionUnitType.RelativeToMaxParentOrChildren => rng.Pick(0, 4),
            _ => rng.Int(1, 60)
        };

        node.XUnits = rng.Pick(GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromSmall,
            GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromLarge, GeneralUnitType.Percentage);
        node.YUnits = rng.Pick(GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromSmall, GeneralUnitType.PixelsFromSmall,
            GeneralUnitType.PixelsFromMiddle, GeneralUnitType.PixelsFromLarge, GeneralUnitType.Percentage);
        node.X = node.XUnits == GeneralUnitType.Percentage ? rng.Pick(0, 25, 50) : rng.Int(-10, 20);
        node.Y = node.YUnits == GeneralUnitType.Percentage ? rng.Pick(0, 25, 50) : rng.Int(-10, 20);
        node.XOrigin = rng.Pick(HorizontalAlignment.Left, HorizontalAlignment.Left, HorizontalAlignment.Center, HorizontalAlignment.Right);
        node.YOrigin = rng.Pick(VerticalAlignment.Top, VerticalAlignment.Top, VerticalAlignment.Center, VerticalAlignment.Bottom);
        node.Visible = !rng.Chance(12);
        node.Ignored = rng.Chance(10);

        if (depth == 0)
        {
            node.IsText = false;
            if (rng.Chance(60))
            {
                node.WidthUnits = DimensionUnitType.Absolute;
                node.HeightUnits = DimensionUnitType.Absolute;
                node.Width = 300;
                node.Height = 200;
            }
            else
            {
                node.WidthUnits = DimensionUnitType.RelativeToChildren;
                node.HeightUnits = DimensionUnitType.RelativeToChildren;
                node.Width = 0;
                node.Height = 0;
            }
        }

        if (!node.IsText)
        {
            node.Layout = rng.Pick(ChildrenLayout.Regular, ChildrenLayout.Regular, ChildrenLayout.TopToBottomStack,
                ChildrenLayout.TopToBottomStack, ChildrenLayout.LeftToRightStack, ChildrenLayout.LeftToRightStack,
                ChildrenLayout.AutoGridHorizontal, ChildrenLayout.AutoGridVertical);
            node.Spacing = rng.Pick(0, 0, 2, 5);
            node.GridCells = rng.Int(1, 4);
            bool stacks = node.Layout == ChildrenLayout.TopToBottomStack || node.Layout == ChildrenLayout.LeftToRightStack;
            if (stacks && rng.Chance(25))
            {
                node.Wraps = true;
                node.MaxWidth = rng.Int(40, 140);
                node.MaxHeight = rng.Int(40, 140);
            }
            node.UseFixedSize = node.Layout == ChildrenLayout.TopToBottomStack && !node.Wraps && rng.Chance(8);

            if (depth < 5)
            {
                int childCount = rng.Chance(25) ? 0 : rng.Int(1, 5);
                for (int i = 0; i < childCount && budget > 0; i++)
                {
                    node.Children.Add(Generate(rng, depth + 1, ref budget, wellFormed, node.Layout, node.WidthUnits, node.HeightUnits));
                }
            }
        }
        return node;
    }

    private static Node GenerateTree(int seed, bool wellFormed)
    {
        Rng rng = new Rng(seed);
        int budget = 45;
        Node root = Generate(rng, 0, ref budget, wellFormed, ChildrenLayout.Regular, DimensionUnitType.Absolute, DimensionUnitType.Absolute);
        root.Visible = true;
        root.Ignored = false;
        root.XUnits = GeneralUnitType.PixelsFromSmall;
        root.YUnits = GeneralUnitType.PixelsFromSmall;
        root.X = 0;
        root.Y = 0;
        root.XOrigin = HorizontalAlignment.Left;
        root.YOrigin = VerticalAlignment.Top;
        if (root.IsText)
        {
            root.IsText = false;
        }
        return root;
    }

    #endregion

    #region Build and snapshot

    private static void Apply(Node node)
    {
        GraphicalUiElement runtime = node.Runtime;
        runtime.WidthUnits = node.WidthUnits;
        runtime.HeightUnits = node.HeightUnits;
        runtime.Width = node.Width;
        runtime.Height = node.Height;
        runtime.XUnits = node.XUnits;
        runtime.YUnits = node.YUnits;
        runtime.X = node.X;
        runtime.Y = node.Y;
        runtime.XOrigin = node.XOrigin;
        runtime.YOrigin = node.YOrigin;
        runtime.Visible = node.Visible;
        runtime.IgnoredByParentSize = node.Ignored;
        if (!node.IsText)
        {
            runtime.ChildrenLayout = node.Layout;
            runtime.StackSpacing = node.Spacing;
            runtime.WrapsChildren = node.Wraps;
            runtime.MaxWidth = node.MaxWidth;
            runtime.MaxHeight = node.MaxHeight;
            runtime.UseFixedStackChildrenSize = node.UseFixedSize;
            if (node.Layout == ChildrenLayout.AutoGridHorizontal)
            {
                runtime.AutoGridHorizontalCells = node.GridCells;
                runtime.AutoGridVerticalCells = 2;
            }
            else if (node.Layout == ChildrenLayout.AutoGridVertical)
            {
                runtime.AutoGridVerticalCells = node.GridCells;
                runtime.AutoGridHorizontalCells = 2;
            }
        }
    }

    private static void CreateRuntime(Node node)
    {
        if (node.IsText)
        {
            TextRuntime text = new();
            text.Text = node.Text;
            node.Runtime = text;
        }
        else
        {
            node.Runtime = new ContainerRuntime();
        }
        node.Runtime.SizeChanged += (_, _) => node.SizeEvents++;
        node.Runtime.PositionChanged += (_, _) => node.PositionEvents++;
        Apply(node);
    }

    private static void BuildInto(Node node)
    {
        CreateRuntime(node);
        foreach (Node child in node.Children)
        {
            BuildInto(child);
            node.Runtime.AddChild(child.Runtime);
        }
    }

    private static void BuildSuspended(Node root)
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

    private static void BuildLive(Node root)
    {
        CreateRuntime(root);
        AddChildrenLive(root);
    }

    private static void AddChildrenLive(Node parent)
    {
        foreach (Node child in parent.Children)
        {
            CreateRuntime(child);
            parent.Runtime.AddChild(child.Runtime);
            AddChildrenLive(child);
        }
    }

    private static IEnumerable<Node> Walk(Node node)
    {
        yield return node;
        foreach (Node child in node.Children)
        {
            foreach (Node descendant in Walk(child))
            {
                yield return descendant;
            }
        }
    }

    private static string Format(float value)
    {
        if (float.IsNaN(value) || float.IsInfinity(value))
        {
            return value.ToString(CultureInfo.InvariantCulture);
        }
        return System.Math.Round(value, 2).ToString("0.##", CultureInfo.InvariantCulture);
    }

    /// <summary>Every node's absolute rectangle, in depth-first order. Hidden nodes are included only when asked.</summary>
    private static string Snapshot(Node root, bool includeHidden)
    {
        StringBuilder builder = new();
        void WalkNode(Node node, string path, bool visibleSoFar)
        {
            bool visible = visibleSoFar && node.Visible;
            if (visible || includeHidden)
            {
                GraphicalUiElement r = node.Runtime;
                builder.Append(path).Append(visible ? " " : " (hidden) ")
                    .Append(Format(r.AbsoluteLeft)).Append(',').Append(Format(r.AbsoluteTop)).Append(' ')
                    .Append(Format(r.AbsoluteWidth)).Append('x').Append(Format(r.AbsoluteHeight)).AppendLine();
            }
            for (int i = 0; i < node.Children.Count; i++)
            {
                WalkNode(node.Children[i], path + "." + i, visible);
            }
        }
        WalkNode(root, "r", true);
        return builder.ToString();
    }

    #endregion

    #region Edits

    private enum Edit
    {
        Resize,
        ToggleVisible,
        Spacing,
        AddChild,
        RemoveChild
    }

    /// <summary>Applies one seeded edit to both the spec and its live runtime. Returns a label for failures.</summary>
    private static string ApplyEdit(Node root, Rng rng, Edit edit)
    {
        List<Node> all = Walk(root).ToList();
        List<Node> nonRoot = all.Skip(1).ToList();
        switch (edit)
        {
            case Edit.Resize when nonRoot.Count > 0:
            {
                Node target = nonRoot[rng.Int(0, nonRoot.Count)];
                target.Width = rng.Int(1, 70);
                target.Height = rng.Int(1, 70);
                target.Runtime.Width = target.Width;
                target.Runtime.Height = target.Height;
                return "resize";
            }
            case Edit.ToggleVisible when nonRoot.Count > 0:
            {
                Node target = nonRoot[rng.Int(0, nonRoot.Count)];
                target.Visible = !target.Visible;
                target.Runtime.Visible = target.Visible;
                return "toggle visible";
            }
            case Edit.Spacing:
            {
                List<Node> containers = all.Where(n => !n.IsText).ToList();
                Node target = containers[rng.Int(0, containers.Count)];
                target.Spacing = rng.Int(0, 12);
                target.Runtime.StackSpacing = target.Spacing;
                return "spacing";
            }
            case Edit.AddChild:
            {
                List<Node> containers = all.Where(n => !n.IsText).ToList();
                Node target = containers[rng.Int(0, containers.Count)];
                Node added = new Node
                {
                    WidthUnits = DimensionUnitType.Absolute,
                    HeightUnits = DimensionUnitType.Absolute,
                    Width = rng.Int(5, 50),
                    Height = rng.Int(5, 50),
                    Layout = ChildrenLayout.Regular
                };
                int index = rng.Int(0, target.Children.Count + 1);
                target.Children.Insert(index, added);
                CreateRuntime(added);
                target.Runtime.Children.Insert(index, added.Runtime);
                return "add child";
            }
            case Edit.RemoveChild when nonRoot.Count > 0:
            {
                Node target = nonRoot[rng.Int(0, nonRoot.Count)];
                Node parent = all.First(n => n.Children.Contains(target));
                parent.Children.Remove(target);
                parent.Runtime.RemoveChild(target.Runtime);
                return "remove child";
            }
            default:
                return "none";
        }
    }

    private static readonly Edit[] EditSequence =
    {
        Edit.Resize, Edit.ToggleVisible, Edit.Spacing, Edit.AddChild, Edit.RemoveChild, Edit.Resize
    };

    #endregion

    #region Sweep driver

    private sealed record CaseResult(
        int Seed,
        string Initial,
        string AfterSecondLayout,
        string BuiltLive,
        List<(string label, string incremental, string fresh)> Edits,
        List<string> Events,
        List<string> Partials,
        string SuspendedEdits,
        string SuspendedFresh);

    private static CaseResult RunCase(int seed, bool wellFormed)
    {
        try
        {
            return RunCaseUnguarded(seed, wellFormed);
        }
        catch (Exception exception)
        {
            GraphicalUiElement.IsAllLayoutSuspended = false;
            string firstFrame = (exception.StackTrace?.Split('\n').FirstOrDefault()?.Trim() ?? "").Split(" in ")[0];
            string crash = $"EXCEPTION {exception.GetType().Name} {firstFrame}";
            return new CaseResult(seed, crash, crash, crash, new List<(string, string, string)>(),
                new List<string>(), new List<string>(), crash, crash);
        }
    }

    private static CaseResult RunCaseUnguarded(int seed, bool wellFormed)
    {
        Node root = GenerateTree(seed, wellFormed);
        BuildSuspended(root);
        string initial = Snapshot(root, includeHidden: true);

        root.Runtime.UpdateLayout();
        string afterSecond = Snapshot(root, includeHidden: true);

        Node liveRoot = GenerateTree(seed, wellFormed);
        BuildLive(liveRoot);
        liveRoot.Runtime.UpdateLayout();
        string builtLive = Snapshot(liveRoot, includeHidden: false);

        List<string> events = new() { EventCounts(root) };
        List<(string, string, string)> edits = new();
        Rng editRng = new Rng(seed + 100000);
        foreach (Edit edit in EditSequence)
        {
            string label = ApplyEdit(root, editRng, edit);
            string incremental = Snapshot(root, includeHidden: false);
            string incrementalAll = Snapshot(root, includeHidden: true);

            Node fresh = CloneSpec(root);
            BuildSuspended(fresh);
            string freshSnapshot = Snapshot(fresh, includeHidden: false);
            edits.Add((label, incremental + "ALL\n" + incrementalAll, freshSnapshot));
            events.Add(EventCounts(root));
        }

        // The same edits made while everything is suspended must end where a fresh build of the result does.
        Node suspended = GenerateTree(seed, wellFormed);
        BuildSuspended(suspended);
        Rng suspendedRng = new Rng(seed + 100000);
        suspended.Runtime.SuspendLayout(recursive: true);
        foreach (Edit edit in EditSequence)
        {
            ApplyEdit(suspended, suspendedRng, edit);
        }
        suspended.Runtime.ResumeLayout(recursive: true);
        string suspendedEdits = Snapshot(suspended, includeHidden: false);
        Node suspendedFresh = CloneSpec(suspended);
        BuildSuspended(suspendedFresh);
        string suspendedFreshSnapshot = Snapshot(suspendedFresh, includeHidden: false);

        // A resize laid out only to a limited depth.
        List<string> partials = new();
        for (int depth = 0; depth <= 3; depth++)
        {
            Node partial = GenerateTree(seed, wellFormed);
            BuildSuspended(partial);
            List<Node> nonRoot = Walk(partial).Skip(1).ToList();
            if (nonRoot.Count > 0)
            {
                Node target = nonRoot[seed % nonRoot.Count];
                GraphicalUiElement.IsAllLayoutSuspended = true;
                target.Runtime.Width = target.Width + 7;
                GraphicalUiElement.IsAllLayoutSuspended = false;
                partial.Runtime.UpdateLayout(true, depth);
            }
            partials.Add(Snapshot(partial, includeHidden: true));
        }

        return new CaseResult(seed, initial, afterSecond, builtLive, edits, events, partials, suspendedEdits, suspendedFreshSnapshot);
    }

    /// <summary>Copies the spec tree (not the runtimes) so it can be built from scratch.</summary>
    private static Node CloneSpec(Node source)
    {
        Node copy = new Node
        {
            IsText = source.IsText,
            Text = source.Text,
            WidthUnits = source.WidthUnits,
            HeightUnits = source.HeightUnits,
            Width = source.Width,
            Height = source.Height,
            XUnits = source.XUnits,
            YUnits = source.YUnits,
            X = source.X,
            Y = source.Y,
            XOrigin = source.XOrigin,
            YOrigin = source.YOrigin,
            Layout = source.Layout,
            Spacing = source.Spacing,
            Wraps = source.Wraps,
            MaxWidth = source.MaxWidth,
            MaxHeight = source.MaxHeight,
            GridCells = source.GridCells,
            UseFixedSize = source.UseFixedSize,
            Visible = source.Visible,
            Ignored = source.Ignored
        };
        foreach (Node child in source.Children)
        {
            copy.Children.Add(CloneSpec(child));
        }
        return copy;
    }

    private static string Hash(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash, 0, 6).ToLowerInvariant();
    }

    private static string GoldenLines(CaseResult result)
    {
        StringBuilder builder = new();
        builder.Append(result.Seed.ToString(CultureInfo.InvariantCulture)).Append(" initial ").AppendLine(Hash(result.Initial));
        for (int i = 0; i < result.Edits.Count; i++)
        {
            builder.Append(result.Seed.ToString(CultureInfo.InvariantCulture)).Append(" edit").Append(i).Append(' ')
                .Append(result.Edits[i].label).Append(' ').AppendLine(Hash(result.Edits[i].incremental));
        }
        for (int i = 0; i < result.Events.Count; i++)
        {
            builder.Append(result.Seed.ToString(CultureInfo.InvariantCulture)).Append(" events").Append(i).Append(' ')
                .AppendLine(Hash(result.Events[i]));
        }
        for (int i = 0; i < result.Partials.Count; i++)
        {
            builder.Append(result.Seed.ToString(CultureInfo.InvariantCulture)).Append(" depth").Append(i).Append(' ')
                .AppendLine(Hash(result.Partials[i]));
        }
        builder.Append(result.Seed.ToString(CultureInfo.InvariantCulture)).Append(" suspended ")
            .AppendLine(Hash(result.SuspendedEdits));
        return builder.ToString();
    }

    private static string GoldenPath()
    {
        // The golden file lives in the source tree next to this test so a regenerated copy is a normal diff.
        string? directory = AppContext.BaseDirectory;
        while (directory != null && !File.Exists(Path.Combine(directory, "MonoGameGum.Tests.csproj")))
        {
            directory = Path.GetDirectoryName(directory);
        }
        directory.ShouldNotBeNull("could not find the MonoGameGum.Tests project folder");
        return Path.Combine(directory, "Runtimes", GoldenFileName);
    }

    private static List<CaseResult> RunWellFormedCases() => RunAllCases(wellFormed: true);

    private static List<CaseResult> RunAllCases(bool wellFormed)
    {
        List<CaseResult> results = new();
        for (int seed = 0; seed < SeedCount; seed++)
        {
            results.Add(RunCase(seed, wellFormed));
        }
        return results;
    }

    #endregion

    #region Tests

    [Fact]
    public void Sweep_ShouldMatchTheGoldenFile()
    {
        List<CaseResult> results = RunAllCases(wellFormed: false);
        string actual = string.Concat(results.Select(GoldenLines));

        string? dumpFolder = Environment.GetEnvironmentVariable("GUM_DUMP_LAYOUT_SWEEP");
        if (!string.IsNullOrEmpty(dumpFolder))
        {
            Directory.CreateDirectory(dumpFolder);
            foreach (CaseResult result in results)
            {
                StringBuilder dump = new();
                dump.AppendLine("== initial").Append(result.Initial);
                foreach ((string label, string incremental, string fresh) in result.Edits)
                {
                    dump.Append("== ").AppendLine(label).Append(incremental);
                }
                File.WriteAllText(Path.Combine(dumpFolder, $"case{result.Seed}.txt"), dump.ToString());
            }
        }

        string path = GoldenPath();
        if (Environment.GetEnvironmentVariable("GUM_UPDATE_LAYOUT_SWEEP") == "1")
        {
            File.WriteAllText(path, actual);
            return;
        }

        File.Exists(path).ShouldBeTrue($"missing {path}; set GUM_UPDATE_LAYOUT_SWEEP=1 to create it");
        string[] expectedLines = File.ReadAllText(path).Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        string[] actualLines = actual.Replace("\r\n", "\n").Split('\n', StringSplitOptions.RemoveEmptyEntries);
        List<string> differences = new();
        for (int i = 0; i < System.Math.Max(expectedLines.Length, actualLines.Length); i++)
        {
            string expected = i < expectedLines.Length ? expectedLines[i] : "<missing>";
            string got = i < actualLines.Length ? actualLines[i] : "<missing>";
            if (expected != got)
            {
                differences.Add($"expected '{expected}' but was '{got}'");
            }
        }
        differences.Count.ShouldBe(0,
            $"{differences.Count} sweep results changed, first: {string.Join("; ", differences.Take(5))}. " +
            "If the change is intended, rerun with GUM_UPDATE_LAYOUT_SWEEP=1 and review the diff.");
    }

    [Fact]
    public void Sweep_WellFormedTrees_ShouldNotThrow()
    {
        List<CaseResult> crashed = RunAllCases(wellFormed: true).Where(r => r.Initial.StartsWith("EXCEPTION")).ToList();

        crashed.Count.ShouldBe(0, string.Join("; ", crashed.Take(5).Select(r => $"seed {r.Seed}: {r.Initial}")));
    }

    [Fact]
    public void Sweep_SecondLayout_ShouldChangeNothing()
    {
        List<CaseResult> failing = RunWellFormedCases().Where(r => r.Initial != r.AfterSecondLayout).ToList();
        List<string> failures = failing.Select(r => $"seed {r.Seed}").ToList();

        _output.WriteLine($"{failures.Count} of {SeedCount}: {string.Join(", ", failures)}");
        foreach (CaseResult r in failing.Take(3))
        {
            _output.WriteLine($"--- seed {r.Seed}\n{Describe(r.Seed, wellFormed: true)}\n{FirstDifference(r.Initial, r.AfterSecondLayout)}");
        }
        failures.Count.ShouldBe(0, string.Join(", ", failures.Take(20)));
    }

    [Fact]
    public void Sweep_BuildingWithLayoutLive_ShouldMatchBuildingSuspended()
    {
        List<string> failures = RunWellFormedCases()
            .Where(r => StripHidden(r.Initial) != r.BuiltLive)
            .Select(r => $"seed {r.Seed}")
            .ToList();

        _output.WriteLine($"{failures.Count} of {SeedCount}: {string.Join(", ", failures)}");
        failures.Count.ShouldBe(0, string.Join(", ", failures.Take(20)));
    }

    [Fact]
    public void Sweep_EditsMadeWhileSuspended_ShouldMatchAFreshBuildOfTheSameTree()
    {
        List<string> failures = RunWellFormedCases()
            .Where(r => r.SuspendedEdits != r.SuspendedFresh)
            .Select(r => $"seed {r.Seed}")
            .ToList();

        _output.WriteLine($"{failures.Count} of {SeedCount}: {string.Join(", ", failures)}");
        failures.Count.ShouldBe(0, string.Join(", ", failures.Take(20)));
    }

    [Fact]
    public void Sweep_EditedTree_ShouldMatchAFreshBuildOfTheSameTree()
    {
        List<string> failures = new();
        foreach (CaseResult result in RunWellFormedCases())
        {
            for (int i = 0; i < result.Edits.Count; i++)
            {
                string incrementalVisible = result.Edits[i].incremental.Split("ALL\n")[0];
                if (incrementalVisible != result.Edits[i].fresh)
                {
                    failures.Add($"seed {result.Seed} edit{i} {result.Edits[i].label}");
                }
            }
        }

        _output.WriteLine($"{failures.Count}: {string.Join(", ", failures)}");
        failures.Count.ShouldBe(0, string.Join(", ", failures.Take(20)));
    }

    /// <summary>The spec of a seed's tree, one node per line, for reading a failure.</summary>
    private static string Describe(int seed, bool wellFormed)
    {
        StringBuilder builder = new();
        void WalkNode(Node node, string path)
        {
            builder.Append(path).Append(node.IsText ? " text" : " " + node.Layout)
                .Append(" W=").Append(node.Width).Append(' ').Append(node.WidthUnits)
                .Append(" H=").Append(node.Height).Append(' ').Append(node.HeightUnits)
                .Append(" X=").Append(node.X).Append(' ').Append(node.XUnits).Append(' ').Append(node.XOrigin)
                .Append(" Y=").Append(node.Y).Append(' ').Append(node.YUnits).Append(' ').Append(node.YOrigin)
                .Append(node.Visible ? "" : " hidden").Append(node.Ignored ? " ignored" : "")
                .Append(node.Wraps ? $" wraps max={node.MaxWidth}x{node.MaxHeight}" : "")
                .Append(node.UseFixedSize ? " fixed" : "")
                .AppendLine();
            for (int i = 0; i < node.Children.Count; i++)
            {
                WalkNode(node.Children[i], path + "." + i);
            }
        }
        WalkNode(GenerateTree(seed, wellFormed), "r");
        return builder.ToString();
    }

    private static string FirstDifference(string a, string b)
    {
        string[] linesA = a.Split('\n');
        string[] linesB = b.Split('\n');
        List<string> differences = new();
        for (int i = 0; i < System.Math.Max(linesA.Length, linesB.Length); i++)
        {
            string left = i < linesA.Length ? linesA[i] : "<missing>";
            string right = i < linesB.Length ? linesB[i] : "<missing>";
            if (left != right)
            {
                differences.Add($"{left}  ->  {right}");
            }
        }
        return string.Join("\n", differences.Take(6));
    }

    private static string EventCounts(Node root)
    {
        StringBuilder builder = new();
        int index = 0;
        foreach (Node node in Walk(root))
        {
            builder.Append(index++).Append(':').Append(node.SizeEvents).Append('/').Append(node.PositionEvents).Append(' ');
        }
        return builder.ToString();
    }

    private static string StripHidden(string snapshotWithHidden)
    {
        return string.Concat(snapshotWithHidden
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Where(line => !line.Contains("(hidden)"))
            .Select(line => line + "\n"));
    }

    #endregion
}
