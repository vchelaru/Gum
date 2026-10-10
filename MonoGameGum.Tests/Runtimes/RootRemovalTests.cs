using Gum.Forms.Controls;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Runtimes;

public class RootRemovalTests : BaseTestClass
{
    public enum RemovalPath
    {
        GraphicalUiElementRemoveFromRoot,
        RootChildrenClear,
        RootChildrenRemove,
        FrameworkElementRemoveFromRoot,
    }

    [Theory]
    [InlineData(RemovalPath.GraphicalUiElementRemoveFromRoot)]
    [InlineData(RemovalPath.RootChildrenClear)]
    [InlineData(RemovalPath.RootChildrenRemove)]
    [InlineData(RemovalPath.FrameworkElementRemoveFromRoot)]
    public void Remove_ShouldClearInputReceiver_WhenFocusedControlIsInsideRemovedScreen(RemovalPath path)
    {
        Panel screen = new();
        screen.AddToRoot();
        TextBox textBox = new();
        screen.AddChild(textBox);
        textBox.IsFocused = true;
        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        Remove(screen, path);

        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Theory]
    [InlineData(RemovalPath.GraphicalUiElementRemoveFromRoot)]
    [InlineData(RemovalPath.FrameworkElementRemoveFromRoot)]
    public void Remove_ShouldClearInputReceiver_WhenFocusedControlIsRemovedDirectly(RemovalPath path)
    {
        TextBox textBox = new();
        textBox.AddToRoot();
        textBox.IsFocused = true;
        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);

        if (path == RemovalPath.GraphicalUiElementRemoveFromRoot)
        {
            textBox.Visual.RemoveFromRoot();
        }
        else
        {
            textBox.RemoveFromRoot();
        }

        InteractiveGue.CurrentInputReceiver.ShouldBeNull();
    }

    [Fact]
    public void Remove_ShouldNotClearInputReceiver_WhenOtherScreenIsRemoved()
    {
        Panel other = new();
        other.AddToRoot();
        Panel screen = new();
        screen.AddToRoot();
        TextBox textBox = new();
        screen.AddChild(textBox);
        textBox.IsFocused = true;

        other.Visual.RemoveFromRoot();

        InteractiveGue.CurrentInputReceiver.ShouldBe(textBox);
    }

    private static void Remove(Panel screen, RemovalPath path)
    {
        switch (path)
        {
            case RemovalPath.GraphicalUiElementRemoveFromRoot:
                screen.Visual.RemoveFromRoot();
                break;
            case RemovalPath.RootChildrenClear:
                Gum.GumService.Default.Root.Children.Clear();
                break;
            case RemovalPath.RootChildrenRemove:
                Gum.GumService.Default.Root.Children.Remove(screen.Visual);
                break;
            case RemovalPath.FrameworkElementRemoveFromRoot:
                screen.RemoveFromRoot();
                break;
        }
    }
}
