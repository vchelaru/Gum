using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.Input;
using Gum.GueDeriving;
using Gum.Wireframe;
using Moq;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// Once a control's Visual is replaced, events raised by the old Visual must no longer reach the control.
/// </summary>
public class VisualReassignmentTests : BaseTestClass
{
    [Fact]
    public void Button_Reassigned_ShouldIgnoreOldVisualClick()
    {
        ButtonVisual oldVisual = new(tryCreateFormsObject: false);
        Button sut = new(oldVisual);
        sut.Visual = new ButtonVisual(tryCreateFormsObject: false);
        int clickCount = 0;
        sut.Click += (_, _) => clickCount++;

        oldVisual.CallClick();

        clickCount.ShouldBe(0);
    }

    [Fact]
    public void ListBoxItem_Reassigned_ShouldIgnoreOldVisualClick()
    {
        ListBoxItemVisual oldVisual = new(tryCreateFormsObject: false);
        ListBoxItem sut = new(oldVisual);
        sut.Visual = new ListBoxItemVisual(tryCreateFormsObject: false);
        int clickCount = 0;
        sut.Clicked += (_, _) => clickCount++;

        oldVisual.CallClick();

        clickCount.ShouldBe(0);
    }

    [Fact]
    public void ListBoxItem_ListBoxDraggingAssigned_ShouldMoveToNewVisualOnReassign()
    {
        ListBoxItemVisual oldVisual = new(tryCreateFormsObject: false);
        ListBoxItem sut = new(oldVisual);
        int draggingCount = 0;
        sut.AssignListBoxEvents(
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => { },
            (_, _) => draggingCount++);
        ListBoxItemVisual newVisual = new(tryCreateFormsObject: false);
        sut.Visual = newVisual;

        oldVisual.TryCallDragging();
        draggingCount.ShouldBe(0);

        newVisual.TryCallDragging();
        draggingCount.ShouldBe(1);
    }

