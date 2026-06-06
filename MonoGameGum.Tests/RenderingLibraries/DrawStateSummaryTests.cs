using System.Collections.Generic;
using System.Collections.ObjectModel;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.RenderingLibraries;

/// <summary>
/// Unit tests for <see cref="DrawStateSummary"/>, which buckets a frame's recorded
/// <see cref="BeginParameters"/> by the cause of each SpriteBatch begin so callers can tell
/// whether a high begin count is driven by clipping, non-clip state changes, or texture sets.
/// </summary>
public class DrawStateSummaryTests : BaseTestClass
{
    /// <summary>Minimal <see cref="IRenderableIpso"/> whose only meaningful property is <see cref="ClipsChildren"/>.</summary>
    private sealed class FakeRenderable : IRenderableIpso
    {
        public FakeRenderable(bool clipsChildren)
        {
            ClipsChildren = clipsChildren;
            Children = new ObservableCollection<IRenderableIpso>();
        }

        public string Name { get; set; } = "";
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
        public string BatchKey => "SpriteBatch";
        public void Render(ISystemManagers managers) { }
        public void PreRender() { }
        public void StartBatch(ISystemManagers managers) { }
        public void EndBatch(ISystemManagers managers) { }
    }

    private static BeginParameters MakeState(object? cause, int textureSets = 0)
    {
        StateChangeInfoList changeRecord = new StateChangeInfoList();
        for (int i = 0; i < textureSets; i++)
        {
            changeRecord.Add(new StateChangeInfo());
        }

        return new BeginParameters
        {
            ObjectChangingState = cause,
            ChangeRecord = changeRecord,
        };
    }

    [Fact]
    public void FromDrawStates_AttributesClipEnterAndExitToClipChanges()
    {
        List<BeginParameters> states = new List<BeginParameters>
        {
            MakeState(new FakeRenderable(clipsChildren: true)),  // clip enter
            MakeState("Un-set ContainerRuntime Clip"),           // clip exit
        };

        DrawStateSummary summary = DrawStateSummary.FromDrawStates(states);

        summary.ClipChangeBeginCount.ShouldBe(2);
        summary.StateChangeBeginCount.ShouldBe(0);
        summary.InitialBeginCount.ShouldBe(0);
    }

    [Fact]
    public void FromDrawStates_AttributesNullToInitial()
    {
        List<BeginParameters> states = new List<BeginParameters> { MakeState(null) };

        DrawStateSummary summary = DrawStateSummary.FromDrawStates(states);

        summary.InitialBeginCount.ShouldBe(1);
        summary.ClipChangeBeginCount.ShouldBe(0);
        summary.StateChangeBeginCount.ShouldBe(0);
    }

    [Fact]
    public void FromDrawStates_AttributesRenderableWithoutClipToStateChange()
    {
        List<BeginParameters> states = new List<BeginParameters>
        {
            MakeState(new FakeRenderable(clipsChildren: false)),
        };

        DrawStateSummary summary = DrawStateSummary.FromDrawStates(states);

        summary.StateChangeBeginCount.ShouldBe(1);
        summary.ClipChangeBeginCount.ShouldBe(0);
    }

    [Fact]
    public void FromDrawStates_BeginCountEqualsSumOfCategories()
    {
        List<BeginParameters> states = new List<BeginParameters>
        {
            MakeState(null),
            MakeState(new FakeRenderable(clipsChildren: true)),
            MakeState("Un-set Foo Clip"),
            MakeState(new FakeRenderable(clipsChildren: false)),
        };

        DrawStateSummary summary = DrawStateSummary.FromDrawStates(states);

        summary.BeginCount.ShouldBe(4);
        (summary.InitialBeginCount + summary.ClipChangeBeginCount + summary.StateChangeBeginCount)
            .ShouldBe(summary.BeginCount);
    }

    [Fact]
    public void FromDrawStates_CarriesShapeBatchBeginCount()
    {
        DrawStateSummary summary = DrawStateSummary.FromDrawStates(new List<BeginParameters>(), shapeBatchBeginCount: 7);

        summary.ShapeBatchBeginCount.ShouldBe(7);
    }

    [Fact]
    public void FromDrawStates_EmptyInput_ProducesZeroCounts()
    {
        DrawStateSummary summary = DrawStateSummary.FromDrawStates(new List<BeginParameters>());

        summary.BeginCount.ShouldBe(0);
        summary.InitialBeginCount.ShouldBe(0);
        summary.ClipChangeBeginCount.ShouldBe(0);
        summary.StateChangeBeginCount.ShouldBe(0);
        summary.TextureSetCount.ShouldBe(0);
        summary.ShapeBatchBeginCount.ShouldBe(0);
    }

    [Fact]
    public void FromDrawStates_SumsTextureSetsAcrossBatches()
    {
        List<BeginParameters> states = new List<BeginParameters>
        {
            MakeState(null, textureSets: 2),
            MakeState("Un-set Foo Clip", textureSets: 3),
        };

        DrawStateSummary summary = DrawStateSummary.FromDrawStates(states);

        summary.TextureSetCount.ShouldBe(5);
    }
}
