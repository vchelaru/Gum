using Gum.DataTypes;
using Gum.Graphics.Animation;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Graphics;
using Moq;
using RenderingLibrary.Content;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Section 4.6 of LAYOUT_TEST_PLAN.md: setters that change what a renderable reports as its size
/// (text measure, texture size, aspect ratio) must re-run layout, and must respect suspension.
/// </summary>
public class LayoutRenderableSetterTests : BaseTestClass
{
    class FakeText : InvisibleRenderable, IText
    {
        public float DescenderHeight { get; set; }
        public float FontScale { get; set; } = 1;
        public float WrappedTextWidth { get; set; }
        public float WrappedTextHeight { get; set; }
        public string? RawText { get; set; } = "text";
        public string? StoredMarkupText => RawText;
        float? IText.Width { get; set; }
        public TextOverflowVerticalMode TextOverflowVerticalMode { get; set; }

        public void SetNeedsRefreshToTrue()
        {
        }

        public void UpdatePreRenderDimensions()
        {
        }
    }

    readonly Action<IText, GraphicalUiElement>? _originalFontLoader;

    public LayoutRenderableSetterTests()
    {
        _originalFontLoader = GraphicalUiElement.UpdateFontFromProperties;
    }

    public override void Dispose()
    {
        GraphicalUiElement.UpdateFontFromProperties = _originalFontLoader;
        base.Dispose();
    }

    static GraphicalUiElement CreateRelativeToChildrenText(FakeText renderable)
    {
        GraphicalUiElement text = new(renderable);
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Height = 0;
        return text;
    }

    #region Font loading

    [Fact]
    public void FontLoad_ShouldResizeRelativeToChildrenElement_WhenLoaderChangesMeasuredSize()
    {
        FakeText renderable = new() { WrappedTextWidth = 20, WrappedTextHeight = 10 };
        GraphicalUiElement text = CreateRelativeToChildrenText(renderable);
        GraphicalUiElement.UpdateFontFromProperties = (_, _) =>
        {
            renderable.WrappedTextWidth = 70;
            renderable.WrappedTextHeight = 25;
        };

        text.UpdateToFontValues();

        text.AbsoluteWidth.ShouldBe(70);
        text.AbsoluteHeight.ShouldBe(25);
    }

    [Fact]
    public void FontLoad_ShouldResizeOnResume_WhenFontChangedWhileSuspended()
    {
        FakeText renderable = new() { WrappedTextWidth = 20, WrappedTextHeight = 10 };
        GraphicalUiElement text = CreateRelativeToChildrenText(renderable);
        GraphicalUiElement.UpdateFontFromProperties = (_, _) => renderable.WrappedTextWidth = 70;

        text.SuspendLayout();
        text.UpdateToFontValues();
        text.AbsoluteWidth.ShouldBe(20);
        text.ResumeLayout();

        text.AbsoluteWidth.ShouldBe(70);
    }

    [Fact]
    public void FontLoad_ShouldMoveTextBaselineElement_WhenDescenderChanges()
    {
        FakeText renderable = new() { WrappedTextHeight = 30, DescenderHeight = 4 };
        GraphicalUiElement text = new(renderable);
        text.Height = 30;
        text.Y = 100;
        text.YOrigin = VerticalAlignment.TextBaseline;
        text.AbsoluteTop.ShouldBe(100 - 30 + 4);
        GraphicalUiElement.UpdateFontFromProperties = (_, _) => renderable.DescenderHeight = 10;

        text.UpdateToFontValues();

        text.AbsoluteTop.ShouldBe(100 - 30 + 10);
    }

    [Fact]
    public void FontLoad_ShouldMovePixelsFromBaselineChild_WhenDescenderChanges()
    {
        FakeText renderable = new() { WrappedTextHeight = 30, DescenderHeight = 4 };
        GraphicalUiElement text = new(renderable);
        text.Height = 30;
        ContainerRuntime child = new();
        child.YUnits = Gum.Converters.GeneralUnitType.PixelsFromBaseline;
        text.AddChild(child);
        float topBefore = child.AbsoluteTop;
        GraphicalUiElement.UpdateFontFromProperties = (_, _) => renderable.DescenderHeight = 10;

        text.UpdateToFontValues();

        child.AbsoluteTop.ShouldBe(topBefore - 6);
    }

