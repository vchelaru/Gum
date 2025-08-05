using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals;
using Gum.Wireframe;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class ListBoxTests
{
    [Fact]
    public void IsEnabled_ShouldSetListBoxItemsDisable_IfSetToFalse()
    {
        bool didSet = false;
        ListBoxItem listBoxItem = new ();
        var disabledState = listBoxItem.GetState(FrameworkElement.DisabledStateName);

        disabledState.Clear();
        disabledState.Apply = () =>
        {
            didSet = true;
        };

        ListBox listBox = new ListBox();
        listBox.Items.Add(listBoxItem);
        didSet.ShouldBeFalse();
        listBox.IsEnabled = false;
        didSet.ShouldBeTrue();

    }

    [Fact]
    public void IsEnabled_ShouldSetListBoxItemAfterDisabled_IfSetToFalse()
    {
        bool didSet = false;
        ListBoxItem listBoxItem = new();
        var disabledState = listBoxItem.GetState(FrameworkElement.DisabledStateName);
        disabledState.Clear();
        disabledState.Apply = () =>
        {
            didSet = true;
        };

        ListBox listBox = new ListBox();
        listBox.IsEnabled = false;
        didSet.ShouldBeFalse();
        listBox.Items.Add(listBoxItem);
        didSet.ShouldBeTrue();
    }

    [Fact]
    public void Click_ShouldNotSelect_IfDisabled()
    {
        ListBox listBox = new();
        listBox.IsEnabled = false;
        listBox.AddToRoot();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }

        Mock<ICursor> mockCursor = SetupForPush();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null,
            0);

        listBox.SelectedObject.ShouldBeNull();
    }

    [Fact]
    public void Click_ShouldSelect_IfEnabled()
    {
        ListBox listBox = new();
        listBox.AddToRoot();
        for (int i = 0; i < 10; i++)
        {
            listBox.Items.Add("Item " + i);
        }

        Mock<ICursor> mockCursor = SetupForPush();

        GueInteractiveExtensionMethods.DoUiActivityRecursively(
            listBox.Visual,
            mockCursor.Object,
            null,
            0);

        listBox.SelectedObject.ShouldNotBeNull();
    }

    private static Mock<ICursor> SetupForPush()
    {
        Mock<ICursor> mockCursor = new();
        mockCursor.Setup(c => c.XRespectingGumZoomAndBounds())
            .Returns(20);
        mockCursor.Setup(c => c.YRespectingGumZoomAndBounds())
            .Returns(20);
        mockCursor.Setup(c => c.X)
            .Returns(20);
        mockCursor.Setup(c => c.Y)
            .Returns(20);

        mockCursor.Setup(c => c.PrimaryPush).Returns(true);
        return mockCursor;
    }
}
