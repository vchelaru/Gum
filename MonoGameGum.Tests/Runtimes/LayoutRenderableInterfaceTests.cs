using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Section 4 of LAYOUT_TEST_PLAN.md: layout inputs that come from the interfaces a renderable
/// implements. Each fake renderable implements only the interface under test, so the test pins the
/// layout engine rather than one backend's Sprite or Text.
/// </summary>
public class LayoutRenderableInterfaceTests : BaseTestClass
{
    #region Fakes

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

    class FakeWrappedText : FakeText, IWrappedText
    {
        public int? MaxNumberOfLines => null;
        public int LineHeightInPixels => 10;
        public bool IsTruncatingWithEllipsisOnLastLine => false;
        public bool IsHeightDependentOnLines { get; set; }
        public bool IsMidWordLineBreakEnabled => false;

        public float MeasureString(string text) => text.Length;
    }

    class TexturedRenderable : InvisibleRenderable, ITextureCoordinate
    {
        public System.Drawing.Rectangle? SourceRectangle { get; set; }
        bool ITextureCoordinate.Wrap { get; set; }
        public float? TextureWidth { get; set; }
        public float? TextureHeight { get; set; }
    }

    class AspectRatioRenderable : InvisibleRenderable, IAspectRatio
    {
        public float AspectRatio { get; set; } = 1;
    }

    #endregion

