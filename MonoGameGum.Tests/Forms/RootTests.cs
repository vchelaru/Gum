using Gum.Forms.Controls;
using Gum.GueDeriving;
using Gum.Wireframe;
using Shouldly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace MonoGameGum.Tests.Forms;
public class RootTests : BaseTestClass
{
    [Fact]
    public void Root_CanBeReassigned_AndIsUsedByAddToRoot()
    {
        InteractiveGue originalRoot = Gum.GumService.Default.Root;
        ContainerRuntime customRoot = new();
        customRoot.AddToManagers(RenderingLibrary.SystemManagers.Default);

        try
        {
            Gum.GumService.Default.Root = customRoot;
            Gum.GumService.Default.Root.ShouldBe(customRoot);

            TextBox textBox = new();
            textBox.AddToRoot();

            customRoot.Children.ShouldContain(textBox.Visual);
            originalRoot.Children.ShouldNotContain(textBox.Visual);
        }
        finally
        {
            Gum.GumService.Default.Root = originalRoot;
            customRoot.Children.Clear();
            customRoot.RemoveFromManagers();
        }
    }

    [Fact]
    public void Root_Reassigned_MovesFocusCleanupSubscriptionToNewRoot()
    {
        InteractiveGue originalRoot = Gum.GumService.Default.Root;
        ContainerRuntime customRoot = new();
        customRoot.AddToManagers(RenderingLibrary.SystemManagers.Default);

        try
        {
            Gum.GumService.Default.Root = customRoot;

            TextBox textBox = new();
            textBox.AddToRoot();
            textBox.IsFocused = true;

            ((object?)InteractiveGue.CurrentInputReceiver).ShouldBe(textBox);

            customRoot.Children.Clear();

            InteractiveGue.CurrentInputReceiver.ShouldBeNull(
                "because the focus-cleanup subscription must move to the reassigned root, or focus on a " +
                "control removed from it would never clear");
        }
        finally
        {
            Gum.GumService.Default.Root = originalRoot;
            customRoot.Children.Clear();
            customRoot.RemoveFromManagers();
        }
    }

    [Fact]
    public void RemovingChildren_ShouldNotThrowException()
    {
        var root = GumService.Default.Root;

        root.Children.Clear();

        TextBox textBox = new();
        (textBox).AddToRoot();
        textBox.IsFocused = true;
        root.Children.Clear();

        (textBox).AddToRoot();
        TextBox textBox2 = new();
        root.Children[0] = textBox2.Visual;
        textBox2.IsFocused = true;
        root.Children.Clear();

        textBox = new();
        root.Children.Add(textBox.Visual);
        textBox.IsFocused = true;
        root.Children.Remove(textBox.Visual);
    }

}
