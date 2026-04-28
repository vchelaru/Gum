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
    public void ClearStickyHeaders_ShouldRestoreAllHeaders()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header1 = MakeStickyHeader(20);
        scrollViewer.AddChild(header1);
        ContainerRuntime header2 = MakeStickyHeader(20);
        scrollViewer.AddChild(header2);

        scrollViewer.RegisterStickyHeader(header1);
        scrollViewer.RegisterStickyHeader(header2);

        scrollViewer.ClearStickyHeaders();

        header1.Parent.ShouldBe(scrollViewer.InnerPanel);
        header2.Parent.ShouldBe(scrollViewer.InnerPanel);
        scrollViewer.StickyHeaderOverlay!.Children.Count.ShouldBe(0);
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
        scrollViewer.AddChild(header);

        scrollViewer.RegisterStickyHeader(header);

        header.Visual.Parent.ShouldBe(scrollViewer.StickyHeaderOverlay);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldBumpFirstHeader_WhenSecondHeaderApproachesTop()
    {
        const float headerHeight = 20f;
        const float middleItemHeight = 100f;

        ScrollViewer scrollViewer = BuildScrollViewerWithTwoSections(headerHeight, middleItemHeight,
            out ContainerRuntime header1,
            out ContainerRuntime header2);

        // Scroll until placeholder2 sits at +10 from overlay top; header1
        // (height 20) must then be at -10 to make room.
        scrollViewer.VerticalScrollBarValue = 110;

        float overlayTop = scrollViewer.StickyHeaderOverlay!.AbsoluteTop;
        (header1.AbsoluteTop - overlayTop).ShouldBe(-10f, tolerance: 0.5f);
        (header2.AbsoluteTop - overlayTop).ShouldBe(10f, tolerance: 0.5f);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldInsertPlaceholderAtHeaderIndex()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime first = MakeStickyHeader(10);
        scrollViewer.AddChild(first);
        ContainerRuntime header = MakeStickyHeader(30);
        scrollViewer.AddChild(header);
        ContainerRuntime last = MakeStickyHeader(10);
        scrollViewer.AddChild(last);

        scrollViewer.RegisterStickyHeader(header);

        // Header is now in the overlay; a placeholder should occupy index 1
        // in the inner panel between `first` and `last`, and have the header's height.
        scrollViewer.InnerPanel.Children.Count.ShouldBe(3);
        scrollViewer.InnerPanel.Children[0].ShouldBe(first);
        scrollViewer.InnerPanel.Children[2].ShouldBe(last);

        GraphicalUiElement placeholder = (GraphicalUiElement)scrollViewer.InnerPanel.Children[1];
        placeholder.ShouldNotBe(header);
        placeholder.Height.ShouldBe(30f);
        placeholder.HeightUnits.ShouldBe(global::Gum.DataTypes.DimensionUnitType.Absolute);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldPinHeader_WhenScrolledPastNaturalPosition()
    {
        const float headerHeight = 20f;
        const float middleItemHeight = 100f;

        ScrollViewer scrollViewer = BuildScrollViewerWithTwoSections(headerHeight, middleItemHeight,
            out ContainerRuntime header1,
            out ContainerRuntime header2);

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
        scrollViewer.AddChild(header);

        scrollViewer.RegisterStickyHeader(header);

        header.Parent.ShouldBe(scrollViewer.StickyHeaderOverlay);
        scrollViewer.StickyHeaderOverlay!.Children.ShouldContain(header);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldSyncPlaceholderHeight_WhenHeaderResizes()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(33);
        scrollViewer.AddChild(header);

        scrollViewer.RegisterStickyHeader(header);

        GraphicalUiElement placeholder = (GraphicalUiElement)scrollViewer.InnerPanel.Children[0];
        placeholder.Height.ShouldBe(33f);

        header.Height = 77;
        placeholder.Height.ShouldBe(77f);
    }

    [Fact]
    public void RegisterStickyHeader_ShouldThrow_WhenHeaderIsNotChildOfInnerPanel()
    {
        ScrollViewer scrollViewer = new();
        ContainerRuntime header = MakeStickyHeader(20);
        // Note: header was NOT added to the scroll viewer.

        Should.Throw<ArgumentException>(() => scrollViewer.RegisterStickyHeader(header));
    }

    [Fact]
    public void RegisterStickyHeader_AfterUnregister_ShouldNotLeaveResidualPlaceholders()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime first = MakeStickyHeader(10);
        scrollViewer.AddChild(first);
        ContainerRuntime header = MakeStickyHeader(20);
        scrollViewer.AddChild(header);
        ContainerRuntime last = MakeStickyHeader(10);
        scrollViewer.AddChild(last);

        // Cycle through register / unregister several times. Each cycle should
        // leave the inner panel with exactly the original three children and no
        // leftover placeholders, and the overlay should only contain the header
        // while registered.
        for (int i = 0; i < 3; i++)
        {
            scrollViewer.RegisterStickyHeader(header);

            scrollViewer.InnerPanel.Children.Count.ShouldBe(3);
            scrollViewer.InnerPanel.Children[0].ShouldBe(first);
            scrollViewer.InnerPanel.Children[2].ShouldBe(last);
            scrollViewer.InnerPanel.Children[1].ShouldNotBe(header); // it's the placeholder
            scrollViewer.StickyHeaderOverlay!.Children.Count.ShouldBe(1);
            scrollViewer.StickyHeaderOverlay.Children[0].ShouldBe(header);

            scrollViewer.UnregisterStickyHeader(header);

            scrollViewer.InnerPanel.Children.Count.ShouldBe(3);
            scrollViewer.InnerPanel.Children[0].ShouldBe(first);
            scrollViewer.InnerPanel.Children[1].ShouldBe(header);
            scrollViewer.InnerPanel.Children[2].ShouldBe(last);
            scrollViewer.StickyHeaderOverlay!.Children.Count.ShouldBe(0);
        }
    }

    [Fact]
    public void RegisterStickyHeader_AfterReorderingBetweenUnregisterAndRegister_ShouldPinAtNewPosition()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime first = MakeStickyHeader(10);
        scrollViewer.AddChild(first);
        ContainerRuntime header = MakeStickyHeader(20);
        scrollViewer.AddChild(header);
        ContainerRuntime last = MakeStickyHeader(10);
        scrollViewer.AddChild(last);

        scrollViewer.RegisterStickyHeader(header);
        scrollViewer.UnregisterStickyHeader(header);

        // Move header from index 1 to index 0 in the inner panel.
        scrollViewer.InnerPanel.Children.Remove(header);
        scrollViewer.InnerPanel.Children.Insert(0, header);

        scrollViewer.RegisterStickyHeader(header);

        // Placeholder should now occupy index 0, and the original `first`
        // child has shifted to index 1.
        scrollViewer.InnerPanel.Children.Count.ShouldBe(3);
        scrollViewer.InnerPanel.Children[0].ShouldNotBe(header); // placeholder at the new slot
        scrollViewer.InnerPanel.Children[1].ShouldBe(first);
        scrollViewer.InnerPanel.Children[2].ShouldBe(last);
        scrollViewer.StickyHeaderOverlay!.Children.Count.ShouldBe(1);
        scrollViewer.StickyHeaderOverlay.Children[0].ShouldBe(header);
    }

    [Fact]
    public void RemoveChild_OnRegisteredStickyHeader_ShouldRemoveBothHeaderAndPlaceholder()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(20);
        scrollViewer.AddChild(header);
        scrollViewer.RegisterStickyHeader(header);

        // Sanity check: placeholder is in the inner panel, header is in the overlay.
        scrollViewer.InnerPanel.Children.Count.ShouldBe(1);
        scrollViewer.StickyHeaderOverlay!.Children.ShouldContain(header);

        scrollViewer.RemoveChild(header);

        // Both should be gone.
        scrollViewer.InnerPanel.Children.Count.ShouldBe(0);
        scrollViewer.StickyHeaderOverlay.Children.ShouldNotContain(header);
    }

    [Fact]
    public void RecomputeStickyHeaders_ShouldDropStaleRegistration_WhenPlaceholderRemovedExternally()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(20);
        scrollViewer.AddChild(header);
        scrollViewer.RegisterStickyHeader(header);

        // User reaches past the API and rips the placeholder out of the stack.
        GraphicalUiElement placeholder = (GraphicalUiElement)scrollViewer.InnerPanel.Children[0];
        scrollViewer.InnerPanel.Children.Remove(placeholder);

        // Trigger a recompute via a visual size change. The defensive cleanup
        // should drop the stale entry (and unsubscribe its SizeChanged handler)
        // without throwing.
        Should.NotThrow(() => scrollViewer.Visual.Height = 250);

        // After the cleanup, resizing the header must not write back to the
        // detached placeholder.
        header.Height = 99;
        placeholder.Height.ShouldBe(20f);
    }

    [Fact]
    public void StickyHeaderOverlay_ShouldBeAvailable_ForDefaultVisual()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.StickyHeaderOverlay.ShouldNotBeNull();
        scrollViewer.StickyHeaderOverlay!.Name.ShouldBe("StickyHeaderOverlayInstance");
    }

    [Fact]
    public void UnregisterStickyHeader_ShouldRestoreHeaderToPlaceholderSlot()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime first = MakeStickyHeader(10);
        scrollViewer.AddChild(first);
        ContainerRuntime header = MakeStickyHeader(20);
        scrollViewer.AddChild(header);
        ContainerRuntime last = MakeStickyHeader(10);
        scrollViewer.AddChild(last);

        scrollViewer.RegisterStickyHeader(header);
        scrollViewer.UnregisterStickyHeader(header);

        // The header is back in the inner panel at its original index, the
        // placeholder is gone, and the overlay no longer contains the header.
        scrollViewer.InnerPanel.Children.Count.ShouldBe(3);
        scrollViewer.InnerPanel.Children[0].ShouldBe(first);
        scrollViewer.InnerPanel.Children[1].ShouldBe(header);
        scrollViewer.InnerPanel.Children[2].ShouldBe(last);
        scrollViewer.StickyHeaderOverlay!.Children.ShouldNotContain(header);
    }

    [Fact]
    public void AddStickyHeaderTerminator_ShouldBumpPreviousHeader_WhenScrolledTo()
    {
        const float headerHeight = 20f;
        const float itemHeight = 100f;

        ScrollViewer scrollViewer = new();
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(headerHeight);
        scrollViewer.AddChild(header);

        ContainerRuntime item = new();
        item.Width = 0;
        item.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        item.Height = itemHeight;
        item.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(item);

        scrollViewer.RegisterStickyHeader(header);
        scrollViewer.AddStickyHeaderTerminator();

        // Tail content so we can scroll the terminator up to the top.
        ContainerRuntime tail = new();
        tail.Width = 0;
        tail.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        tail.Height = 200;
        tail.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(tail);

        scrollViewer.UpdateVerticalScrollBarValues();

        // Terminator's natural Y from the overlay = headerHeight + itemHeight = 120.
        // After scrolling 110, terminator sits at +10, so the header (20 tall)
        // must be bumped to -10.
        scrollViewer.VerticalScrollBarValue = 110;

        float overlayTop = scrollViewer.StickyHeaderOverlay!.AbsoluteTop;
        (header.AbsoluteTop - overlayTop).ShouldBe(-10f, tolerance: 0.5f);
    }

    [Fact]
    public void AddStickyHeaderTerminator_ShouldAddInvisibleZeroHeightMarkerToInnerPanel()
    {
        ScrollViewer scrollViewer = new();
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        int beforeCount = scrollViewer.InnerPanel.Children.Count;
        GraphicalUiElement? marker = scrollViewer.AddStickyHeaderTerminator();

        marker.ShouldNotBeNull();
        scrollViewer.InnerPanel.Children.Count.ShouldBe(beforeCount + 1);
        scrollViewer.InnerPanel.Children[scrollViewer.InnerPanel.Children.Count - 1].ShouldBe(marker);
        marker!.Height.ShouldBe(0f);
    }

    [Fact]
    public void RemoveChild_OnTerminator_ShouldDropTerminatorRegistration()
    {
        const float headerHeight = 20f;
        const float itemHeight = 100f;

        ScrollViewer scrollViewer = new();
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        ContainerRuntime header = MakeStickyHeader(headerHeight);
        scrollViewer.AddChild(header);

        ContainerRuntime item = new();
        item.Width = 0;
        item.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        item.Height = itemHeight;
        item.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(item);

        scrollViewer.RegisterStickyHeader(header);
        GraphicalUiElement terminator = scrollViewer.AddStickyHeaderTerminator()!;

        // Tail so we have somewhere to scroll.
        ContainerRuntime tail = new();
        tail.Width = 0;
        tail.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        tail.Height = 200;
        tail.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(tail);

        scrollViewer.UpdateVerticalScrollBarValues();

        // Remove the terminator. The header should now pin without ever being
        // bumped, even when scrolled far past where the terminator used to be.
        scrollViewer.RemoveChild(terminator);
        scrollViewer.InnerPanel.Children.ShouldNotContain(terminator);

        scrollViewer.VerticalScrollBarValue = 110;

        float overlayTop = scrollViewer.StickyHeaderOverlay!.AbsoluteTop;
        (header.AbsoluteTop - overlayTop).ShouldBe(0f, tolerance: 0.5f);
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
        out ContainerRuntime header2)
    {
        ScrollViewer scrollViewer = new();
        // Hide scroll bars so they don't add layout margins that confuse the math.
        scrollViewer.VerticalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.HorizontalScrollBarVisibility = ScrollBarVisibility.Hidden;
        scrollViewer.Visual.Width = 200;
        scrollViewer.Visual.Height = 200;

        header1 = MakeStickyHeader(headerHeight);
        scrollViewer.AddChild(header1);

        ContainerRuntime middle = new();
        middle.Width = 0;
        middle.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        middle.Height = middleItemHeight;
        middle.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(middle);

        header2 = MakeStickyHeader(headerHeight);
        scrollViewer.AddChild(header2);

        ContainerRuntime tail = new();
        tail.Width = 0;
        tail.WidthUnits = global::Gum.DataTypes.DimensionUnitType.RelativeToParent;
        tail.Height = 200;
        tail.HeightUnits = global::Gum.DataTypes.DimensionUnitType.Absolute;
        scrollViewer.AddChild(tail);

        scrollViewer.RegisterStickyHeader(header1);
        scrollViewer.RegisterStickyHeader(header2);

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
