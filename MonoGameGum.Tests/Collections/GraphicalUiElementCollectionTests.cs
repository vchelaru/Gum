using System.Collections.ObjectModel;
using Gum.Collections;
using Gum.GueDeriving;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Collections;

/// <summary>
/// Unit tests for <see cref="GraphicalUiElementCollection"/>, the wrapper that presents a raw
/// <c>ObservableCollection&lt;IRenderableIpso&gt;</c> as <c>ObservableCollection&lt;GraphicalUiElement&gt;</c>.
/// Found while investigating a Gum-batching draw-order discrepancy (a card's fill+stroke shape
/// pair should have been DFS-adjacent - and mergeable into one batch - but a child text ended up
/// wedged between them).
/// </summary>
public class GraphicalUiElementCollectionTests : BaseTestClass
{
    /// <summary>
    /// Minimal non-<see cref="Gum.Wireframe.GraphicalUiElement"/> <see cref="IRenderableIpso"/> -
    /// mirrors the shape of e.g. <c>RectangleRuntime</c>'s auto-wired stroke renderable, which is a
    /// raw renderable object, not a GraphicalUiElement.
    /// </summary>
    private sealed class RawRenderable : IRenderableIpso
    {
        public RawRenderable(string name) => Name = name;
        public string Name { get; set; }
        public bool Visible { get; set; } = true;
        public bool ClipsChildren { get; set; }
        public bool IsRenderTarget { get; set; }
        public ObservableCollection<IRenderableIpso> Children { get; } = new();
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
        public string BatchKey { get; set; } = "";
        public object? BatchSortKey { get; set; }
        public void Render(ISystemManagers managers) { }
        public void PreRender() { }
        public void StartBatch(ISystemManagers managers) { }
        public void EndBatch(ISystemManagers managers) { }
        public override string ToString() => Name;
    }

    [Fact]
    public void Add_AfterARawNonGraphicalUiElementItemWasAlreadyAttached_InsertsAfterIt()
    {
        // Reproduces the bug found investigating RectangleRuntime's fill+stroke composite:
        // SetContainedObject wraps the fill's raw Children in a GraphicalUiElementCollection
        // BEFORE the stroke (a raw, non-GraphicalUiElement IRenderableIpso) is attached via
        // RenderableBase.Parent's own Children.Add. The wrapper's CollectionChanged handler
        // silently drops that raw item from its own bookkeeping (it isn't a GraphicalUiElement),
        // so a subsequent logical Add computed its insertion index against an undercounted Count
        // and landed the new item BEFORE the raw one in the real draw list - reordering a user's
        // AddChild ahead of infrastructure that was attached first.
        ObservableCollection<IRenderableIpso> innerCollection = new();
        GraphicalUiElementCollection wrapper = new(innerCollection);

        RawRenderable stroke = new("stroke");
        innerCollection.Add(stroke); // mirrors RenderableBase.Parent's Children.Add(this)

        ContainerRuntime text = new() { Name = "text" };
        wrapper.Add(text); // mirrors GraphicalUiElement.AddChild

        innerCollection.ShouldBe(new IRenderableIpso[] { stroke, text });
    }

    [Fact]
    public void Clear_OnAShapeRuntime_RemovesTheChildrenButKeepsTheAutoWiredStroke()
    {
        // A RectangleRuntime's Children wrap its fill renderable's raw child list, which already
        // holds the auto-wired stroke - so a wholesale inner Clear() destroyed the stroke along
        // with the user's children and nothing re-created it, leaving the rectangle unstroked.
        RectangleRuntime rectangle = new();
        IRenderableIpso fill = (IRenderableIpso)rectangle.RenderableComponent;
        IRenderableIpso stroke = fill.Children.ShouldHaveSingleItem();

        ContainerRuntime child = new();
        rectangle.Children.Add(child);
        rectangle.Children.Clear();

        rectangle.Children.ShouldBeEmpty();
        fill.Children.ShouldBe(new[] { stroke });
    }

    [Fact]
    public void Clear_WithOnlyMirroredItems_EmptiesBothCollections()
    {
        ObservableCollection<IRenderableIpso> innerCollection = new();
        GraphicalUiElementCollection wrapper = new(innerCollection);

        wrapper.Add(new ContainerRuntime());
        wrapper.Add(new ContainerRuntime());

        wrapper.Clear();

        wrapper.ShouldBeEmpty();
        innerCollection.ShouldBeEmpty();
    }

    [Fact]
    public void Remove_WithARawNonGraphicalUiElementItemBeforeIt_RemovesTheCorrectItem()
    {
        // Same root cause as the Add test, for RemoveItem: the wrapper's logical index doesn't
        // match the raw list's index once a non-GraphicalUiElement item is interleaved, so a
        // naive index passthrough removes the wrong raw entry. Built via direct inner-collection
        // manipulation (not wrapper.Add) so this test's setup doesn't itself depend on the Add
        // bug being present or fixed - inner.Add(text) mirrors text into the wrapper through the
        // (already-correct) from-inner sync path, independent of InsertItem's outer-driven path.
        ObservableCollection<IRenderableIpso> innerCollection = new();
        GraphicalUiElementCollection wrapper = new(innerCollection);

        RawRenderable stroke = new("stroke");
        innerCollection.Add(stroke); // raw index 0, not mirrored into the wrapper

        ContainerRuntime text = new() { Name = "text" };
        innerCollection.Add(text); // raw index 1; mirrored into the wrapper at logical index 0

        wrapper.Remove(text);

        innerCollection.ShouldBe(new IRenderableIpso[] { stroke });
    }
}
