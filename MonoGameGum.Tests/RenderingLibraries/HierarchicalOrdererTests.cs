using System.Collections.Generic;
using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Unit tests for <see cref="HierarchicalOrderer"/>. Asserts that the main-pass DFS walk
/// produces the same visit order as the legacy recursive <c>Renderer.Render</c> path,
/// with explicit <see cref="DrawCommandKind.BeginClip"/> / <see cref="DrawCommandKind.EndClip"/>
/// commands bracketing any node whose <see cref="IRenderableIpso.ClipsChildren"/> is true.
/// </summary>
public class HierarchicalOrdererTests : BaseTestClass
{
    /// <summary>
    /// Minimal <see cref="IRenderableIpso"/> for orderer tests. Only the properties the orderer
    /// reads (<c>Visible</c>, <c>Children</c>, <c>ClipsChildren</c>, <c>IsRenderTarget</c>) are
    /// meaningful; positional fields are stubs.
    /// </summary>
    private sealed class FakeRenderable : IRenderableIpso
    {
        public FakeRenderable(string name)
        {
            Name = name;
            Visible = true;
            Children = new ObservableCollection<IRenderableIpso>();
        }

        public string Name { get; set; }
        public bool Visible { get; set; }
        public bool ClipsChildren { get; set; }
        public bool IsRenderTarget { get; set; }
        public ObservableCollection<IRenderableIpso> Children { get; }

        public IRenderableIpso? Parent { get; set; }
        IVisible? IVisible.Parent => Parent;
        public bool AbsoluteVisible => Visible;

        public void SetParentDirect(IRenderableIpso? newParent) => Parent = newParent;

        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Rotation { get; set; }
        public bool FlipHorizontal { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public object? Tag { get; set; }

        public int Alpha => 255;
        public ColorOperation ColorOperation => ColorOperation.Modulate;
        public Gum.BlendState BlendState => Gum.BlendState.NonPremultiplied;
        public bool Wrap => false;

        public string BatchKey => string.Empty;
        public void Render(ISystemManagers managers) { }
        public void PreRender() { }
        public void StartBatch(ISystemManagers managers) { }
        public void EndBatch(ISystemManagers managers) { }
    }

    private static FakeRenderable AddChild(FakeRenderable parent, string name)
    {
        FakeRenderable child = new FakeRenderable(name);
        child.SetParentDirect(parent);
        parent.Children.Add(child);
        return child;
    }

    private static Layer BuildLayer(params IRenderableIpso[] renderables)
    {
        Layer layer = new Layer();
        foreach (IRenderableIpso renderable in renderables)
        {
            layer.Add(renderable);
        }
        return layer;
    }

    private static List<string> Describe(List<DrawCommand> commands)
    {
        List<string> result = new List<string>();
        foreach (DrawCommand cmd in commands)
        {
            string targetName = ((FakeRenderable)cmd.Target).Name;
            result.Add($"{cmd.Kind}:{targetName}");
        }
        return result;
    }

    [Fact]
    public void BuildDrawList_ClippingRenderable_BracketsChildrenWithBeginEndClip()
    {
        FakeRenderable container = new FakeRenderable("container");
        container.ClipsChildren = true;
        FakeRenderable inner = AddChild(container, "inner");

        Layer layer = BuildLayer(container);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "BeginClip:container",
            "DrawRenderable:container",
            "DrawRenderable:inner",
            "EndClip:container",
        });
    }

    [Fact]
    public void BuildDrawList_DepthFirstWalk_VisitsRenderablesInPreOrder()
    {
        FakeRenderable a = new FakeRenderable("a");
        FakeRenderable a1 = AddChild(a, "a1");
        FakeRenderable a2 = AddChild(a, "a2");
        AddChild(a1, "a1a");
        FakeRenderable b = new FakeRenderable("b");

        Layer layer = BuildLayer(a, b);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "DrawRenderable:a",
            "DrawRenderable:a1",
            "DrawRenderable:a1a",
            "DrawRenderable:a2",
            "DrawRenderable:b",
        });
    }

    [Fact]
    public void BuildDrawList_InvisibleRenderable_SkipsRenderableAndChildren()
    {
        FakeRenderable visible = new FakeRenderable("visible");
        FakeRenderable hidden = new FakeRenderable("hidden");
        hidden.Visible = false;
        AddChild(hidden, "hiddenChild");

        Layer layer = BuildLayer(visible, hidden);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[] { "DrawRenderable:visible" });
    }

    [Fact]
    public void BuildDrawList_IsRenderTargetNode_DoesNotRecurseIntoChildren()
    {
        FakeRenderable rt = new FakeRenderable("rt");
        rt.IsRenderTarget = true;
        AddChild(rt, "rtChild");

        Layer layer = BuildLayer(rt);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[] { "DrawRenderable:rt" });
    }

    [Fact]
    public void BuildDrawList_NestedClips_EmitsNestedBeginEndPairs()
    {
        FakeRenderable outer = new FakeRenderable("outer");
        outer.ClipsChildren = true;
        FakeRenderable inner = AddChild(outer, "inner");
        inner.ClipsChildren = true;
        AddChild(inner, "leaf");

        Layer layer = BuildLayer(outer);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[]
        {
            "BeginClip:outer",
            "DrawRenderable:outer",
            "BeginClip:inner",
            "DrawRenderable:inner",
            "DrawRenderable:leaf",
            "EndClip:inner",
            "EndClip:outer",
        });
    }

    [Fact]
    public void BuildDrawList_RenderUsingHierarchyFalse_DoesNotRecurse()
    {
        bool originalValue = Renderer.RenderUsingHierarchy;
        try
        {
            Renderer.RenderUsingHierarchy = false;

            FakeRenderable parent = new FakeRenderable("parent");
            AddChild(parent, "child");

            Layer layer = BuildLayer(parent);
            List<DrawCommand> commands = new List<DrawCommand>();

            HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

            Describe(commands).ShouldBe(new[] { "DrawRenderable:parent" });
        }
        finally
        {
            Renderer.RenderUsingHierarchy = originalValue;
        }
    }

    [Fact]
    public void BuildDrawList_WithPreExistingCommands_ClearsDestinationFirst()
    {
        Layer layer = BuildLayer(new FakeRenderable("only"));
        List<DrawCommand> commands = new List<DrawCommand>();
        commands.Add(new DrawCommand(DrawCommandKind.DrawRenderable, new FakeRenderable("stale")));

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands);

        Describe(commands).ShouldBe(new[] { "DrawRenderable:only" });
    }
}
