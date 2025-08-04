using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;
public class InteractiveGueTests : BaseTestClass
{
    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfRootIsReset()
    {
        TextBox textBox = new ();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.Root.Children.Clear();

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromRoot()
    {
        TextBox textBox = new ();
        textBox.AddToRoot();
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.Root.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldNotGetUnset_IfSiblingIsRemovedFromRoot()
    {
        TextBox first = new ();
        first.AddToRoot();

        TextBox second = new();
        second.AddToRoot();
        second.IsFocused = true;

        GumService.Default.Root.Children.Remove(first.Visual);

        second.IsFocused.ShouldBeTrue();

    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfChild_IfRootIsReset()
    {
        Panel panel = new();
        panel.AddToRoot();

        TextBox textBox = new();
        panel.AddChild(textBox);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.Root.Children.Clear();

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }


    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromPopup()
    {
        TextBox textBox = new();
        GumService.Default.PopupRoot.Children.Add(textBox.Visual);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.PopupRoot.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void CurrentInputReceiver_ShouldGetUnset_IfControlIsRemovedFromModal()
    {
        TextBox textBox = new();
        GumService.Default.ModalRoot.Children.Add(textBox.Visual);
        textBox.IsFocused = true;

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        GumService.Default.ModalRoot.Children.Remove(textBox.Visual);

        textBox.IsFocused.ShouldBeFalse();
        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    // todo - repeat the tests above for ModalRoot and PopupRoot
}
