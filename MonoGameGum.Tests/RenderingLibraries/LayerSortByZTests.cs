using System.Collections.Generic;
using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Pins <see cref="Layer.SortByZ"/>, the stable-Z-sort helper shared by
/// <see cref="Layer.SortRenderables"/> and <see cref="Renderer"/>'s deferred immediate-mode flush
/// (issue #4573) so both sort the same way without duplicating the algorithm.
/// </summary>
public class LayerSortByZTests : BaseTestClass
{
    private sealed class FakeRenderable : IRenderableIpso, IWrappedText
    {
        public FakeRenderable(string name, float z)
        {
            Name = name;
            Z = z;
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

    [Fact]
    public void SortByZ_OrdersAscendingByZ()
    {
        FakeRenderable low = new("low", 1);
        FakeRenderable mid = new("mid", 2);
        FakeRenderable high = new("high", 3);
        List<IRenderableIpso> renderables = new() { high, low, mid };

        Layer.SortByZ(renderables);

        renderables.ShouldBe(new IRenderableIpso[] { low, mid, high });
    }

    [Fact]
    public void SortByZ_IsStableForEqualZ()
    {
        // All three share Z=0. A stable sort preserves original relative (insertion/call) order
        // as the tie-break, matching what Layer.SortRenderables already documents.
        FakeRenderable first = new("first", 0);
        FakeRenderable second = new("second", 0);
        FakeRenderable third = new("third", 0);
        List<IRenderableIpso> renderables = new() { first, second, third };

        Layer.SortByZ(renderables);

        renderables.ShouldBe(new IRenderableIpso[] { first, second, third });
    }
}
