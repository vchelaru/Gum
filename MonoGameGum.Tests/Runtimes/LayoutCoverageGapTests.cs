using Gum.Converters;
using Gum.DataTypes;
using Gum.GueDeriving;
using Gum.Managers;
using Gum.Wireframe;
using Moq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using Shouldly;
using System;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

/// <summary>
/// Layout code that a coverage run of the layout tests never executed (#5867). Tests drive layout
/// through public properties and assert on absolute positions and sizes.
/// </summary>
public class LayoutCoverageGapTests : BaseTestClass
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

    // Records the renderable width at the moment the height is measured: a text sized to its
    // children measures its height at its max width, or unwrapped when there is no max.
    class WidthRecordingText : FakeText, IText
    {
        public float WidthWhenHeightWasRead { get; private set; }
        float IText.WrappedTextHeight
        {
            get
            {
                WidthWhenHeightWasRead = Width;
                return 10;
            }
        }
    }

    class ThrowingText : FakeText, IText
    {
        float IText.WrappedTextWidth => throw new BadImageFormatException();
    }

    static ContainerRuntime CreateContainer(float width, float height)
    {
        ContainerRuntime container = new();
        container.WidthUnits = DimensionUnitType.Absolute;
        container.HeightUnits = DimensionUnitType.Absolute;
        container.Width = width;
        container.Height = height;
        return container;
    }

    #region Invalid values (diagnostics build)

    // GumCommon is built with FULL_DIAGNOSTICS in every configuration except Release_No_Diagnostics,
    // where these setters accept the values instead of throwing.
    [Theory]
    [InlineData(nameof(GraphicalUiElement.X), float.NaN)]
    [InlineData(nameof(GraphicalUiElement.X), float.PositiveInfinity)]
    [InlineData(nameof(GraphicalUiElement.X), float.NegativeInfinity)]
    [InlineData(nameof(GraphicalUiElement.Y), float.NaN)]
    [InlineData(nameof(GraphicalUiElement.Y), float.PositiveInfinity)]
    [InlineData(nameof(GraphicalUiElement.Y), float.NegativeInfinity)]
    [InlineData(nameof(GraphicalUiElement.Width), float.NaN)]
    [InlineData(nameof(GraphicalUiElement.Width), float.PositiveInfinity)]
    [InlineData(nameof(GraphicalUiElement.Width), float.NegativeInfinity)]
    [InlineData(nameof(GraphicalUiElement.Height), float.NaN)]
    [InlineData(nameof(GraphicalUiElement.Height), float.PositiveInfinity)]
    [InlineData(nameof(GraphicalUiElement.Height), float.NegativeInfinity)]
    public void PositionOrSizeSetter_ShouldThrow_WhenValueIsNotFinite(string propertyName, float value)
    {
        ContainerRuntime element = CreateContainer(100, 100);

        Should.Throw<ArgumentException>(() => element.SetProperty(propertyName, value));
    }

    #endregion

    #region Rotated element setters

    // A rotated element lays out both axes when its size changes, so the stack after it still follows.
    [Theory]
    [InlineData(ChildrenLayout.LeftToRightStack, true)]
    [InlineData(ChildrenLayout.TopToBottomStack, false)]
    public void RotatedStackChild_ShouldMoveLaterSiblings_WhenMainAxisSizeChanges(ChildrenLayout stack, bool changeWidth)
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        parent.ChildrenLayout = stack;
        ContainerRuntime rotated = CreateContainer(100, 100);
        rotated.Rotation = 90;
        parent.AddChild(rotated);
        ContainerRuntime next = CreateContainer(10, 10);
        parent.AddChild(next);

        if (changeWidth)
        {
            rotated.Width = 60;
            rotated.AbsoluteWidth.ShouldBe(60);
            next.AbsoluteLeft.ShouldBe(60);
        }
        else
        {
            rotated.Height = 60;
            rotated.AbsoluteHeight.ShouldBe(60);
            next.AbsoluteTop.ShouldBe(60);
        }
    }

    #endregion

    #region AbsoluteX and AbsoluteY

    // AbsoluteX and AbsoluteY are the point the X and Y values place, so they do not depend on the
    // origin or on rotation about that point.
    [Theory]
    [InlineData(HorizontalAlignment.Left, VerticalAlignment.Top, 0f)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Center, 0f)]
    [InlineData(HorizontalAlignment.Right, VerticalAlignment.Bottom, 0f)]
    [InlineData(HorizontalAlignment.Left, VerticalAlignment.Top, 90f)]
    [InlineData(HorizontalAlignment.Center, VerticalAlignment.Center, 90f)]
    [InlineData(HorizontalAlignment.Right, VerticalAlignment.Bottom, 90f)]
    public void AbsoluteXAndY_ShouldBeThePlacedPoint_ForAnyOriginAndRotation(HorizontalAlignment xOrigin,
        VerticalAlignment yOrigin, float rotation)
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        ContainerRuntime child = CreateContainer(20, 10);
        child.X = 100;
        child.Y = 50;
        child.XOrigin = xOrigin;
        child.YOrigin = yOrigin;
        child.Rotation = rotation;
        parent.AddChild(child);

        child.AbsoluteX.ShouldBe(100, tolerance: 0.001f);
        child.AbsoluteY.ShouldBe(50, tolerance: 0.001f);
    }

    [Fact]
    public void AbsoluteX_ShouldBeThePlacedPoint_ForTextBaselineOriginWhenRotated()
    {
        FakeText renderable = new() { WrappedTextHeight = 30, DescenderHeight = 4 };
        GraphicalUiElement text = new(renderable);
        text.Height = 30;
        text.X = 100;
        text.Y = 50;
        text.YOrigin = VerticalAlignment.TextBaseline;
        text.Rotation = 90;

        text.AbsoluteX.ShouldBe(100, tolerance: 0.001f);
        text.AbsoluteY.ShouldBe(50, tolerance: 0.001f);
    }

    [Fact]
    public void AbsoluteY_ShouldBeThePlacedPoint_ForTextBaselineOrigin()
    {
        FakeText renderable = new() { WrappedTextHeight = 30, DescenderHeight = 4 };
        GraphicalUiElement text = new(renderable);
        text.Height = 30;
        text.Y = 50;
        text.YOrigin = VerticalAlignment.TextBaseline;

        text.AbsoluteTop.ShouldBe(50 - 30 + 4);
        text.AbsoluteY.ShouldBe(50, tolerance: 0.001f);
    }

    #endregion

    #region Fixed-size stack measuring

    static ContainerRuntime CreateFixedSizeStack()
    {
        ContainerRuntime stack = new();
        stack.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        stack.UseFixedStackChildrenSize = true;
        stack.WidthUnits = DimensionUnitType.Absolute;
        stack.Width = 100;
        stack.HeightUnits = DimensionUnitType.RelativeToChildren;
        stack.Height = 0;
        return stack;
    }

    [Fact]
    public void FixedSizeStack_ShouldHaveNoHeight_WhenEveryChildIsHidden()
    {
        ContainerRuntime stack = CreateFixedSizeStack();
        for (int i = 0; i < 3; i++)
        {
            ContainerRuntime child = CreateContainer(50, 40);
            child.Visible = false;
            stack.AddChild(child);
        }

        stack.AbsoluteHeight.ShouldBe(0);

        stack.Children[0].Visible = true;

        stack.AbsoluteHeight.ShouldBe(40);
    }

    // The first child sets the fixed size. When it also depends on the parent (here its width), it is
    // measured for height first and gets its width afterwards.
    [Fact]
    public void FixedSizeStack_ShouldMeasureFromFirstChild_WhenFirstChildWidthDependsOnParent()
    {
        ContainerRuntime stack = CreateFixedSizeStack();
        ContainerRuntime first = CreateContainer(0, 40);
        first.WidthUnits = DimensionUnitType.PercentageOfParent;
        first.Width = 50;
        stack.AddChild(first);
        stack.AddChild(CreateContainer(50, 40));

        stack.AbsoluteHeight.ShouldBe(80);
        first.AbsoluteWidth.ShouldBe(50);
    }

    // A first child that is a percentage of a stack sized to its children is circular, so both read 0.
    [Fact]
    public void FixedSizeStack_ShouldHaveNoHeight_WhenFirstChildHeightIsPercentageOfTheStack()
    {
        ContainerRuntime stack = CreateFixedSizeStack();
        ContainerRuntime first = CreateContainer(50, 0);
        first.HeightUnits = DimensionUnitType.PercentageOfParent;
        first.Height = 50;
        stack.AddChild(first);
        stack.AddChild(CreateContainer(50, 40));

        (stack.AbsoluteHeight, first.AbsoluteHeight).ShouldBe((0f, 0f));
    }

    #endregion

    #region Height and width units

    [Fact]
    public void ScreenPixelHeight_ShouldDivideByCameraZoom_WhenManagersAreAttached()
    {
        Camera camera = new();
        camera.Zoom = 2;
        Mock<IRenderer> renderer = new();
        renderer.Setup(item => item.Camera).Returns(camera);
        Mock<ISystemManagers> managers = new();
        managers.Setup(item => item.Renderer).Returns(renderer.Object);
        ContainerRuntime element = new();
        element.AttachManagersOnly(managers.Object);

        element.HeightUnits = DimensionUnitType.ScreenPixel;
        element.Height = 100;

        element.AbsoluteHeight.ShouldBe(50);
    }

    // A text sized to its children on both axes measures its height at its max width, or unwrapped
    // when it has none, and restores the renderable's width afterwards.
    [Theory]
    [InlineData(100f, 100f)]
    [InlineData(null, float.PositiveInfinity)]
    public void TextHeight_ShouldBeMeasuredAtMaxWidth_WhenWidthIsRelativeToChildren(float? maxWidth, float expectedMeasureWidth)
    {
        WidthRecordingText renderable = new();
        GraphicalUiElement text = new(renderable);
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.HeightUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;
        text.Height = 0;
        text.MaxWidth = maxWidth;

        text.UpdateLayout();

        renderable.WidthWhenHeightWasRead.ShouldBe(expectedMeasureWidth);
    }

    // Some platforms throw while measuring a text; the width falls back to 64.
    [Fact]
    public void TextWidth_ShouldFallBackTo64_WhenMeasuringThrowsBadImageFormat()
    {
        GraphicalUiElement text = new(new ThrowingText());
        text.WidthUnits = DimensionUnitType.RelativeToChildren;
        text.Width = 0;

        text.UpdateLayout();

        text.AbsoluteWidth.ShouldBe(64);
    }

    // The ratio child gets the height left after each sibling, whatever unit the sibling's height uses.
    [Theory]
    [InlineData(DimensionUnitType.AbsoluteMultipliedByFontScale, 25f, 250f)]
    [InlineData(DimensionUnitType.RelativeToParent, -100f, 100f)]
    [InlineData(DimensionUnitType.PercentageOfOtherDimension, 50f, 280f)]
    [InlineData(DimensionUnitType.ScreenPixel, 60f, 240f)]
    [InlineData(DimensionUnitType.RelativeToChildren, 0f, 270f)]
    public void RatioHeight_ShouldSubtractSiblingsOfEveryUnit(DimensionUnitType siblingUnits, float siblingHeight, float expectedRatioHeight)
    {
        GraphicalUiElement.GlobalFontScale = 2;
        ContainerRuntime parent = CreateContainer(100, 300);
        parent.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        ContainerRuntime sibling = CreateContainer(40, 0);
        sibling.HeightUnits = siblingUnits;
        sibling.Height = siblingHeight;
        if (siblingUnits == DimensionUnitType.RelativeToChildren)
        {
            sibling.AddChild(CreateContainer(10, 30));
        }
        parent.AddChild(sibling);
        ContainerRuntime ratio = CreateContainer(100, 1);
        ratio.HeightUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratio);

        ratio.AbsoluteHeight.ShouldBe(expectedRatioHeight);
    }

    #endregion

    #region Visibility, positions and public offsets

    // A Ratio element made visible lays out its parent again so the siblings share the new split.
    [Fact]
    public void RatioChild_ShouldReclaimSpace_WhenMadeVisibleAgain()
    {
        ContainerRuntime parent = CreateContainer(300, 100);
        parent.ChildrenLayout = ChildrenLayout.LeftToRightStack;
        parent.AddChild(CreateContainer(100, 50));
        ContainerRuntime ratio = CreateContainer(1, 50);
        ratio.WidthUnits = DimensionUnitType.Ratio;
        parent.AddChild(ratio);
        ContainerRuntime after = CreateContainer(20, 50);
        parent.AddChild(after);
        ratio.AbsoluteWidth.ShouldBe(180);

        ratio.Visible = false;
        after.AbsoluteLeft.ShouldBe(100);

        ratio.Visible = true;

        ratio.AbsoluteWidth.ShouldBe(180);
        after.AbsoluteLeft.ShouldBe(280);
    }

    // Without a texture, a PercentageOfFile Y falls back to a 64 pixel file.
    [Fact]
    public void PercentageOfFileY_ShouldUseFallbackFile_WhenThereIsNoTexture()
    {
        ContainerRuntime element = new();
        element.YUnits = GeneralUnitType.PercentageOfFile;
        element.Y = 50;

        element.AbsoluteTop.ShouldBe(32);
    }

    [Fact]
    public void GetParentOffsets_ShouldReportTheOffsetInsideAStackingParent()
    {
        ContainerRuntime parent = CreateContainer(400, 400);
        parent.ChildrenLayout = ChildrenLayout.TopToBottomStack;
        parent.StackSpacing = 5;
        parent.AddChild(CreateContainer(50, 40));
        ContainerRuntime second = CreateContainer(50, 40);
        parent.AddChild(second);

        second.GetParentOffsets(out float offsetX, out float offsetY);

        offsetX.ShouldBe(0);
        offsetY.ShouldBe(45);
        second.AbsoluteTop.ShouldBe(45);
    }

    #endregion

    #region Dock and Anchor on text

    // On a text, Dock and Anchor also set the alignment so the text sits against the same edge.
    [Theory]
    [InlineData(Anchor.TopLeft, HorizontalAlignment.Left, VerticalAlignment.Top)]
    [InlineData(Anchor.Top, HorizontalAlignment.Center, VerticalAlignment.Top)]
    [InlineData(Anchor.TopRight, HorizontalAlignment.Right, VerticalAlignment.Top)]
    [InlineData(Anchor.Left, HorizontalAlignment.Left, VerticalAlignment.Center)]
    [InlineData(Anchor.Center, HorizontalAlignment.Center, VerticalAlignment.Center)]
    [InlineData(Anchor.Right, HorizontalAlignment.Right, VerticalAlignment.Center)]
    [InlineData(Anchor.BottomLeft, HorizontalAlignment.Left, VerticalAlignment.Bottom)]
    [InlineData(Anchor.Bottom, HorizontalAlignment.Center, VerticalAlignment.Bottom)]
    [InlineData(Anchor.BottomRight, HorizontalAlignment.Right, VerticalAlignment.Bottom)]
    public void AnchorOnText_ShouldSetAlignment(Anchor anchor, HorizontalAlignment expectedHorizontal, VerticalAlignment expectedVertical)
    {
        TextRuntime text = new();

        text.Anchor(anchor);

        text.HorizontalAlignment.ShouldBe(expectedHorizontal);
        text.VerticalAlignment.ShouldBe(expectedVertical);
    }

    [Theory]
    [InlineData(Anchor.CenterHorizontally, HorizontalAlignment.Center, VerticalAlignment.Top)]
    [InlineData(Anchor.CenterVertically, HorizontalAlignment.Left, VerticalAlignment.Center)]
    public void SingleAxisAnchorOnText_ShouldSetOnlyThatAlignment(Anchor anchor, HorizontalAlignment expectedHorizontal, VerticalAlignment expectedVertical)
    {
        TextRuntime text = new();
        text.HorizontalAlignment = HorizontalAlignment.Left;
        text.VerticalAlignment = VerticalAlignment.Top;

        text.Anchor(anchor);

        text.HorizontalAlignment.ShouldBe(expectedHorizontal);
        text.VerticalAlignment.ShouldBe(expectedVertical);
    }

    [Theory]
    [InlineData(Dock.Left, HorizontalAlignment.Left, VerticalAlignment.Center)]
    [InlineData(Dock.Right, HorizontalAlignment.Right, VerticalAlignment.Center)]
    [InlineData(Dock.Top, HorizontalAlignment.Center, VerticalAlignment.Top)]
    [InlineData(Dock.Bottom, HorizontalAlignment.Center, VerticalAlignment.Bottom)]
    [InlineData(Dock.Fill, HorizontalAlignment.Center, VerticalAlignment.Center)]
    public void DockOnText_ShouldSetAlignment(Dock dock, HorizontalAlignment expectedHorizontal, VerticalAlignment expectedVertical)
    {
        TextRuntime text = new();

        text.Dock(dock);

        text.HorizontalAlignment.ShouldBe(expectedHorizontal);
        text.VerticalAlignment.ShouldBe(expectedVertical);
    }

    [Theory]
    [InlineData(Dock.FillHorizontally, HorizontalAlignment.Center, VerticalAlignment.Top)]
    [InlineData(Dock.FillVertically, HorizontalAlignment.Left, VerticalAlignment.Center)]
    public void SingleAxisDockOnText_ShouldSetOnlyThatAlignment(Dock dock, HorizontalAlignment expectedHorizontal, VerticalAlignment expectedVertical)
    {
        TextRuntime text = new();
        text.HorizontalAlignment = HorizontalAlignment.Left;
        text.VerticalAlignment = VerticalAlignment.Top;

        text.Dock(dock);

        text.HorizontalAlignment.ShouldBe(expectedHorizontal);
        text.VerticalAlignment.ShouldBe(expectedVertical);
    }

    [Fact]
    public void GetDockAndGetAnchor_ShouldReturnNull_WhenValuesMatchNothing()
    {
        ContainerRuntime element = new();
        element.X = 7;
        element.Y = 7;
        element.XUnits = GeneralUnitType.Percentage;
        element.YUnits = GeneralUnitType.Percentage;

        element.GetDock().ShouldBeNull();
        element.GetAnchor().ShouldBeNull();
    }

    // Centering on one axis reads back as that single-axis anchor only when the other axis matches no edge.
    [Fact]
    public void GetAnchor_ShouldReadSingleAxisCentering_WhenOtherAxisMatchesNoEdge()
    {
        ContainerRuntime horizontal = new();
        horizontal.YUnits = GeneralUnitType.Percentage;
        horizontal.Y = 7;
        horizontal.Anchor(Anchor.CenterHorizontally);
        ContainerRuntime vertical = new();
        vertical.XUnits = GeneralUnitType.Percentage;
        vertical.X = 7;
        vertical.Anchor(Anchor.CenterVertically);

        horizontal.GetAnchor().ShouldBe(Anchor.CenterHorizontally);
        vertical.GetAnchor().ShouldBe(Anchor.CenterVertically);
    }

    [Fact]
    public void Dock_ShouldThrow_WhenValueIsNotADock()
    {
        ContainerRuntime element = new();

        Should.Throw<NotImplementedException>(() => element.Dock((Dock)99));
    }

    #endregion

    #region Elements without a renderable (a Screen) and edge sizes

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

    [Fact]
    public void RenderablelessScreen_ShouldSuspendAndResumeItsContainedChildren_Recursively()
    {
        GraphicalUiElement screen = new(null);
        GraphicalUiElement child = CreateParentlessChild(screen, 50, 50);

        screen.SuspendLayout(true);
        child.IsLayoutSuspended.ShouldBeTrue();
        child.Width = 70;
        child.AbsoluteWidth.ShouldBe(50);

        screen.ResumeLayout(true);

        child.IsLayoutSuspended.ShouldBeFalse();
        child.AbsoluteWidth.ShouldBe(70);
    }

    // An AutoGrid screen sized to its children adds rows for the extra children, so each cell stays
    // one row tall: the canvas is 600 tall, so two rows are 300 each.
    [Fact]
    public void RenderablelessAutoGrid_ShouldAddRowsForExtraChildren_WhenHeightIsRelativeToChildren()
    {
        GraphicalUiElement screen = new(null);
        screen.ChildrenLayout = ChildrenLayout.AutoGridHorizontal;
        screen.AutoGridHorizontalCells = 2;
        screen.AutoGridVerticalCells = 1;
        screen.HeightUnits = DimensionUnitType.RelativeToChildren;
        GraphicalUiElement first = CreateParentlessChild(screen, 10, 10);
        GraphicalUiElement second = CreateParentlessChild(screen, 10, 10);
        GraphicalUiElement third = CreateParentlessChild(screen, 10, 10);

        screen.UpdateLayout();

        second.AbsoluteLeft.ShouldBe(400);
        third.AbsoluteLeft.ShouldBe(0);
        third.AbsoluteTop.ShouldBe(300);
    }

    // Children that depend on the screen's canvas size are sized once the screen's own size is known,
    // while the ones sized by themselves are measured first.
    [Fact]
    public void RenderablelessScreen_ShouldSizeCanvasRelativeChildren_WhenScreenIsRelativeToChildren()
    {
        GraphicalUiElement screen = new(null);
        screen.WidthUnits = DimensionUnitType.RelativeToChildren;
        screen.HeightUnits = DimensionUnitType.RelativeToChildren;
        GraphicalUiElement half = CreateParentlessChild(screen, 50, 40);
        half.WidthUnits = DimensionUnitType.PercentageOfParent;
        GraphicalUiElement ignored = CreateParentlessChild(screen, 30, 30);
        ignored.IgnoredByParentSize = true;

        screen.UpdateLayout();

        half.AbsoluteWidth.ShouldBe(400);
        half.AbsoluteHeight.ShouldBe(40);
        ignored.AbsoluteWidth.ShouldBe(30);
    }

    // A text placed from its baseline counts from its baseline up to the descender in a parent sized
    // to its children.
    [Fact]
    public void RelativeToChildrenParent_ShouldMeasureTextBaselineChild_FromItsBaseline()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        FakeText renderable = new() { WrappedTextHeight = 30, DescenderHeight = 4 };
        GraphicalUiElement text = new(renderable);
        text.Height = 30;
        text.Y = 50;
        text.YOrigin = VerticalAlignment.TextBaseline;
        parent.AddChild(text);

        parent.AbsoluteHeight.ShouldBe(50);
    }

    // A child that has no renderable yet contributes nothing to a parent sized to its children.
    [Fact]
    public void RelativeToChildrenParent_ShouldIgnoreChildWithoutRenderable()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        parent.AddChild(CreateContainer(30, 20));
        GraphicalUiElement empty = new(null);

        parent.Children.Add(empty);
        parent.UpdateLayout();

        parent.AbsoluteWidth.ShouldBe(30);
        parent.AbsoluteHeight.ShouldBe(20);
    }

    // A renderable that is visible but is not an IRenderableIpso leaves the element without a
    // positioned object, so it has no edges to report to a parent sized to its children.
    class VisibleOnlyRenderable : IRenderable, IVisible
    {
        public Gum.BlendState? BlendState => null;
        public bool Wrap => false;
        public void Render(ISystemManagers managers) { }
        public void PreRender() { }
        public string BatchKey => string.Empty;
        public object? BatchSortKey => null;
        public void StartBatch(ISystemManagers systemManagers) { }
        public void EndBatch(ISystemManagers systemManagers) { }
        public bool Visible { get; set; } = true;
        public IVisible? Parent => null;
        public bool AbsoluteVisible => Visible;
    }

    [Fact]
    public void RelativeToChildrenParent_ShouldIgnoreVisibleChildWithoutPositionedObject()
    {
        ContainerRuntime parent = new();
        parent.WidthUnits = DimensionUnitType.RelativeToChildren;
        parent.HeightUnits = DimensionUnitType.RelativeToChildren;
        parent.Width = 0;
        parent.Height = 0;
        parent.AddChild(CreateContainer(30, 20));
        GraphicalUiElement visibleOnly = new(new VisibleOnlyRenderable());

        parent.Children.Add(visibleOnly);
        parent.UpdateLayout();

        parent.AbsoluteWidth.ShouldBe(30);
        parent.AbsoluteHeight.ShouldBe(20);
    }

    #endregion

    #region Public UpdateWidth and UpdateHeight

    // The public methods write the measured size to the renderable, so with none they throw.
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void UpdateWidthAndHeight_ShouldThrow_WhenRelativeToChildrenElementHasNoRenderable(bool isWidth)
    {
        GraphicalUiElement element = new(null);
        element.WidthUnits = DimensionUnitType.RelativeToChildren;
        element.HeightUnits = DimensionUnitType.RelativeToChildren;
        ContainerRuntime child = CreateContainer(30, 20);
        child.ElementGueContainingThis = element;

        Should.Throw<InvalidOperationException>(() =>
        {
            if (isWidth)
            {
                element.UpdateWidth(100, considerWrappedStacked: true);
            }
            else
            {
                element.UpdateHeight(100, considerWrappedStacked: true);
            }
        });
    }

    #endregion

    #region Invalid canvas size (diagnostics build)

    // The canvas size is a static property that accepts any value, so non-finite values reach layout.
    // These checks exist only when GumCommon is built with FULL_DIAGNOSTICS.
    [Fact]
    public void UpdateLayout_ShouldThrow_WhenCanvasHeightIsPositiveInfinity()
    {
        ContainerRuntime element = CreateContainer(100, 100);
        GraphicalUiElement.CanvasHeight = float.PositiveInfinity;

        Should.Throw<Exception>(() => element.UpdateLayout());
    }

    [Fact]
    public void UpdateLayout_ShouldThrow_WhenCanvasHeightIsNegativeInfinity()
    {
        ContainerRuntime element = CreateContainer(100, 100);
        GraphicalUiElement.CanvasHeight = float.NegativeInfinity;

        Should.Throw<ArgumentException>(() => element.UpdateLayout());
    }

    [Fact]
    public void UpdateLayout_ShouldThrow_WhenPercentageXIsMeasuredAgainstNaNCanvasWidth()
    {
        ContainerRuntime element = CreateContainer(100, 100);
        element.XUnits = GeneralUnitType.Percentage;
        GraphicalUiElement.CanvasWidth = float.NaN;

        Exception exception = Should.Throw<Exception>(() => element.UpdateLayout());
        exception.Message.ShouldContain("AdjustOffsetsByUnits");
        exception.Message.ShouldContain("unitOffsetX");
    }

    [Fact]
    public void UpdateLayout_ShouldThrow_WhenPercentageYIsMeasuredAgainstNaNCanvasHeight()
    {
        ContainerRuntime element = CreateContainer(100, 100);
        element.YUnits = GeneralUnitType.Percentage;
        GraphicalUiElement.CanvasHeight = float.NaN;

        Exception exception = Should.Throw<Exception>(() => element.UpdateLayout());
        exception.Message.ShouldContain("AdjustOffsetsByUnits");
        exception.Message.ShouldContain("unitOffsetY");
    }

    [Fact]
    public void UpdateLayout_ShouldThrow_WhenCenteredYOriginIsMeasuredAgainstNaNHeight()
    {
        ContainerRuntime element = CreateContainer(100, 100);
        element.HeightUnits = DimensionUnitType.PercentageOfParent;
        element.YOrigin = VerticalAlignment.Center;
        GraphicalUiElement.CanvasHeight = float.NaN;

        Exception exception = Should.Throw<Exception>(() => element.UpdateLayout());
        exception.Message.ShouldContain("AdjustOffsetsByOrigin");
        exception.Message.ShouldContain("unitOffsetY");
    }

    [Fact]
    public void UpdateLayout_ShouldThrow_WhenRotatedCenteredYOriginIsMeasuredAgainstNaNHeight()
    {
        ContainerRuntime element = CreateContainer(100, 100);
        element.HeightUnits = DimensionUnitType.PercentageOfParent;
        element.YOrigin = VerticalAlignment.Center;
        element.Rotation = 45;
        GraphicalUiElement.CanvasHeight = float.NaN;

        Exception exception = Should.Throw<Exception>(() => element.UpdateLayout());
        exception.Message.ShouldContain("AdjustOffsetsByOrigin");
        exception.Message.ShouldContain("unitOffsetX");
    }

    #endregion
}
