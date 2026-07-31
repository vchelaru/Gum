using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
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
    /// meaningful; positional fields are stubs. Also implements <see cref="IWrappedText"/> so
    /// camera-driven cull tests (#4144) can set <see cref="WrappedTextHeight"/>; it defaults to 0
    /// and is otherwise unused, so pre-existing (no-camera) tests are unaffected.
    /// </summary>
    private sealed class FakeRenderable : IRenderableIpso, IWrappedText
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

        public float WrappedTextHeight { get; set; }
        public int? MaxNumberOfLines => null;
        public int LineHeightInPixels => 0;
        public bool IsTruncatingWithEllipsisOnLastLine => false;
        public bool IsHeightDependentOnLines { get; set; }
        public bool IsMidWordLineBreakEnabled => false;
        public float MeasureString(string text) => 0;
        public void SetNeedsRefreshToTrue() { }
        public void UpdatePreRenderDimensions() { }
        public float DescenderHeight => 0;
        public float FontScale => 1;
        public float WrappedTextWidth => 0;
        public string? RawText { get; set; }
        public string? StoredMarkupText => null;
        float? IText.Width { get => Width; set => Width = value ?? 0; }
        public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }
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
    public void BuildDrawList_Roots_CullsOffscreenSubtreeUsingSuppliedCullTestBoundsMapping()
    {
        // getScissorRectangle deliberately returns an IN-clip rectangle for "cull" -- if the
        // orderer used it (instead of getCullTestBounds) for the cull decision, "cull" would
        // wrongly survive. getCullTestBounds returns the true (far outside the margin) rectangle,
        // proving the subtree overload's cull decision is driven by getCullTestBounds, mirroring
        // the Layer overload's GetCullTestBoundsFor/GetScissorRectangleFor split (#4144, #4154).
        FakeRenderable clipContainer = new FakeRenderable("clipContainer");
        clipContainer.ClipsChildren = true;
        FakeRenderable keep = AddChild(clipContainer, "keep");
        FakeRenderable cull = AddChild(clipContainer, "cull");

        Rectangle clipRect = new Rectangle(0, 0, 100, 100);
        Rectangle insideClip = new Rectangle(10, 10, 20, 20);
        Rectangle farOutside = new Rectangle(1000, 1000, 10, 10);

        Rectangle GetScissorRectangle(IRenderableIpso r) => r == clipContainer ? clipRect : insideClip;
        Rectangle GetCullTestBounds(IRenderableIpso r) => r == cull ? farOutside : GetScissorRectangle(r);

        List<DrawCommand> commands = new List<DrawCommand>();
        HierarchicalOrderer.Instance.BuildDrawList(
            new List<IRenderableIpso> { clipContainer },
            commands,
            GetScissorRectangle,
            GetCullTestBounds);

        Describe(commands).ShouldBe(new[]
        {
            "BeginClip:clipContainer",
            "DrawRenderable:clipContainer",
            "DrawRenderable:keep",
            "EndClip:clipContainer",
        });
    }

    [Fact]
    public void BuildDrawList_Roots_DepthFirstWalk_MatchesLayerOverload()
    {
        FakeRenderable a = new FakeRenderable("a");
        FakeRenderable a1 = AddChild(a, "a1");
        AddChild(a1, "a1a");
        FakeRenderable b = new FakeRenderable("b");
        b.ClipsChildren = true;
        AddChild(b, "b1");

        Layer layer = BuildLayer(a, b);
        List<DrawCommand> commandsFromLayer = new List<DrawCommand>();
        HierarchicalOrderer.Instance.BuildDrawList(layer, commandsFromLayer);

        List<DrawCommand> commandsFromRoots = new List<DrawCommand>();
        HierarchicalOrderer.Instance.BuildDrawList(new List<IRenderableIpso> { a, b }, commandsFromRoots);

        Describe(commandsFromRoots).ShouldBe(Describe(commandsFromLayer));
    }

    [Fact]
    public void BuildDrawList_Roots_WhenCullTestBoundsMappingOmitted_FallsBackToScissorRectangleMapping()
    {
        FakeRenderable clipContainer = new FakeRenderable("clipContainer");
        clipContainer.ClipsChildren = true;
        FakeRenderable keep = AddChild(clipContainer, "keep");
        FakeRenderable cull = AddChild(clipContainer, "cull");

        Rectangle clipRect = new Rectangle(0, 0, 100, 100);
        Rectangle insideClip = new Rectangle(10, 10, 20, 20);
        Rectangle farOutside = new Rectangle(1000, 1000, 10, 10);

        Rectangle GetScissorRectangle(IRenderableIpso r)
        {
            if (r == cull)
            {
                return farOutside;
            }
            return r == clipContainer ? clipRect : insideClip;
        }

        List<DrawCommand> commands = new List<DrawCommand>();
        HierarchicalOrderer.Instance.BuildDrawList(
            new List<IRenderableIpso> { clipContainer },
            commands,
            GetScissorRectangle);

        Describe(commands).ShouldBe(new[]
        {
            "BeginClip:clipContainer",
            "DrawRenderable:clipContainer",
            "DrawRenderable:keep",
            "EndClip:clipContainer",
        });
    }

    // Regression guard: an ordinary (non-text) child genuinely scrolled off the bottom of an
    // on-screen clip — the ListBox/ScrollViewer case the cull exists for — must still be culled.
    [Fact]
    public void BuildDrawList_ScrolledPlainRenderableOutsideOnScreenClip_IsStillCulled()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

        FakeRenderable clipParent = new FakeRenderable("clipParent");
        clipParent.ClipsChildren = true;
        clipParent.Y = 300;
        clipParent.Width = 200;
        clipParent.Height = 200;

        FakeRenderable scrolledOffItem = AddChild(clipParent, "scrolledOffItem");
        scrolledOffItem.Width = 190;
        scrolledOffItem.Height = 80;
        scrolledOffItem.Y = 250; // well past the 200px-tall clip band (absolute Y: 300 + 250 = 550)

        Layer layer = BuildLayer(clipParent);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands, camera);

        Describe(commands).ShouldNotContain("DrawRenderable:scrolledOffItem");
    }

    // #4144: a multi-line Forms TextBox scrolls by moving its Text's Y while Height stays fixed to
    // the visible box, so the Text's own declared bounds can drift entirely outside an on-screen
    // clip even though its actual wrapped content (WrappedTextHeight) still overlaps it. The
    // off-screen cull must not skip drawing it in that case.
    //
    // clipParent is deliberately NOT at Y=0: GetScissorRectangleFor clamps its result to the
    // camera's overall client bounds, and a clip sitting exactly at that boundary makes an
    // off-clip renderable's declared bounds collapse to a degenerate rect AT the same boundary —
    // which then spuriously reads as "not fully outside" regardless of any fix. Placing the clip
    // in the middle of the camera avoids that false negative.
    [Fact]
    public void BuildDrawList_ScrolledWrappedTextInsideOnScreenClip_IsNotCulled()
    {
        Camera camera = new Camera();
        camera.ClientWidth = 800;
        camera.ClientHeight = 600;
        camera.CameraCenterOnScreen = CameraCenterOnScreen.TopLeft;

        FakeRenderable clipParent = new FakeRenderable("clipParent");
        clipParent.ClipsChildren = true;
        clipParent.Y = 300;
        clipParent.Width = 200;
        clipParent.Height = 200;

        FakeRenderable scrolledText = AddChild(clipParent, "scrolledText");
        scrolledText.Width = 190;
        scrolledText.Height = 200; // fixed to the visible box, like TextInstance's RelativeToParent Height
        scrolledText.Y = -400;     // scrolled past many wrapped lines (absolute Y: 300 - 400 = -100)
        scrolledText.WrappedTextHeight = 700; // actual content is far taller than Height

        Layer layer = BuildLayer(clipParent);
        List<DrawCommand> commands = new List<DrawCommand>();

        HierarchicalOrderer.Instance.BuildDrawList(layer, commands, camera);

        Describe(commands).ShouldContain("DrawRenderable:scrolledText");
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
