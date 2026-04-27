using Gum.Forms.Controls;
using Gum.Wireframe;
using Microsoft.Xna.Framework.Input;
using MonoGameGum.GueDeriving;
using MonoGameGum.Input;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ScrollViewerTests : BaseTestClass
{

    [Fact]
    public void Children_Containers_ShouldNotHaveEvents()
    {
        ScrollViewer scrollViewer = new();
        InteractiveGue visual = scrollViewer.Visual;

        List<ContainerRuntime> children = new();
        visual.FillListWithChildrenByTypeRecursively<ContainerRuntime>(children);

        foreach (var child in children)
        {
            // ThumbContainer is used by ScrollBar for clicking to change value
            if(child.Name != "ThumbContainer")
            {
                child.HasEvents.ShouldBeFalse(
                    $"Because child {child.Name} with parent {child.Parent?.Name} should not be clickable, but it is so it eats events");
            }
        }
    }

    [Fact]
    public void DoItemsHaveFoucus_SetToFalse_ShouldRemoveFocusFromItems()
    {
        ScrollViewer scrollViewer = new();

        Button button1 = new();
        scrollViewer.AddChild(button1);

        Button button2 = new();
        scrollViewer.AddChild(button2);

        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;

        button1.IsFocused.ShouldBeTrue();
        scrollViewer.DoItemsHaveFocus = false;

        button1.IsFocused.ShouldBeFalse();
        scrollViewer.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void DoItemsHaveFocus_Set_ShouldGiveFocusToFirstItem()
    {

        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);


        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;
        button1.IsFocused.ShouldBeTrue();
    }


    [Fact]
    public void EnterInput_ShouldGiveFocusToFirstItem()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);


        scrollViewer.IsFocused = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(m => m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Enter))
            .Returns(true);
        // KeyCombo.IsComboPushed routes through the Gum.Forms.Input.Keys overload.
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(m => m.KeyPushed(Gum.Forms.Input.Keys.Enter))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        scrollViewer.OnFocusUpdate();

        button1.IsFocused.ShouldBeTrue();
    }

    [Fact]
    public void EscInput_ShouldRemoveFocusFromItems()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);
        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;

        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = new ();
        mockKeyboard
            .Setup(m => m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Escape))
            .Returns(true);
        mockKeyboard
            .Setup(m => m.KeysTyped)
            .Returns(new List<Keys>());
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        Mock<ICursor> mockCursor = new ();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            scrollViewer.Visual, 
            mockCursor.Object, 
            mockKeyboard.Object, 
            0);

        scrollViewer.IsFocused.ShouldBeTrue();
        scrollViewer.DoItemsHaveFocus.ShouldBeFalse();
    }

    [Fact]
    public void IsFocused_ShouldBeTrue_WhenReceivingTab()
    {
        StackPanel parent = new();

        Button button1 = new();
        parent.AddChild(button1);

        ScrollViewer scrollViewer = new();
        parent.AddChild(scrollViewer);

        Button button2 = new();
        parent.AddChild(button2);

        button1.IsFocused = true;
        button1.HandleTab();

        scrollViewer.IsFocused.ShouldBeTrue();
        (InteractiveGue.CurrentInputReceiver == scrollViewer).ShouldBeTrue();
    }

    [Fact]
    public void ReceiveTab_ShouldSkipInternalItems()
    {
        StackPanel parent = new();

        ScrollViewer scrollViewer = new();
        parent.AddChild(scrollViewer);

        Button button1 = new();
        scrollViewer.AddChild(button1);

        Button button2 = new();
        parent.AddChild(button2);

        scrollViewer.IsFocused = true;

        var mockKeyboard = new Mock<IInputReceiverKeyboardMonoGame>();
        mockKeyboard
            .Setup(m=>m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Tab))
            .Returns(true);
        // KeyCombo.IsComboPushed (used for Tab navigation) routes through Gum.Forms.Input.Keys.
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(m => m.KeyPushed(Gum.Forms.Input.Keys.Tab))
            .Returns(true);
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        scrollViewer.OnFocusUpdate();

        scrollViewer.IsFocused.ShouldBeFalse();
        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();

    }

    [Fact]
    public void RemoveChild_ShouldRemoveChildFromScrollViewer()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        scrollViewer.RemoveChild(button1);
        scrollViewer.Visual.Children.ShouldNotContain(button1.Visual);
    }

    [Fact]
    public void TabInput_ShouldMoveFocusToNextItem()
    {
        ScrollViewer scrollViewer = new();
        Button button1 = new();
        scrollViewer.AddChild(button1);
        Button button2 = new();
        scrollViewer.AddChild(button2);
        scrollViewer.IsFocused = true;
        scrollViewer.DoItemsHaveFocus = true;
        button1.IsFocused.ShouldBeTrue();


        Mock<IInputReceiverKeyboardMonoGame> mockKeyboard = new();
        mockKeyboard
            .Setup(m => m.KeyPushed(Microsoft.Xna.Framework.Input.Keys.Tab))
            .Returns(true);
        mockKeyboard
            .Setup(m => m.KeysTyped)
            .Returns(new List<Keys>() { Keys.Tab });
        // KeyCombo.IsComboPushed (used for Tab navigation) routes through Gum.Forms.Input.Keys.
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(m => m.KeyPushed(Gum.Forms.Input.Keys.Tab))
            .Returns(true);
        mockKeyboard.As<Gum.Wireframe.IInputReceiverKeyboard>()
            .Setup(m => m.KeysTyped)
            .Returns(new List<Gum.Forms.Input.Keys>() { Gum.Forms.Input.Keys.Tab });
        FrameworkElement.KeyboardsForUiControl.Add(mockKeyboard.Object);

        Mock<ICursor> mockCursor = new();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            scrollViewer.Visual,
            mockCursor.Object,
            mockKeyboard.Object,
            0);


        button1.IsFocused.ShouldBeFalse();
        button2.IsFocused.ShouldBeTrue();

    }

    #region Sticky Headers

    [Fact]
    public void StickyHeaderOverlay_ShouldBeAvailable_ForDefaultVisual()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.StickyHeaderOverlay.ShouldNotBeNull();
        scrollViewer.StickyHeaderOverlay!.Name.ShouldBe("StickyHeaderOverlayInstance");
    }

    [Fact]
    public void ClearStickyHeaders_ShouldRemoveAllRegistrations()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header1 = MakeStickyHeader(20);
        ContainerRuntime placeholder1 = new();
        scrollViewer.AddChild(placeholder1);

        ContainerRuntime header2 = MakeStickyHeader(20);
        ContainerRuntime placeholder2 = new();
        scrollViewer.AddChild(placeholder2);

        scrollViewer.RegisterStickyHeader(header1, placeholder1);
        scrollViewer.RegisterStickyHeader(header2, placeholder2);

        scrollViewer.ClearStickyHeaders();

        // After clearing, header height changes should no longer touch placeholder
        // (the size-changed handler was unsubscribed).
        placeholder1.Height = 5;
        header1.Height = 99;
        placeholder1.Height.ShouldBe(5);
    }

    [Fact]
    public void RegisterStickyHeader_FrameworkElementOverload_ShouldDelegateToVisualOverload()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        Button header = new();
        header.Visual.Height = 25;
        header.Visual.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;

        Button placeholder = new();
        scrollViewer.AddChild(placeholder);

        scrollViewer.RegisterStickyHeader(header, placeholder);

        header.Visual.Parent.ShouldBe(scrollViewer.StickyHeaderOverlay);
        placeholder.Visual.Height.ShouldBe(25);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldBumpFirstHeader_WhenSecondHeaderApproachesTop()
    {
        const float headerHeight = 20f;
        const float middleItemHeight = 100f;

        ScrollViewer scrollViewer = BuildScrollViewerWithTwoSections(headerHeight, middleItemHeight,
            out ContainerRuntime header1,
            out ContainerRuntime placeholder1,
            out ContainerRuntime header2,
            out ContainerRuntime placeholder2);

        // Scroll far enough that placeholder2's top is within headerHeight of the
        // overlay top — header1 should slide up to make room.
        // Placeholder2 natural offset from overlay = headerHeight + middleItemHeight = 120.
        // After scrolling 110, placeholder2 sits at +10 from overlay top, so header1
        // (height 20) must be at -10.
        scrollViewer.VerticalScrollBarValue = 110;

        float overlayTop = scrollViewer.StickyHeaderOverlay!.AbsoluteTop;
        (header1.AbsoluteTop - overlayTop).ShouldBe(-10f, tolerance: 0.5f);
        // header2 just reached the top
        (header2.AbsoluteTop - overlayTop).ShouldBe(10f, tolerance: 0.5f);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldPinHeader_WhenScrolledPastNaturalPosition()
    {
        const float headerHeight = 20f;
        const float middleItemHeight = 100f;

        ScrollViewer scrollViewer = BuildScrollViewerWithTwoSections(headerHeight, middleItemHeight,
            out ContainerRuntime header1,
            out ContainerRuntime placeholder1,
            out ContainerRuntime header2,
            out ContainerRuntime placeholder2);

        // Scroll past header1's natural position but well before header2 arrives.
        scrollViewer.VerticalScrollBarValue = 50;

        float overlayTop = scrollViewer.StickyHeaderOverlay!.AbsoluteTop;
        (header1.AbsoluteTop - overlayTop).ShouldBe(0f, tolerance: 0.5f);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldReparentHeaderToOverlay()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(30);
        ContainerRuntime placeholder = new();
        scrollViewer.AddChild(placeholder);

        scrollViewer.RegisterStickyHeader(header, placeholder);

        header.Parent.ShouldBe(scrollViewer.StickyHeaderOverlay);
        scrollViewer.StickyHeaderOverlay!.Children.ShouldContain(header);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldSizePlaceholderToHeader()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(33);
        ContainerRuntime placeholder = new();
        scrollViewer.AddChild(placeholder);

        scrollViewer.RegisterStickyHeader(header, placeholder);

        placeholder.HeightUnits.ShouldBe(global::Gum.DataTypes.DimensionUnitType.Absolute);
        placeholder.Height.ShouldBe(33f);

        // Resizing the header should keep the placeholder in sync.
        header.Height = 77;
        placeholder.Height.ShouldBe(77f);
    }

    [Fact]
    public void UnregisterStickyHeader_ShouldStopSyncingPlaceholder()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(20);
        ContainerRuntime placeholder = new();
        scrollViewer.AddChild(placeholder);

        scrollViewer.RegisterStickyHeader(header, placeholder);
        scrollViewer.UnregisterStickyHeader(header);

        // After unregistering, placeholder is no longer driven by header height.
        placeholder.Height = 7;
        header.Height = 99;
        placeholder.Height.ShouldBe(7f);
    }

    private static ContainerRuntime MakeStickyHeader(float height)
    {
        ContainerRuntime header = new();
        header.Width = 0;
        header.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        header.Height = height;
        header.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        return header;
    }

    private static ScrollViewer BuildScrollViewerWithTwoSections(
        float headerHeight,
        float middleItemHeight,
        out ContainerRuntime header1,
        out ContainerRuntime placeholder1,
        out ContainerRuntime header2,
        out ContainerRuntime placeholder2)
    {
        ScrollViewer scrollViewer = new();
        // Hide scroll bars so they don't add layout margins that confuse the math.
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        header1 = MakeStickyHeader(headerHeight);
        placeholder1 = new ContainerRuntime();
        placeholder1.Width = 0;
        placeholder1.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        scrollViewer.AddChild(placeholder1);

        ContainerRuntime middle = new();
        middle.Width = 0;
        middle.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        middle.Height = middleItemHeight;
        middle.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(middle);

        header2 = MakeStickyHeader(headerHeight);
        placeholder2 = new ContainerRuntime();
        placeholder2.Width = 0;
        placeholder2.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        scrollViewer.AddChild(placeholder2);

        ContainerRuntime tail = new();
        tail.Width = 0;
        tail.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        tail.Height = 200;
        tail.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(tail);

        scrollViewer.RegisterStickyHeader(header1, placeholder1);
        scrollViewer.RegisterStickyHeader(header2, placeholder2);

        // Force scroll bar maximum to be recomputed now that content is in place.
        scrollViewer.UpdateVerticalScrollBarValues();

        return scrollViewer;
    }

    #endregion

    #region Visual
    [Fact]
    public void Visual_HasEvents_ShouldBeTrue()
    {
        ScrollViewer sut = new();
        sut.Visual.HasEvents.ShouldBeTrue();
    }

    #endregion
}