    static ContainerRuntime CreateContainer(float width, float height)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = width;
        container.Height = height;
        return container;
    }

    static ContainerRuntime CreateRelativeToChildrenContainer()
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.RelativeToChildren;
        container.HeightUnits = DimensionUnitType.RelativeToChildren;
        container.Width = 0;
        container.Height = 0;
        return container;
    }

    static GraphicalUiElement CreateParentlessChild(GraphicalUiElement containingElement, float width, float height)
    {
        GraphicalUiElement child = new(new InvisibleRenderable(), containingElement);
        child.Parent = null;
        child.WidthUnits = DimensionUnitType.Absolute;
        child.HeightUnits = DimensionUnitType.Absolute;
        child.Width = width;
        child.Height = height;
        return child;
    }

    #region 4.1 No renderable

    [Fact]
    public void ParentlessChildren_ShouldStack_WhenContainingElementStacksTopToBottom()
    {
        ContainerRuntime component = CreateContainer(100, 400);
        component.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        component.StackSpacing = 5;
        GraphicalUiElement first = CreateParentlessChild(component, 100, 50);
        GraphicalUiElement second = CreateParentlessChild(component, 100, 30);

        component.UpdateLayout();

        first.AbsoluteTop.ShouldBe(0);
        second.AbsoluteTop.ShouldBe(55);
    }

    [Fact]
    public void ParentlessChildren_ShouldFillGridCells_WhenContainingElementIsAutoGrid()
    {
        ContainerRuntime component = CreateContainer(200, 100);
        component.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        component.AutoGridHorizontalCells = 2;
        component.AutoGridVerticalCells = 2;
        GraphicalUiElement first = CreateParentlessChild(component, 10, 10);
        GraphicalUiElement second = CreateParentlessChild(component, 10, 10);
        GraphicalUiElement third = CreateParentlessChild(component, 10, 10);

        component.UpdateLayout();

        first.AbsoluteLeft.ShouldBe(0);
        first.AbsoluteTop.ShouldBe(0);
        second.AbsoluteLeft.ShouldBe(100);
        second.AbsoluteTop.ShouldBe(0);
        third.AbsoluteLeft.ShouldBe(0);
        third.AbsoluteTop.ShouldBe(50);
    }

    [Fact]
    public void ParentlessChildren_ShouldStack_WhenRenderablelessScreenStacksLeftToRight()
    {
        GraphicalUiElement screen = new(null);
        screen.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        GraphicalUiElement first = CreateParentlessChild(screen, 40, 10);
        GraphicalUiElement second = CreateParentlessChild(screen, 60, 10);

        screen.UpdateLayout();

        first.AbsoluteLeft.ShouldBe(0);
        second.AbsoluteLeft.ShouldBe(40);
    }

    [Fact]
    public void SettingX_ShouldKeepStackPosition_ForParentlessChildOfStackingContainer()
    {
        ContainerRuntime component = CreateContainer(400, 100);
        component.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        CreateParentlessChild(component, 50, 10);
        GraphicalUiElement second = CreateParentlessChild(component, 50, 10);
        component.UpdateLayout();
        float stackedLeft = second.AbsoluteLeft;

        second.X = 5;

        second.AbsoluteLeft.ShouldBe(stackedLeft + 5);
    }

    // An element without a renderable (an old-style screen) holds its instances through
    // ElementGueContainingThis, not as Children. Assigning a renderable later does not reparent
    // them: they stay contained, keep a null Parent, and are not positioned inside the element.
    // Intended (#5772).
    [Fact]
    public void SetContainedObject_ShouldNotReparentContainedInstances_WhenRenderableIsAssignedLater()
    {
        GraphicalUiElement element = new(null);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 200;
        element.Height = 100;
        element.X = 30;
        GraphicalUiElement child = new(new InvisibleRenderable(), element);
        child.WidthUnits = DimensionUnitType.PercentageOfParent;
        child.Width = 50;
        element.UpdateLayout();

        element.SetContainedObject(new InvisibleRenderable());
        element.UpdateLayout();

        child.Parent.ShouldBeNull();
        child.ElementGueContainingThis.ShouldBe(element);
        element.Children.ShouldNotContain(child);
        child.AbsoluteLeft.ShouldBe(0);
        child.AbsoluteWidth.ShouldBe(GraphicalUiElement.CanvasWidth * 50 / 100);
    }

    #endregion

    #region 4.2 IText

    [Fact]
    public void TextChange_ShouldResizeRelativeToChildrenParent()
    {
        ContainerRuntime parent = CreateRelativeToChildrenContainer();
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Text = "a";
        parent.AddChild(text);
        float shortWidth = parent.AbsoluteWidth;

        text.Text = "a much longer string of text";

        text.AbsoluteWidth.ShouldBeGreaterThan(shortWidth);
        parent.AbsoluteWidth.ShouldBe(text.AbsoluteWidth);
    }

    [Fact]
    public void TextChange_ShouldMoveLaterStackSiblings()
    {
        ContainerRuntime stack = CreateContainer(800, 100);
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Text = "a";
        stack.AddChild(text);
        ContainerRuntime sibling = CreateContainer(10, 10);
        stack.AddChild(sibling);

        text.Text = "a much longer string of text";

        sibling.AbsoluteLeft.ShouldBe(text.AbsoluteRight);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void EmptyOrNullText_ShouldHaveZeroWidth_WhenRelativeToChildren(string? value)
    {
        TextRuntime text = new();
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Text = "abc";

        text.Text = value;

        text.AbsoluteWidth.ShouldBe(0);
        float.IsFinite(text.AbsoluteHeight).ShouldBeTrue();
    }

    [Fact]
    public void NullText_ShouldMatchEmptyText()
    {
        TextRuntime empty = new();
        empty.WidthUnits = DimensionUnitType.RelativeToChildren;
        empty.HeightUnits = DimensionUnitType.RelativeToChildren;
        empty.Text = "";
        TextRuntime nullText = new();
        nullText.WidthUnits = DimensionUnitType.RelativeToChildren;
        nullText.HeightUnits = DimensionUnitType.RelativeToChildren;
        nullText.Text = null;

        nullText.AbsoluteWidth.ShouldBe(empty.AbsoluteWidth);
        nullText.AbsoluteHeight.ShouldBe(empty.AbsoluteHeight);
    }

    [Fact]
    public void Text_ShouldTakeWidthFromTextAndHeightFromParent_WhenAxesUseDifferentUnits()
    {
        ContainerRuntime parent = CreateContainer(400, 200);
        FakeText renderable = new() { WrappedTextWidth = 120, WrappedTextHeight = 30 };
        GraphicalUiElement text = new(renderable);
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.HeightUnits = DimensionUnitType.PercentageOfParent;
        text.Height = 50;
        parent.AddChild(text);

        text.AbsoluteWidth.ShouldBe(120);
        text.AbsoluteHeight.ShouldBe(100);
    }

    [Fact]
    public void Text_ShouldTakeHeightFromTextAndWidthFromParent_WhenAxesUseDifferentUnits()
    {
        ContainerRuntime parent = CreateContainer(400, 200);
        FakeText renderable = new() { WrappedTextWidth = 120, WrappedTextHeight = 30 };
        GraphicalUiElement text = new(renderable);
        text.WidthUnits = DimensionUnitType.PercentageOfParent;
        text.Width = 50;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        parent.AddChild(text);

        text.AbsoluteWidth.ShouldBe(200);
        text.AbsoluteHeight.ShouldBe(30);
    }

    [Fact]
    public void TextBaselineOrigin_ShouldPlaceBaselineAtY_WithScaledDescender()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        FakeText renderable = new() { WrappedTextWidth = 100, WrappedTextHeight = 40, DescenderHeight = 5, FontScale = 2 };
        GraphicalUiElement text = new(renderable);
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.YOrigin = VerticalAlignment.TextBaseline;
        text.Y = 100;
        parent.AddChild(text);

        // Baseline sits DescenderHeight * FontScale above the bottom: 100 - 40 + 5 * 2.
        text.AbsoluteTop.ShouldBe(70);
    }

    [Fact(Skip = "Behavior change pending decision: #5771")]
    public void PixelsFromBaselineChild_ShouldSitOnTextBaseline_WhenFontScaleIsNotOne()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        FakeText renderable = new() { WrappedTextWidth = 100, WrappedTextHeight = 40, DescenderHeight = 5, FontScale = 2 };
        GraphicalUiElement text = new(renderable);
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.YOrigin = VerticalAlignment.TextBaseline;
        text.Y = 100;
        parent.AddChild(text);
        ContainerRuntime child = CreateContainer(10, 10);
        child.YUnits = GeneralUnitType.PixelsFromBaseline;
        child.YOrigin = VerticalAlignment.Bottom;
        child.Y = 0;
        text.AddChild(child);

        child.AbsoluteBottom.ShouldBe(100);
    }

    [Fact]
    public void PixelsFromBaselineChild_ShouldSitOnTextBaseline_WhenFontScaleIsOne()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        FakeText renderable = new() { WrappedTextWidth = 100, WrappedTextHeight = 40, DescenderHeight = 5, FontScale = 1 };
        GraphicalUiElement text = new(renderable);
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Height = 0;
        text.YOrigin = VerticalAlignment.TextBaseline;
        text.Y = 100;
        parent.AddChild(text);
        ContainerRuntime child = CreateContainer(10, 10);
        child.YUnits = GeneralUnitType.PixelsFromBaseline;
        child.YOrigin = VerticalAlignment.Bottom;
        child.Y = 0;
        text.AddChild(child);

        child.AbsoluteBottom.ShouldBe(100);
    }

    #endregion

    #region 4.3 IWrappedText

    [Theory]
    [InlineData(DimensionUnitType.RelativeToChildren, true)]
    [InlineData(DimensionUnitType.Absolute, false)]
    [InlineData(DimensionUnitType.PercentageOfParent, false)]
    public void IsHeightDependentOnLines_ShouldFollowHeightUnits_OnFirstLayout(DimensionUnitType heightUnits, bool expected)
    {
        FakeWrappedText renderable = new() { IsHeightDependentOnLines = !expected };
        GraphicalUiElement text = new(renderable);

        text.HeightUnits = heightUnits;
        text.UpdateLayout();

        renderable.IsHeightDependentOnLines.ShouldBe(expected);
    }

    [Fact]
    public void IsHeightDependentOnLines_ShouldUpdate_WhenHeightUnitsChangeAfterLayout()
    {
        FakeWrappedText renderable = new();
        GraphicalUiElement text = new(renderable);
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        renderable.IsHeightDependentOnLines.ShouldBeTrue();

        text.HeightUnits = DimensionUnitType.Absolute;
        renderable.IsHeightDependentOnLines.ShouldBeFalse();

        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        renderable.IsHeightDependentOnLines.ShouldBeTrue();
    }

    [Fact]
    public void IsHeightDependentOnLines_ShouldBeReapplied_OnEveryHeightUpdate()
    {
        FakeWrappedText renderable = new();
        GraphicalUiElement text = new(renderable);
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        renderable.IsHeightDependentOnLines = false;

        text.UpdateLayout();

        renderable.IsHeightDependentOnLines.ShouldBeTrue();
    }

    #endregion

    #region 4.4 ITextureCoordinate

    [Fact]
    public void DimensionsBased_ShouldDeriveSourceRectangleFromSizeAndTextureScale()
    {
        TexturedRenderable renderable = new();
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.Height = 50;
        element.TextureLeft = 3;
        element.TextureTop = 4;
        element.TextureWidthScale = 2;
        element.TextureHeightScale = 0.5f;
        element.TextureAddress = TextureAddress.DimensionsBased;

        renderable.SourceRectangle.ShouldBe(new System.Drawing.Rectangle(3, 4, 50, 100));

        element.Width = 200;

        renderable.SourceRectangle.ShouldBe(new System.Drawing.Rectangle(3, 4, 100, 100));
    }

    [Fact]
    public void DimensionsBased_ShouldGiveZeroSourceSize_WhenTextureScaleIsZero()
    {
        TexturedRenderable renderable = new();
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.Height = 50;
        element.TextureWidthScale = 0;
        element.TextureHeightScale = 0;
        element.TextureAddress = TextureAddress.DimensionsBased;

        // A zero scale is guarded against dividing by zero and yields an empty source rectangle.
        renderable.SourceRectangle.ShouldBe(new System.Drawing.Rectangle(0, 0, 0, 0));
        element.AbsoluteWidth.ShouldBe(100);
        element.AbsoluteHeight.ShouldBe(50);
    }

    [Fact]
    public void Custom_ShouldReapplySourceRectangle_AfterSizeChange()
    {
        TexturedRenderable renderable = new();
        GraphicalUiElement element = new(renderable);
        element.TextureAddress = TextureAddress.Custom;
        element.TextureLeft = 1;
        element.TextureTop = 2;
        element.TextureWidth = 30;
        element.TextureHeight = 40;
        renderable.SourceRectangle = new System.Drawing.Rectangle(9, 9, 9, 9);

        element.Width = 77;

        renderable.SourceRectangle.ShouldBe(new System.Drawing.Rectangle(1, 2, 30, 40));
    }

    [Fact]
    public void EntireTexture_ShouldClearSourceRectangle_AfterSizeChange()
    {
        TexturedRenderable renderable = new();
        GraphicalUiElement element = new(renderable);
        element.TextureAddress = TextureAddress.EntireTexture;
        renderable.SourceRectangle = new System.Drawing.Rectangle(9, 9, 9, 9);

        element.Width = 77;

        renderable.SourceRectangle.ShouldBeNull();
    }

    [Fact]
    public void PercentageOfSourceFile_ShouldUseFallbackSize_WhenTextureSizeIsNull()
    {
        TexturedRenderable renderable = new();
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Width = 50;
        element.Height = 50;

        element.AbsoluteWidth.ShouldBe(32);
        element.AbsoluteHeight.ShouldBe(32);
    }

    [Fact]
    public void PercentageOfSourceFile_ShouldUseTextureSize_WhenTextureSizeIsSet()
    {
        TexturedRenderable renderable = new() { TextureWidth = 200, TextureHeight = 80 };
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Width = 50;
        element.Height = 50;

        element.AbsoluteWidth.ShouldBe(100);
        element.AbsoluteHeight.ShouldBe(40);
    }

    [Fact]
    public void PercentageOfSourceFile_ShouldPreferCustomSourceRectangle_OverTextureSize()
    {
        TexturedRenderable renderable = new() { TextureWidth = 200, TextureHeight = 80 };
        GraphicalUiElement element = new(renderable);
        element.TextureAddress = TextureAddress.Custom;
        element.TextureWidth = 30;
        element.TextureHeight = 40;
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Width = 50;
        element.Height = 50;

        element.AbsoluteWidth.ShouldBe(15);
        element.AbsoluteHeight.ShouldBe(20);
    }

    [Fact]
    public void UpdateTextureValuesFrom_ShouldResizePercentageOfSourceFileElementAndParent()
    {
        ContainerRuntime parent = CreateRelativeToChildrenContainer();
        TexturedRenderable renderable = new() { TextureWidth = 200, TextureHeight = 80 };
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Width = 100;
        element.Height = 100;
        parent.AddChild(element);
        TexturedRenderable frame = new() { SourceRectangle = new System.Drawing.Rectangle(5, 6, 20, 30) };

        element.UpdateTextureValuesFrom(frame);

        element.AbsoluteWidth.ShouldBe(20);
        element.AbsoluteHeight.ShouldBe(30);
        parent.AbsoluteWidth.ShouldBe(20);
        parent.AbsoluteHeight.ShouldBe(30);
    }

    #endregion

    #region 4.5 IAspectRatio

    [Fact]
    public void AspectRatioChange_ShouldResizeElementAndParent_OnNextLayout()
    {
        ContainerRuntime parent = CreateRelativeToChildrenContainer();
        AspectRatioRenderable renderable = new() { AspectRatio = 2 };
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Height = 100;
        parent.AddChild(element);
        parent.AbsoluteHeight.ShouldBe(50);

        renderable.AspectRatio = 4;
        element.UpdateLayout();

        element.AbsoluteHeight.ShouldBe(25);
        parent.AbsoluteHeight.ShouldBe(25);
    }

    [Theory]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void MaintainFileAspectRatioHeight_ShouldUseFallbackSize_WhenAspectRatioIsUnusable(float aspectRatio)
    {
        AspectRatioRenderable renderable = new() { AspectRatio = aspectRatio };
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.Absolute;
        element.Width = 100;
        element.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Height = 100;

        // Same 64-pixel fallback as an element with no texture.
        element.AbsoluteHeight.ShouldBe(64);
    }

    [Theory]
    [InlineData(-2f)]
    [InlineData(float.NaN)]
    [InlineData(float.PositiveInfinity)]
    public void MaintainFileAspectRatioWidth_ShouldUseFallbackSize_WhenAspectRatioIsUnusable(float aspectRatio)
    {
        AspectRatioRenderable renderable = new() { AspectRatio = aspectRatio };
        GraphicalUiElement element = new(renderable);
        element.HeightUnits = DimensionUnitType.Absolute;
        element.Height = 100;
        element.WidthUnits = DimensionUnitType.MaintainFileAspectRatio;
        element.Width = 100;

        element.AbsoluteWidth.ShouldBe(64);
    }

    #endregion

    #region 4.6 Size reported by the renderable changing outside Gum

    [Fact]
    public void TextureAssignedAfterLayout_ShouldResizePercentageOfSourceFileElement_OnNextLayout()
    {
        TexturedRenderable renderable = new();
        GraphicalUiElement element = new(renderable);
        element.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        element.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        element.Width = 100;
        element.Height = 100;
        element.AbsoluteWidth.ShouldBe(64);

        renderable.TextureWidth = 200;
        renderable.TextureHeight = 80;
        element.UpdateLayout();

        element.AbsoluteWidth.ShouldBe(200);
        element.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void SpriteTextureAssignedAfterLayout_ShouldResizePercentageOfSourceFileSprite()
    {
        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        sprite.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        sprite.Width = 100;
        sprite.Height = 100;

        sprite.Texture = MakeTexture(200, 80);

        sprite.AbsoluteWidth.ShouldBe(200);
        sprite.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void SpriteTextureAssignedAfterLayout_ShouldResizeMaintainFileAspectRatioSprite()
    {
        SpriteRuntime sprite = new();
        sprite.WidthUnits = DimensionUnitType.Absolute;
        sprite.Width = 100;
        sprite.HeightUnits = DimensionUnitType.MaintainFileAspectRatio;
        sprite.Height = 100;

        sprite.Texture = MakeTexture(200, 50);

        sprite.AbsoluteHeight.ShouldBe(25);
    }

    [Fact]
    public void NineSliceTextureAssignedAfterLayout_ShouldResizePercentageOfSourceFileNineSlice()
    {
        NineSliceRuntime nineSlice = new();
        nineSlice.WidthUnits = DimensionUnitType.PercentageOfSourceFile;
        nineSlice.HeightUnits = DimensionUnitType.PercentageOfSourceFile;
        nineSlice.Width = 100;
        nineSlice.Height = 100;

        nineSlice.Texture = MakeTexture(200, 80);

        nineSlice.AbsoluteWidth.ShouldBe(200);
        nineSlice.AbsoluteHeight.ShouldBe(80);
    }

    [Fact]
    public void RenderableSizeChange_ShouldReachRelativeToChildrenParentAndLaterStackSiblings()
    {
        ContainerRuntime stack = CreateRelativeToChildrenContainer();
        stack.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        FakeText renderable = new() { WrappedTextWidth = 20, WrappedTextHeight = 10 };
        GraphicalUiElement text = new(renderable);
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Height = 0;
        stack.AddChild(text);
        ContainerRuntime sibling = CreateContainer(10, 10);
        stack.AddChild(sibling);

        renderable.WrappedTextWidth = 70;
        renderable.WrappedTextHeight = 25;
        text.UpdateLayout();

        text.AbsoluteWidth.ShouldBe(70);
        sibling.AbsoluteLeft.ShouldBe(70);
        stack.AbsoluteWidth.ShouldBe(80);
        stack.AbsoluteHeight.ShouldBe(25);
    }

    // Fabricates a Texture2D (its constructor needs a GraphicsDevice we don't have headlessly) and
    // sets the size fields its Width/Height properties read.
    static Microsoft.Xna.Framework.Graphics.Texture2D MakeTexture(int width, int height)
    {
        Type type = typeof(Microsoft.Xna.Framework.Graphics.Texture2D);
        var texture = (Microsoft.Xna.Framework.Graphics.Texture2D)System.Runtime.CompilerServices
            .RuntimeHelpers.GetUninitializedObject(type);
        var flags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        type.GetField("width", flags)!.SetValue(texture, width);
        type.GetField("height", flags)!.SetValue(texture, height);
        return texture;
    }

    #endregion
}
