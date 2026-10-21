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
/// in the sweep fails here. Hashes are taken over LF-normalized text so the golden file is portable across OSes.
/// The invariant tests check properties that must hold regardless of the engine's
/// current output (a second layout changes nothing; an edited tree matches a freshly built one).
/// Set <c>GUM_UPDATE_LAYOUT_SWEEP=1</c> to rewrite the golden file and review the diff; set
/// <c>GUM_DUMP_LAYOUT_SWEEP=&lt;folder&gt;</c> to write every case's full geometry for diffing two builds.
/// </summary>
public partial class LayoutRandomTreeSweepTests : BaseTestClass
{
    private const int SeedCount = 300;

    /// <summary>Seeds for the invariant sweeps: all of <see cref="SeedCount"/> in CI, a tenth locally. The golden file test always runs all of them.</summary>
    private static int Seeds => SweepScale.Cases(SeedCount);
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
        public bool IsScreen;
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
        else if (node.IsScreen)
        {
            node.Runtime = new GraphicalUiElement();
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
            if (node.IsScreen)
            {
                // A Screen has no renderable, so it has no Children collection; its elements are attached as ParentGue.
                child.Runtime.ParentGue = node.Runtime;
            }
            else
            {
                node.Runtime.AddChild(child.Runtime);
            }
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

    /// <summary>Everything a case reports, in full.</summary>
    private static string Fingerprint(CaseResult result)
    {
        StringBuilder builder = new();
        builder.Append(result.Initial).Append("|").Append(result.AfterSecondLayout).Append("|").Append(result.BuiltLive).Append("|");
        foreach ((string label, string incremental, string fresh) in result.Edits)
        {
            builder.Append(label).Append('|').Append(incremental).Append('|').Append(fresh).Append('|');
        }
        builder.Append(string.Join("|", result.Events)).Append(string.Join("|", result.Partials));
        builder.Append(result.SuspendedEdits).Append('|').Append(result.SuspendedFresh);
        return builder.ToString();
    }

    private static string Hash(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text.Replace("\r\n", "\n")));
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

    private static List<CaseResult> RunWellFormedCases() => RunAllCases(wellFormed: true, Seeds);