    [Fact]
    public void TextBox_Reassigned_ShouldIgnoreOldVisualClick()
    {
        TextBoxVisual oldVisual = new(tryCreateFormsObject: false);
        TextBox sut = new(oldVisual);
        sut.Visual = new TextBoxVisual(tryCreateFormsObject: false);
        InteractiveGue.CurrentInputReceiver = null;

        oldVisual.CallClick();

        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void ComboBox_Reassigned_ShouldIgnoreOldVisualPush()
    {
        ComboBoxVisual oldVisual = new(tryCreateFormsObject: false);
        ComboBox sut = new(oldVisual);
        sut.Visual = new ComboBoxVisual(tryCreateFormsObject: false);

        oldVisual.TryCallPush();

        sut.IsDropDownOpen.ShouldBeFalse();
    }

    [Fact]
    public void Splitter_Reassigned_ShouldIgnoreOldVisualPush()
    {
        Mock<ICursor> cursor = new();
        FrameworkElement.MainCursor = cursor.Object;
        SplitterVisual oldVisual = new(tryCreateFormsObject: false);
        Splitter sut = new(oldVisual);
        sut.Visual = new SplitterVisual(tryCreateFormsObject: false);

        oldVisual.TryCallPush();

        cursor.Verify(c => c.XRespectingGumZoomAndBounds(), Times.Never);
    }

    [Fact]
    public void Slider_Reassigned_ShouldIgnoreOldTrackAndThumbRemovedAsPushed()
    {
        SliderVisual oldVisual = new(tryCreateFormsObject: false);
        Slider sut = new(oldVisual);
        sut.IsMoveToPointEnabled = true;
        sut.Visual = new SliderVisual(tryCreateFormsObject: false);
        sut.Value = 50;
        int completedCount = 0;
        sut.ValueChangeCompleted += (_, _) => completedCount++;

        ((InteractiveGue)oldVisual.GetGraphicalUiElementByName("TrackInstance")!).TryCallRemoveAsPushed();
        oldVisual.ThumbInstance!.TryCallRemoveAsPushed();

        completedCount.ShouldBe(0);
    }

    [Fact]
    public void ColorPicker_Reassigned_ShouldIgnoreOldVisualInput()
    {
        Mock<ICursor> cursor = new();
        FrameworkElement.MainCursor = cursor.Object;
        ColorPickerVisual oldVisual = new(tryCreateFormsObject: false);
        ColorPicker sut = new(oldVisual);
        ColorPickerVisual newVisual = new(tryCreateFormsObject: false);
        newVisual.Width = 200;
        newVisual.Height = 200;
        sut.Visual = newVisual;
        newVisual.UpdateLayout();

        ((InteractiveGue)oldVisual.GetGraphicalUiElementByName("HueContainer")!).TryCallPush();
        ((InteractiveGue)oldVisual.GetGraphicalUiElementByName("SaturationValueContainer")!).TryCallPush();

        cursor.Verify(c => c.YRespectingGumZoomAndBounds(), Times.Never);
    }

    [Fact]
    public void ScrollBar_Reassigned_ShouldIgnoreOldUpAndDownButtonPush()
    {
        ScrollBarVisual oldVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar sut = new(oldVisual);
        InteractiveGue oldUpVisual = sut.UpButton!.Visual;
        InteractiveGue oldDownVisual = sut.DownButton!.Visual;
        sut.Visual = new ScrollBarVisual(fullInstantiation: true, tryCreateFormsObject: false);
        sut.Value = 50;

        oldUpVisual.TryCallPush();
        sut.Value.ShouldBe(50);
        oldDownVisual.TryCallPush();
        sut.Value.ShouldBe(50);
    }

    [Fact]
    public void ScrollBar_Reassigned_ShouldIgnoreOldTrackPush()
    {
        Mock<ICursor> cursor = new();
        FrameworkElement.MainCursor = cursor.Object;
        ScrollBarVisual oldVisual = new(fullInstantiation: true, tryCreateFormsObject: false);
        ScrollBar sut = new(oldVisual);
        InteractiveGue oldTrack = sut.Track!;
        sut.Visual = new ScrollBarVisual(fullInstantiation: true, tryCreateFormsObject: false);
        cursor.Invocations.Clear();

        oldTrack.TryCallPush();

        cursor.Verify(c => c.YRespectingGumZoomAndBounds(), Times.Never);
    }

    [Fact]
    public void MenuItem_Reassigned_ShouldIgnoreOldVisualPushClickAndRollOver()
    {
        Mock<ICursor> cursor = new();
        FrameworkElement.MainCursor = cursor.Object;
        MenuItemVisual oldVisual = new(tryCreateFormsObject: false);
        MenuItem sut = new(oldVisual);
        sut.Visual = new MenuItemVisual(tryCreateFormsObject: false);
        cursor.Invocations.Clear();

        oldVisual.TryCallPush();
        oldVisual.CallClick();
        oldVisual.TryCallRollOn();
        oldVisual.TryCallRollOver();

        cursor.Invocations.ShouldBeEmpty();
    }

    [Fact]
    public void MenuItem_Reassigned_ShouldIgnoreOldVisualRollOff()
    {
        MenuItemVisual oldVisual = new(tryCreateFormsObject: false);
        MenuItem sut = new(oldVisual);
        sut.Visual = new MenuItemVisual(tryCreateFormsObject: false);
        sut.IsHighlighted = true;

        oldVisual.TryCallRollOff();

        sut.IsHighlighted.ShouldBeTrue();
    }

    [Fact]
    public void MenuItem_Reassigned_ShouldIgnoreOldSubItemContainerChanges()
    {
        ContainerRuntime oldVisual = new();
        ContainerRuntime oldSubItemContainer = new() { Name = "SubItemContainerInstance" };
        oldVisual.AddChild(oldSubItemContainer);
        MenuItem sut = new(oldVisual);
        sut.Visual = new ContainerRuntime();
        MenuItem subItem = new();

        oldSubItemContainer.Children.Add(subItem.Visual);
        Gum.GumService.Default.DeferredQueue.ProcessPending();

        sut.Items!.Count.ShouldBe(0);
    }
}