    [Fact]
    public void FontLoad_ShouldNotLayOut_WhenTextIsFixedSizeAndDescenderIsUnchanged()
    {
        FakeText renderable = new() { WrappedTextWidth = 20, WrappedTextHeight = 10 };
        GraphicalUiElement text = new(renderable);
        text.Width = 50;
        text.Height = 50;
        GraphicalUiElement.UpdateFontFromProperties = (_, _) => renderable.WrappedTextWidth = 70;
        int layoutsBefore = GraphicalUiElement.UpdateLayoutCallCount;

        text.UpdateToFontValues();

        GraphicalUiElement.UpdateLayoutCallCount.ShouldBe(layoutsBefore);
    }

    [Fact]
    public void MaxNumberOfLines_ShouldResizeRelativeToChildrenText()
    {
        TextRuntime text = new();
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.Text = "Line1\nLine2\nLine3";
        float threeLineHeight = text.AbsoluteHeight;

        text.MaxNumberOfLines = 1;

        text.AbsoluteHeight.ShouldBeLessThan(threeLineHeight);
    }

    #endregion

    #region Texture and animation

    [Fact]
    public void SpriteTexture_ShouldResizeOnResume_WhenAssignedWhileSuspended()
    {
        SpriteRuntime sprite = new();
        sprite.AbsoluteWidth.ShouldBe(64);

        sprite.SuspendLayout();
        sprite.Texture = MakeTexture(200, 80);
        sprite.AbsoluteWidth.ShouldBe(64);
        sprite.ResumeLayout();

        sprite.AbsoluteWidth.ShouldBe(200);
        sprite.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void SpriteTexture_ShouldMovePercentageOfFileChild_WhenTextureSizeChanges()
    {
        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.HeightUnits = DimensionUnitType.Absolute;
        sprite.XUnits = Gum.Converters.GeneralUnitType.PercentageOfFile;
        sprite.X = 50;
        sprite.Texture = MakeTexture(100, 100);
        sprite.AbsoluteLeft.ShouldBe(50);

        sprite.Texture = MakeTexture(300, 100);

        sprite.AbsoluteLeft.ShouldBe(150);
    }

    [Fact]
    public void NineSliceTextureByName_ShouldResizePercentageOfSourceFileNineSlice()
    {
        NineSliceRuntime nineSlice = new();
        nineSlice.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        nineSlice.HeightUnits = DimensionUnitType.PercentageOfSourceFile;

        nineSlice.SetProperty("Texture", MakeTexture(200, 80));

        nineSlice.AbsoluteWidth.ShouldBe(200);
        nineSlice.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void NineSliceSourceFile_ShouldResizePercentageOfSourceFileNineSlice()
    {
        IContentLoader? originalLoader = LoaderManager.Self.ContentLoader;
        Mock<IContentLoader> loader = new();
        loader.Setup(x => x.LoadContent<Texture2D>(It.IsAny<string>())).Returns(MakeTexture(200, 80));
        LoaderManager.Self.ContentLoader = loader.Object;
        try
        {
            NineSliceRuntime nineSlice = new();
            nineSlice.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
            nineSlice.HeightUnits = DimensionUnitType.PercentageOfSourceFile;

            nineSlice.SourceFileName = "frame.png";

            nineSlice.AbsoluteWidth.ShouldBe(200);
            nineSlice.AbsoluteHeight.ShouldBe(80);
        }
        finally
        {
            LoaderManager.Self.ContentLoader = originalLoader;
        }
    }

    [Fact]
    public void SpriteCurrentChainName_ShouldResizeToNewChainFrame()
    {
        SpriteRuntime sprite = new();
        sprite.AnimationChains = CreateChains(MakeTexture(100, 100));

        sprite.CurrentChainName = "Wide";

        sprite.AbsoluteWidth.ShouldBe(80);
        sprite.AbsoluteHeight.ShouldBe(20);
    }

    [Fact]
    public void NineSliceCurrentChainName_ShouldResizeToNewChainFrame()
    {
        NineSliceRuntime nineSlice = new();
        nineSlice.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        nineSlice.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        nineSlice.AnimationChains = CreateChains(MakeTexture(100, 100));

        nineSlice.CurrentChainName = "Wide";

        nineSlice.AbsoluteWidth.ShouldBe(80);
        nineSlice.AbsoluteHeight.ShouldBe(20);
    }

    [Fact]
    public void SpriteAnimationChainFrameIndex_ShouldResizeToThatFrame()
    {
        SpriteRuntime sprite = new();
        AnimationChainList chains = CreateChains(MakeTexture(100, 100));
        chains[0].Add(chains[1][0]);
        sprite.AnimationChains = chains;

        sprite.AnimationChainFrameIndex = 1;

        sprite.AbsoluteWidth.ShouldBe(80);
        sprite.AbsoluteHeight.ShouldBe(20);
    }

    [Fact]
    public void SpriteAnimationChainTime_ShouldResizeToFrameAtThatTime()
    {
        SpriteRuntime sprite = new();
        AnimationChainList chains = CreateChains(MakeTexture(100, 100));
        chains[0].Add(chains[1][0]);
        sprite.AnimationChains = chains;

        sprite.AnimationChainTime = 1.5;

        sprite.AnimationChainFrameIndex.ShouldBe(1);
        sprite.AbsoluteWidth.ShouldBe(80);
    }

    [Fact]
    public void SpriteAnimationChainFrameIndex_ShouldResizeOnResume_WhenSetWhileSuspended()
    {
        SpriteRuntime sprite = new();
        AnimationChainList chains = CreateChains(MakeTexture(100, 100));
        chains[0].Add(chains[1][0]);
        sprite.AnimationChains = chains;
        sprite.AbsoluteWidth.ShouldBe(40);

        sprite.SuspendLayout();
        sprite.AnimationChainFrameIndex = 1;
        sprite.AbsoluteWidth.ShouldBe(40);
        sprite.ResumeLayout();

        sprite.AbsoluteWidth.ShouldBe(80);
    }

    [Fact]
    public void SpriteAnimationChainFrameIndex_ShouldResizeRelativeToChildrenParent()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        SpriteRuntime sprite = new();
        AnimationChainList chains = CreateChains(MakeTexture(100, 100));
        chains[0].Add(chains[1][0]);
        sprite.AnimationChains = chains;
        parent.AddChild(sprite);
        parent.AbsoluteWidth.ShouldBe(40);

        sprite.AnimationChainFrameIndex = 1;

        parent.AbsoluteWidth.ShouldBe(80);
        parent.AbsoluteHeight.ShouldBe(20);
    }

    [Fact]
    public void SpriteAnimationChainFrameIndex_ShouldLeaveTextureAddress_WhenNoChainsAreSet()
    {
        SpriteRuntime sprite = new();
        sprite.TextureAddress = TextureAddress.EntireTexture;

        sprite.AnimationChainFrameIndex = 1;
        sprite.AnimationChainTime = 1.5;

        sprite.TextureAddress.ShouldBe(TextureAddress.EntireTexture);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void NineSliceAnimationChainFrameIndexOrTime_ShouldResizeToThatFrame(bool setTime)
    {
        NineSliceRuntime nineSlice = new();
        nineSlice.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        nineSlice.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        AnimationChainList chains = CreateChains(MakeTexture(100, 100));
        chains[0].Add(chains[1][0]);
        nineSlice.AnimationChains = chains;

        if (setTime)
        {
            nineSlice.AnimationChainTime = 1.5;
        }
        else
        {
            nineSlice.AnimationChainFrameIndex = 1;
        }

        nineSlice.AbsoluteWidth.ShouldBe(80);
        nineSlice.AbsoluteHeight.ShouldBe(20);
    }

    // Two chains on one texture: "Square" shows a 40x40 region, "Wide" an 80x20 region.
    static AnimationChainList CreateChains(Texture2D texture)
    {
        AnimationChain square = new() { Name = "Square" };
        square.Add(new AnimationFrame
        {
            Texture = texture, FrameLength = 1,
            LeftCoordinate = 0, RightCoordinate = .4f, TopCoordinate = 0, BottomCoordinate = .4f
        });
        AnimationChain wide = new() { Name = "Wide" };
        wide.Add(new AnimationFrame
        {
            Texture = texture, FrameLength = 1,
            LeftCoordinate = 0, RightCoordinate = .8f, TopCoordinate = 0, BottomCoordinate = .2f
        });
        AnimationChainList chains = new();
        chains.Add(square);
        chains.Add(wide);
        return chains;
    }

    // Fabricates a Texture2D (its constructor needs a GraphicsDevice we don't have headlessly) and
    // sets the size fields its Width/Height properties read.
    static Texture2D MakeTexture(int width, int height)
    {
        Type type = typeof(Texture2D);
        Texture2D texture = (Texture2D)System.Runtime.CompilerServices.RuntimeHelpers.GetUninitializedObject(type);
        System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        type.GetField("width", flags)!.SetValue(texture, width);
        type.GetField("height", flags)!.SetValue(texture, height);
        return texture;
    }

    #endregion
}