    private static List<CaseResult> RunAllCases(bool wellFormed, int seeds = SeedCount)
    {
        List<CaseResult> results = new();
        for (int seed = 0; seed < seeds; seed++)
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

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Sweep_SkippingRepeatedChildLayouts_ShouldMatchLayingEveryChildOutEveryTime(bool wellFormed)
    {
        // Seeds past the golden file's. Laying a child out again is cut down to placing it when the first
        // layout changed nothing, which has to give exactly what laying it out in full gives: the same
        // geometry, edits, partial layouts and events, in trees that settle and in ones that do not.
        const int firstSeed = SeedCount;
        int lastSeed = SeedCount + SweepScale.Cases(500);
        List<string> differences = new();
        for (int seed = firstSeed; seed < lastSeed; seed++)
        {
            GraphicalUiElement.SkipRepeatedChildLayouts = false;
            CaseResult everyTime;
            try
            {
                everyTime = RunCase(seed, wellFormed);
            }
            finally
            {
                GraphicalUiElement.SkipRepeatedChildLayouts = true;
            }
            CaseResult skipping = RunCase(seed, wellFormed);

            if (Fingerprint(everyTime) != Fingerprint(skipping))
            {
                differences.Add($"seed {seed}");
            }
        }

        differences.Count.ShouldBe(0, string.Join(", ", differences.Take(20)));
    }

    private static Node Spec(ChildrenLayout layout, DimensionUnitType widthUnits, float width, DimensionUnitType heightUnits, float height,
        params Node[] children)
    {
        Node node = new Node { Layout = layout, WidthUnits = widthUnits, Width = width, HeightUnits = heightUnits, Height = height };
        node.Children.AddRange(children);
        return node;
    }

    [Fact]
    public void CutDownSweepTree_PlacedWhileItsSizeWasStillBeingMeasured_ShouldReportTheSameEventsWhetherOrNotRepeatedChildLayoutsAreSkipped()
    {
        // Seed 5367, cut down, laid out a second time. A layout measures an element twice, and for R0 (sized
        // from its children and its parent) the two measures differ. R0 is placed between them, so where it
        // goes follows the first size, and placing it again at the final size would move it. A visit placed at
        // a size other than its final one is therefore not one to skip, and R0 reports its moves as often as before.
        Node Tree()
        {
            Node r000 = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfParent, 25, DimensionUnitType.Absolute, 33);
            Node r00 = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute, 45, DimensionUnitType.RelativeToMaxParentOrChildren, 4, r000);
            r00.Wraps = true;
            r00.MaxWidth = 97;
            r00.MaxHeight = 108;
            Node r0 = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToMaxParentOrChildren, 0, r00);
            r0.YOrigin = VerticalAlignment.Center;
            Node r1 = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Ratio, 1, DimensionUnitType.PercentageOfParent, 100);
            Node root = Spec(ChildrenLayout.AutoGridHorizontal, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren, 0, r0, r1);
            root.GridCells = 1;
            return root;
        }

        GraphicalUiElement.SkipRepeatedChildLayouts = false;
        string everyTime;
        try
        {
            Node full = Tree();
            BuildSuspended(full);
            full.Runtime.UpdateLayout();
            everyTime = Snapshot(full, includeHidden: true) + EventCounts(full);
        }
        finally
        {
            GraphicalUiElement.SkipRepeatedChildLayouts = true;
        }
        Node skipping = Tree();
        BuildSuspended(skipping);
        skipping.Runtime.UpdateLayout();

        (Snapshot(skipping, includeHidden: true) + EventCounts(skipping)).ShouldBe(everyTime);
    }

    [Theory]
    [InlineData(false, 6, 1.5f)]
    [InlineData(true, 8, 2f)]
    public void CutDownSweepTree_StackSizedToChildrenHoldingAPercentOfItself_ShouldIgnoreThatChildAndStayPutOnASecondLayout(
        bool useFixedStackChildrenSize, float expectedStackHeight, float expectedChildHeight)
    {
        // Seed 273, cut down. The first child is a percent of a stack that is sized from its children, so the
        // stack ignores it, as it ignores any child sized from its parent. With UseFixedStackChildrenSize the
        // stack measured only its first child and read that child's height anyway, so its height followed its
        // own previous height and grew on every layout (8, 12, 14, ...). The ignored first child stands in for
        // the others as zero, leaving only the spacing between them.
        Node child = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.PercentageOfParent, 25);
        Node sibling = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0);
        Node stack = Spec(ChildrenLayout.TopToBottomStack, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren, 6, child, sibling);
        stack.UseFixedSize = useFixedStackChildrenSize;
        stack.Spacing = 2;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, stack);

        BuildSuspended(root);
        string first = Snapshot(root, includeHidden: true);
        root.Runtime.UpdateLayout();

        stack.Runtime.AbsoluteHeight.ShouldBe(expectedStackHeight);
        child.Runtime.AbsoluteHeight.ShouldBe(expectedChildHeight);
        Snapshot(root, includeHidden: true).ShouldBe(first);
    }

    /// <summary>Lays the tree out suspended, then again, and returns the first result after checking the second matches it.</summary>
    private static string LayOutTwiceAndExpectNoChange(Node root)
    {
        BuildSuspended(root);
        string first = Snapshot(root, includeHidden: true);
        root.Runtime.UpdateLayout();
        Snapshot(root, includeHidden: true).ShouldBe(first);
        return first;
    }

    private static Node Absolute(float width, float height) =>
        Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, width, DimensionUnitType.Absolute, height);

    [Fact]
    public void CutDownSweepTree_ParentHeightSetByAnotherChildAfterTheMaxChildWasLaidOut_ShouldLayTheMaxChildOutAgainAtTheFinalHeight()
    {
        // Seed 220, cut down. The parent is sized from its children, and its height comes from the first child
        // (a percent of the child's width, itself relative to the parent). The second child is the larger of
        // the parent's height and its own content, and was laid out while the parent was still 0 high.
        Node first = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToParent, -5, DimensionUnitType.PercentageOfOtherDimension, 100);
        Node second = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToMaxParentOrChildren, 0);
        Node parent = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren, 0, first, second);

        LayOutTwiceAndExpectNoChange(parent);

        (parent.Runtime.AbsoluteHeight, second.Runtime.AbsoluteHeight).ShouldBe((5f, 5f));
    }

    [Fact]
    public void CutDownSweepTree_ChildIgnoredByParentSizeThatReadsTheParentWidth_ShouldBeLaidOutAgainWhenTheParentShrinks()
    {
        // Seed 241, cut down. A child ignored by its parent's size can still be the larger of the parent's
        // width and its content. It was laid out against a width the parent then measured smaller.
        Node ignored = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToMaxParentOrChildren, 0, DimensionUnitType.Absolute, 35);
        ignored.Ignored = true;
        ignored.YOrigin = VerticalAlignment.Center;
        Node sized = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren, 0);
        Node stack = Spec(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToChildren, 6, DimensionUnitType.Absolute, 29, ignored, sized);
        stack.Wraps = true;
        stack.MaxWidth = 64;
        stack.MaxHeight = 64;
        stack.Spacing = 5;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, stack);

        LayOutTwiceAndExpectNoChange(root);

        (stack.Runtime.AbsoluteWidth, ignored.Runtime.AbsoluteWidth).ShouldBe((6f, 6f));
    }

    [Fact]
    public void CutDownSweepTree_WrappingStackWithABottomOrigin_ShouldBePlacedAtItsFinalHeight()
    {
        // Seed 78, cut down. The stack is sized to its children and wraps, so its height is only known after
        // its children wrap. It was placed from a bottom origin before that, at the height it had then.
        Node ratio = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToParent, -20, DimensionUnitType.Ratio, 1);
        Node other = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToParent, -20, DimensionUnitType.Absolute, 0);
        Node stack = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute, 57, DimensionUnitType.RelativeToChildren, 0, ratio, other);
        stack.Wraps = true;
        stack.MaxWidth = 44;
        stack.MaxHeight = 92;
        stack.Spacing = 5;
        stack.YOrigin = VerticalAlignment.Bottom;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, stack);

        LayOutTwiceAndExpectNoChange(root);

        (stack.Runtime.AbsoluteHeight, stack.Runtime.AbsoluteTop).ShouldBe((5f, -5f));
    }

    [Fact]
    public void CutDownSweepTree_WrappingStackWithAMaxChildAndARelativeToParentChild_ShouldNotSizeItselfFromTheMaxChildsOwnWidth()
    {
        // Seed 241, cut down. A child that is the larger of its parent's width and its content reads the width
        // the wrapping stack measures from its columns. Counting that child's full width made each layout add
        // the stack's own previous width to itself.
        Node max = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToMaxParentOrChildren, 0, DimensionUnitType.Absolute, 0);
        Node relative = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToParent, -5, DimensionUnitType.Ratio, 2);
        Node stack = Spec(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToChildren, 6, DimensionUnitType.Absolute, 0,
            max, relative, Absolute(0, 10));
        stack.Wraps = true;
        stack.MaxWidth = 64;
        stack.MaxHeight = 64;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, stack);

        LayOutTwiceAndExpectNoChange(root);

        (stack.Runtime.AbsoluteWidth, max.Runtime.AbsoluteWidth).ShouldBe((6f, 6f));
    }

    [Fact]
    public void CutDownSweepTree_WrappingRowWithAMaxChildAndARelativeToParentChild_ShouldNotSizeItselfFromTheMaxChildsOwnHeight()
    {
        // Seed 241 with the axes swapped: a wrapping left-to-right stack sized to its children in height.
        Node max = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToMaxParentOrChildren, 0);
        Node relative = Spec(ChildrenLayout.Regular, DimensionUnitType.Ratio, 2, DimensionUnitType.RelativeToParent, -5);
        Node stack = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute, 0, DimensionUnitType.RelativeToChildren, 6,
            max, relative, Absolute(10, 0));
        stack.Wraps = true;
        stack.MaxWidth = 64;
        stack.MaxHeight = 64;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, stack);

        LayOutTwiceAndExpectNoChange(root);

        (stack.Runtime.AbsoluteHeight, max.Runtime.AbsoluteHeight).ShouldBe((6f, 6f));
    }

    [Fact]
    public void CutDownSweepTree_TextWithAPercentWidthInAContentSizedParent_ShouldBePlacedAtTheFinalParentSize()
    {
        // Seed 272, cut down. The text's width is a percent of a parent sized from its children and it is
        // anchored to the bottom, so its height and where it goes follow a parent size that is measured last.
        Node text = new Node
        {
            IsText = true,
            Text = "Hello world",
            WidthUnits = DimensionUnitType.PercentageOfParent,
            Width = 50,
            HeightUnits = DimensionUnitType.RelativeToChildren,
            Height = 6,
            YUnits = GeneralUnitType.PixelsFromLarge,
            Y = 9,
            YOrigin = VerticalAlignment.Bottom
        };
        Node sibling = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfOtherDimension, 100, DimensionUnitType.Absolute, 52);
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren, 0, text, sibling);

        LayOutTwiceAndExpectNoChange(root);

        // The text hangs 9 below its parent's bottom edge from a bottom origin, so its top is at 0.
        (root.Runtime.AbsoluteWidth, root.Runtime.AbsoluteHeight).ShouldBe((52f, 81f));
        (text.Runtime.AbsoluteTop, text.Runtime.AbsoluteHeight).ShouldBe((0f, 90f));
    }

    [Fact]
    public void CutDownSweepTree_WidthAndHeightEachDerivedFromTheOtherThroughTheChildren_ShouldIgnoreTheChildAndStayPutOnASecondLayout()
    {
        // Seed 291, cut down. The parent's width is its child's width, which is 200% of the child's height,
        // which is the larger of the parent and the child's own content plus 4, and the parent's height is 100%
        // of its width. No size satisfies that, so each layout used to grow it. The parent now ignores such a
        // child on that axis, like any child sized from its parent.
        Node child = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToMaxParentOrChildren, 4);
        Node parent = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.PercentageOfOtherDimension, 100, child);
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, parent);

        LayOutTwiceAndExpectNoChange(root);

        (parent.Runtime.AbsoluteWidth, parent.Runtime.AbsoluteHeight).ShouldBe((0f, 0f));
        (child.Runtime.AbsoluteWidth, child.Runtime.AbsoluteHeight).ShouldBe((8f, 4f));
    }

    [Fact]
    public void CutDownSweepTree_HeightAndWidthEachDerivedFromTheOtherThroughTheChildren_ShouldIgnoreTheChildAndStayPutOnASecondLayout()
    {
        // Seed 289, cut down: seed 291 with the axes swapped.
        Node child = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToMaxParentOrChildren, 4, DimensionUnitType.PercentageOfOtherDimension, 100);
        Node parent = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToChildren, 0, child);
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 0, DimensionUnitType.Absolute, 0, parent);

        LayOutTwiceAndExpectNoChange(root);

        (parent.Runtime.AbsoluteWidth, parent.Runtime.AbsoluteHeight).ShouldBe((0f, 0f));
        (child.Runtime.AbsoluteWidth, child.Runtime.AbsoluteHeight).ShouldBe((4f, 4f));
    }


    [Fact]
    public void CutDownSweepTree_WrappingStackWhoseSuspendedChildrenAreRepositionedAfterItResizes_ShouldNotThrowAndShouldMatchAFreshBuild()
    {
        // Seed 705, cut down, and not specific to ill-formed trees. Three children are centered in their line, so
        // their position reads the line's size. While layout is suspended the stack narrows (each child now
        // wraps) and the children grow, then everything resumes. The stack lays out first, while its children
        // are still suspended and keep their old lines, and repositions them against the new line sizes.
        // That moved the second child to a new line that no child had measured, and the third child then summed
        // a line size that did not exist.
        Node Tree(float stackWidth, float childHeight)
        {
            Node Child()
            {
                Node child = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 30, DimensionUnitType.Absolute, childHeight);
                child.YUnits = GeneralUnitType.PixelsFromMiddle;
                return child;
            }
            Node stack = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute, stackWidth, DimensionUnitType.Absolute, 100,
                Child(), Child(), Child());
            stack.Wraps = true;
            return Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 200, DimensionUnitType.Absolute, 200, stack);
        }

        Node edited = Tree(stackWidth: 100, childHeight: 10);
        BuildSuspended(edited);
        edited.Runtime.SuspendLayout(recursive: true);
        edited.Children[0].Runtime.Width = 40;
        foreach (Node child in edited.Children[0].Children)
        {
            child.Runtime.Height = 20;
        }
        edited.Runtime.ResumeLayout(recursive: true);

        Node fresh = Tree(stackWidth: 40, childHeight: 20);
        BuildSuspended(fresh);

        Snapshot(edited, includeHidden: true).ShouldBe(Snapshot(fresh, includeHidden: true));
    }


    [Fact]
    public void CutDownSweepTree_ChildSizedByARatioWhoseHeightIsAPercentOfItsWidth_ShouldBeIgnoredByAContentSizedParentWhoseWidthFollowsItsHeight()
    {
        // Seed 848, cut down. Like seed 291 but the child's width is a Ratio, which also comes from the parent's
        // size. The parent's width is 200% of its height, its height is the larger of its own content and its
        // parent, and the child's content is 200% of the child's width: the height would have to be four times
        // itself, so each layout grew it until it was infinite.
        Node child = Spec(ChildrenLayout.Regular, DimensionUnitType.Ratio, 2, DimensionUnitType.PercentageOfOtherDimension, 200);
        Node parent = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToMaxParentOrChildren, 0, child);
        child.YUnits = GeneralUnitType.PixelsFromMiddle;
        child.Y = 15;
        Node outer = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToChildren, 0, parent);
        outer.XUnits = GeneralUnitType.Percentage;
        Node outer2 = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToChildren, 0, outer);
        outer2.YUnits = GeneralUnitType.PixelsFromMiddle;
        outer2.Y = 10;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToChildren, 0, outer2);

        LayOutTwiceAndExpectNoChange(root);

        (parent.Runtime.AbsoluteWidth, parent.Runtime.AbsoluteHeight).ShouldBe((0f, 0f));
        (child.Runtime.AbsoluteWidth, child.Runtime.AbsoluteHeight).ShouldBe((0f, 0f));
    }


    [Fact]
    public void CutDownSweepTree_FixedSizeStackWhoseFirstChildIsTheLargerOfTheStackAndItsContent_ShouldMeasureThatChildByItsContentAndStayPutOnASecondLayout()
    {
        // Seed 742, cut down. With UseFixedStackChildrenSize the stack measured its first child by its laid-out
        // height. That child is the larger of the stack's height and its content, so its height includes the
        // stack's own previous height, and the stack grew with it on every layout. The measure that does not
        // use the fixed size already counts such a child by its content only.
        Node first = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToMaxParentOrChildren, 0);
        first.Y = 3;
        Node second = Absolute(10, 10);
        Node stack = Spec(ChildrenLayout.TopToBottomStack, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToChildren, 0, first, second);
        stack.UseFixedSize = true;
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.Absolute, 10, stack);

        LayOutTwiceAndExpectNoChange(root);

        (stack.Runtime.AbsoluteHeight, first.Runtime.AbsoluteHeight).ShouldBe((0f, 0f));
    }


    [Fact]
    public void CutDownSweepTree_FixedSizeStackSizedToChildrenOnBothAxes_ShouldMeasureEveryChildOnTheFirstLayout()
    {
        // Seed 985, cut down, and not specific to ill-formed trees. A fixed-size stack lays out only its first
        // child before it is measured, which is enough for its height, but its width is the widest child.
        // The second child, centered with an offset, was measured before it had a width or a position, so
        // the stack was 10 wide until the next layout made it 24.
        Node first = Absolute(10, 10);
        Node second = Absolute(10, 10);
        second.XUnits = GeneralUnitType.PixelsFromMiddle;
        second.X = 2;
        Node stack = Spec(ChildrenLayout.TopToBottomStack, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.RelativeToChildren, 0, first, second);
        stack.UseFixedSize = true;

        LayOutTwiceAndExpectNoChange(stack);

        (stack.Runtime.AbsoluteWidth, stack.Runtime.AbsoluteHeight).ShouldBe((24f, 20f));
    }


    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void CutDownSweepTree_ChangingTheAxisAnotherAxisIsAPercentOf_ShouldMoveTheElementAsAFreshBuildWould(bool widthFollowsHeight)
    {
        // Seeds 433, 897 and 924, cut down, and not specific to ill-formed trees. A child whose width is a percent
        // of its height and whose origin is its center sits half its width to the left. Setting its height lays
        // out that axis only, but the width follows it, and the position was left at the old width.
        Node Tree(float height)
        {
            Node child = widthFollowsHeight
                ? Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfOtherDimension, 50, DimensionUnitType.Absolute, height)
                : Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, height, DimensionUnitType.PercentageOfOtherDimension, 50);
            child.XOrigin = HorizontalAlignment.Center;
            child.YOrigin = VerticalAlignment.Center;
            return Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 100, DimensionUnitType.Absolute, 100, child);
        }

        Node edited = Tree(10);
        BuildSuspended(edited);
        if (widthFollowsHeight)
        {
            edited.Children[0].Runtime.Height = 40;
        }
        else
        {
            edited.Children[0].Runtime.Width = 40;
        }
        Node fresh = Tree(40);
        BuildSuspended(fresh);

        Snapshot(edited, includeHidden: true).ShouldBe(Snapshot(fresh, includeHidden: true));
    }

    [Fact]
    public void CutDownSweepTree_HidingContentInASiblingOfARatioElement_ShouldResizeTheRatioElementsDescendantsAsAFreshBuildWould()
    {
        // Seed 14, cut down, and well-formed. Hiding the 50 wide content of A makes A, which is sized from it,
        // narrower, so the Ratio element B takes the room that frees up. Hiding reaches A one level up, so B's
        // width is reached with a short depth, and B's grandchildren read B's width two levels down.
        Node Tree(bool contentVisible)
        {
            Node content = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 50, DimensionUnitType.Absolute, 10);
            content.Visible = contentVisible;
            Node a = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.Absolute, 15, content);
            Node percentChild = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfParent, 50, DimensionUnitType.Absolute, 5);
            Node b0 = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.Absolute, 22, percentChild);
            Node b = Spec(ChildrenLayout.Regular, DimensionUnitType.Ratio, 1, DimensionUnitType.Absolute, 50, b0);
            return Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute, 100, DimensionUnitType.Absolute, 65, a, b);
        }

        Node edited = Tree(contentVisible: true);
        BuildSuspended(edited);
        edited.Children[0].Children[0].Visible = false;
        edited.Children[0].Children[0].Runtime.Visible = false;
        Node fresh = Tree(contentVisible: false);
        BuildSuspended(fresh);

        Snapshot(edited, includeHidden: false).ShouldBe(Snapshot(fresh, includeHidden: false));
    }

    [Fact]
    public void CutDownSweepTree_DivergentCycleThroughAnIntermediateContainer_ShouldLayOutToFiniteSizes()
    {
        // Seed 848, cut down. The cycle runs through M, so the engine does not detect it: the parent's height is
        // the height of M, which is 5 plus the height of I, which is 200% of I's width, a Ratio share of M's
        // width, which is the parent's width, which is 200% of its height. The height would be four times itself
        // plus 5, so each layout grew it, past what a float holds after about 60 layouts. Circular input has no right answer,
        // but no size may be infinite.
        Node inner = Spec(ChildrenLayout.Regular, DimensionUnitType.Ratio, 1, DimensionUnitType.PercentageOfOtherDimension, 200);
        Node middle = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.RelativeToChildren, 5, inner);
        Node parent = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfOtherDimension, 200, DimensionUnitType.RelativeToChildren, 0, middle);
        Node root = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 10, DimensionUnitType.RelativeToChildren, 0, parent);

        BuildSuspended(root);
        for (int layout = 0; layout < 100; layout++)
        {
            root.Runtime.UpdateLayout();
        }

        foreach (Node node in new[] { root, parent, middle, inner })
        {
            float.IsFinite(node.Runtime.AbsoluteWidth).ShouldBeTrue($"width {node.Runtime.AbsoluteWidth}");
            float.IsFinite(node.Runtime.AbsoluteHeight).ShouldBeTrue($"height {node.Runtime.AbsoluteHeight}");
        }
    }

    [Fact]
    public void CutDownSweepTree_HidingContentInASiblingOfARatioElementUnderAScreen_ShouldResizeTheRatioElementsDescendantsAsAFreshBuildWould()
    {
        // The tree of the test above with a Screen (no contained renderable) as the stack, so its children are
        // updated by the branch of UpdateChildren that has no renderable (#5962).
        Node Tree(bool contentVisible)
        {
            Node content = Spec(ChildrenLayout.Regular, DimensionUnitType.Absolute, 50, DimensionUnitType.Absolute, 10);
            content.Visible = contentVisible;
            Node a = Spec(ChildrenLayout.Regular, DimensionUnitType.RelativeToChildren, 0, DimensionUnitType.Absolute, 15, content);
            Node percentChild = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfParent, 50, DimensionUnitType.Absolute, 5);
            Node b0 = Spec(ChildrenLayout.Regular, DimensionUnitType.PercentageOfParent, 100, DimensionUnitType.Absolute, 22, percentChild);
            Node b = Spec(ChildrenLayout.Regular, DimensionUnitType.Ratio, 1, DimensionUnitType.Absolute, 50, b0);
            Node screen = Spec(ChildrenLayout.LeftToRightStack, DimensionUnitType.Absolute, 100, DimensionUnitType.Absolute, 65, a, b);
            screen.IsScreen = true;
            return screen;
        }

        Node edited = Tree(contentVisible: true);
        BuildSuspended(edited);
        edited.Children[0].Children[0].Visible = false;
        edited.Children[0].Children[0].Runtime.Visible = false;
        Node fresh = Tree(contentVisible: false);
        BuildSuspended(fresh);

        Snapshot(edited, includeHidden: false).ShouldBe(Snapshot(fresh, includeHidden: false));
    }

    // Ill-formed seeds past the golden file's are held to the same checks. These are the ones that are not, each
    // with why. A cycle that runs through an intermediate container (not directly between a parent and its
    // child) is circular input with no defined result (#5959): it may grow without bound, approach its fixed
    // point a little on each layout, or depend on a wrapped text. The engine does not detect these.
    private static readonly Dictionary<int, string> IllFormedSeedsNotHeld = new()
    {
        [848] = "circular input: units are circular through an intermediate container and the size diverges, written as 0 once it is not finite (#5959)",
        [1185] = "circular input: a wrapped text's height depends on its width, which follows the container's height (#5959)",
        [1271] = "circular input: units are circular through a sibling and settle toward a fixed point over repeated layouts (#5959)"
    };

    [Fact]
    public void Sweep_IllFormedTrees_ShouldNotThrowAndShouldSettleOnASecondLayout()
    {
        // Ill-formed trees are held to the invariants that do not depend on the input making sense: no
        // exception, a second layout changes nothing, building with layout live or suspended, or making
        // the edits suspended, gives the same result as a fresh build, and so does editing the tree live.
        int lastSeed = SweepScale.Cases(1300);
        List<string> failures = new();
        for (int seed = 0; seed < lastSeed; seed++)
        {
            if (IllFormedSeedsNotHeld.ContainsKey(seed))
            {
                continue;
            }

            CaseResult result = RunCase(seed, wellFormed: false);
            if (result.Initial.StartsWith("EXCEPTION"))
            {
                failures.Add($"seed {seed} threw");
                continue;
            }
            if (result.Initial != result.AfterSecondLayout)
            {
                failures.Add($"seed {seed} second layout");
            }
            if (StripHidden(result.Initial) != result.BuiltLive)
            {
                failures.Add($"seed {seed} live");
            }
            if (result.SuspendedEdits != result.SuspendedFresh)
            {
                failures.Add($"seed {seed} suspended edits");
            }
            for (int i = 0; i < result.Edits.Count; i++)
            {
                if (result.Edits[i].incremental.Split("ALL\n")[0] != result.Edits[i].fresh)
                {
                    failures.Add($"seed {seed} edit{i} {result.Edits[i].label}");
                    break;
                }
            }
        }

        failures.Count.ShouldBe(0, string.Join(", ", failures.Take(20)));
    }

    [Fact]
    public void Sweep_WellFormedTrees_ShouldNotThrow()
    {
        List<CaseResult> crashed = RunWellFormedCases().Where(r => r.Initial.StartsWith("EXCEPTION")).ToList();

        crashed.Count.ShouldBe(0, string.Join("; ", crashed.Take(5).Select(r => $"seed {r.Seed}: {r.Initial}")));
    }

    [Fact]
    public void Sweep_SecondLayout_ShouldChangeNothing()
    {
        List<CaseResult> failing = RunWellFormedCases().Where(r => r.Initial != r.AfterSecondLayout).ToList();
        List<string> failures = failing.Select(r => $"seed {r.Seed}").ToList();

        _output.WriteLine($"{failures.Count} of {Seeds}: {string.Join(", ", failures)}");
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

        _output.WriteLine($"{failures.Count} of {Seeds}: {string.Join(", ", failures)}");
        failures.Count.ShouldBe(0, string.Join(", ", failures.Take(20)));
    }

    [Fact]
    public void Sweep_EditsMadeWhileSuspended_ShouldMatchAFreshBuildOfTheSameTree()
    {
        List<string> failures = RunWellFormedCases()
            .Where(r => r.SuspendedEdits != r.SuspendedFresh)
            .Select(r => $"seed {r.Seed}")
            .ToList();

        _output.WriteLine($"{failures.Count} of {Seeds}: {string.Join(", ", failures)}");
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
