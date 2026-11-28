using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Gum.Forms.Controls;
using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.Forms;

/// <summary>
/// A BindingContext bound with SetBinding in fuller trees: deep descendants, Visual swaps, item controls, explicit child
/// contexts, FallbackValue and rebinding, plus the unbound assignment pattern generated code uses.
/// </summary>
public class BoundBindingContextScenarioTests : BaseTestClass
{
    public class PlayerVm : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public int NameReads;
        public ObservableCollection<string> Items { get; } = new();
    }

    public class GameVm : ViewModel
    {
        public string? Name { get => Get<string?>(); set => Set(value); }
        public PlayerVm? Player { get => Get<PlayerVm?>(); set => Set(value); }
    }

    public class TeamVm : ViewModel
    {
        public GameVm? Game { get => Get<GameVm?>(); set => Set(value); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static WeakReference BuildDeep(GameVm vm, out TextRuntime leaf, out StackPanel middle)
    {
        var root = new StackPanel { BindingContext = vm };
        var top = new StackPanel();
        root.AddChild(top);
        middle = new StackPanel();
        top.AddChild(middle);
        middle.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        var inner = new ContainerRuntime();
        middle.Visual.Children.Add(inner);
        var inner2 = new StackPanel();
        inner.Children.Add(inner2.Visual);
        leaf = new TextRuntime();
        leaf.SetBinding(nameof(TextRuntime.Text), nameof(PlayerVm.Name));
        inner2.Visual.Children.Add(leaf);
        var old = new PlayerVm { Name = "old" };
        vm.Player = old;
        leaf.Text.ShouldBe("old");
        vm.Player = null;
        vm.Player = new PlayerVm { Name = "new" };
        return new WeakReference(old);
    }

    [Fact]
    public void DeepTree_PropertyNullThenNew_LeafShowsNewValueAndOldViewModelIsCollected()
    {
        var vm = new GameVm { Name = "parent" };
        var weak = BuildDeep(vm, out var leaf, out var middle);
        leaf.Text.ShouldBe("new");
        leaf.BindingContext.ShouldBe(vm.Player);
        vm.Player!.Name = "new2";
        leaf.Text.ShouldBe("new2");
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
        weak.IsAlive.ShouldBeFalse();
        vm.Player = null;
        leaf.BindingContext.ShouldBeNull();
        middle.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void VisualReplacedWhileNull_PropertySetAgain_NewVisualGetsValue()
    {
        var vm = new GameVm { Player = new PlayerVm { Name = "a" } };
        var panel = new StackPanel { BindingContext = vm };
        var sut = new Label();
        panel.AddChild(sut);
        sut.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        vm.Player = null;
        var newVisual = new LabelVisual(tryCreateFormsObject: false);
        panel.Visual.Children.Add(newVisual);
        sut.Visual = newVisual;
        sut.BindingContext.ShouldBeNull();
        var p = new PlayerVm { Name = "b" };
        vm.Player = p;
        sut.BindingContext.ShouldBe(p);
        newVisual.BindingContext.ShouldBe(p);
        sut.ClearBinding(nameof(FrameworkElement.BindingContext));
        sut.BindingContext.ShouldBe(vm);
    }

    [Fact]
    public void ListBoxUnderBoundContext_PropertyNullThenNew_RebuildsItemContexts()
    {
        var vm = new GameVm { Player = new PlayerVm() };
        vm.Player.Items.Add("x"); vm.Player.Items.Add("y");
        var panel = new StackPanel { BindingContext = vm };
        var playerPanel = new StackPanel();
        panel.AddChild(playerPanel);
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        var listBox = new ListBox();
        listBox.SetBinding(nameof(ListBox.Items), nameof(PlayerVm.Items));
        playerPanel.AddChild(listBox);
        listBox.ListBoxItems.Select(i => i.BindingContext).ShouldBe(new object[] { "x", "y" });

        vm.Player = null;
        listBox.ListBoxItems.Count.ShouldBe(0);

        var p2 = new PlayerVm(); p2.Items.Add("z");
        vm.Player = p2;
        listBox.ListBoxItems.Select(i => i.BindingContext).ShouldBe(new object[] { "z" });

        // Unbound ListBox in an unbound parent: changing parent context keeps item contexts
        var lb2 = new ListBox();
        panel.AddChild(lb2);
        lb2.Items.Add("q");
        panel.BindingContext = new GameVm();
        lb2.ListBoxItems[0].BindingContext.ShouldBe("q");
    }

    [Fact]
    public void AssignedContextSetToNull_InheritsParentContext()
    {
        var screen = new ContainerRuntime();
        var child = new StackPanel();
        screen.Children.Add(child.Visual);
        var text = new TextRuntime();
        text.SetBinding(nameof(TextRuntime.Text), nameof(GameVm.Name));
        child.Visual.Children.Add(text);
        child.BindingContext = screen; // like codegen "instance.BindingContext = this"
        child.BindingContext.ShouldBe(screen);
        var vm = new GameVm { Name = "n" };
        screen.BindingContext = vm;
        child.BindingContext = null;
        child.BindingContext.ShouldBe(vm);
        text.Text.ShouldBe("n");
    }

    [Fact]
    public void ChildWithOwnContext_ParentBoundToNull_KeepsOwnContext()
    {
        var vm = new GameVm { Name = "parent", Player = new PlayerVm { Name = "a" } };
        var panel = new StackPanel { BindingContext = vm };
        var playerPanel = new StackPanel();
        panel.AddChild(playerPanel);
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        var own = new PlayerVm { Name = "own" };
        var child = new Label { BindingContext = own };
        child.SetBinding(nameof(Label.Text), nameof(PlayerVm.Name));
        playerPanel.AddChild(child);
        var inheritor = new Label();
        inheritor.SetBinding(nameof(Label.Text), nameof(PlayerVm.Name));
        playerPanel.AddChild(inheritor);
        inheritor.Text.ShouldBe("a");

        vm.Player = null;

        child.BindingContext.ShouldBe(own);
        child.Text.ShouldBe("own");
        inheritor.BindingContext.ShouldBeNull();
        own.Name = "own2";
        child.Text.ShouldBe("own2");
    }

    [Fact]
    public void ParentBoundToNull_ChildShowsFallbackValue()
    {
        var vm = new GameVm { Name = "parent", Player = new PlayerVm { Name = "a" } };
        var panel = new StackPanel { BindingContext = vm };
        var playerPanel = new StackPanel();
        panel.AddChild(playerPanel);
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        var label = new Label();
        label.SetBinding(nameof(Label.Text), new Gum.Forms.Data.Binding(nameof(PlayerVm.Name)) { FallbackValue = "fb" });
        playerPanel.AddChild(label);
        label.Text.ShouldBe("a");
        vm.Player = null;
        label.Text.ShouldBe("fb");
    }

    [Fact]
    public void BindingContextBoundTwice_FollowsSecondBinding()
    {
        var vm = new GameVm { Name = "parent", Player = null };
        var panel = new StackPanel { BindingContext = vm };
        var playerPanel = new StackPanel();
        panel.AddChild(playerPanel);
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        playerPanel.BindingContext.ShouldBeNull();
        var p = new PlayerVm();
        vm.Player = p;
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        playerPanel.BindingContext.ShouldBe(p);
        vm.Player = null;
        playerPanel.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void BoundElementRemovedFromParent_ContextIsNullNotStale()
    {
        PlayerVm player = new() { Name = "a" };
        ContainerRuntime parent = new() { BindingContext = new GameVm { Player = player } };
        ContainerRuntime playerPanel = new();
        playerPanel.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(GameVm.Player));
        parent.Children.Add(playerPanel);
        playerPanel.BindingContext.ShouldBe(player);

        parent.Children.Remove(playerPanel);

        playerPanel.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void BoundFormsElementRemovedFromParent_ContextIsNullAndChildResets()
    {
        PlayerVm player = new() { Name = "a" };
        StackPanel parent = new() { BindingContext = new GameVm { Player = player } };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        parent.AddChild(playerPanel);
        Label label = new();
        string? preBindingText = label.Text;
        label.SetBinding(nameof(Label.Text), nameof(PlayerVm.Name));
        playerPanel.AddChild(label);
        label.Text.ShouldBe("a");

        parent.RemoveChild(playerPanel);

        playerPanel.BindingContext.ShouldBeNull();
        label.BindingContext.ShouldBeNull();
        label.Text.ShouldBe(preBindingText);
    }

    [Fact]
    public void ParentContextSetToNull_BoundVisualContextIsNull()
    {
        PlayerVm player = new() { Name = "a" };
        ContainerRuntime parent = new() { BindingContext = new GameVm { Player = player } };
        ContainerRuntime playerPanel = new();
        playerPanel.SetBinding(nameof(GraphicalUiElement.BindingContext), nameof(GameVm.Player));
        parent.Children.Add(playerPanel);
        playerPanel.BindingContext.ShouldBe(player);

        parent.BindingContext = null;

        playerPanel.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void ParentContextSetToNull_BoundFormsContextIsNull()
    {
        PlayerVm player = new() { Name = "a" };
        StackPanel parent = new() { BindingContext = new GameVm { Player = player } };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        parent.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBe(player);

        parent.BindingContext = null;

        playerPanel.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void ParentContextSetToNull_BoundContextWithFallback_ShowsFallback()
    {
        PlayerVm fallback = new() { Name = "fallback" };
        StackPanel parent = new() { BindingContext = new GameVm { Player = new PlayerVm { Name = "a" } } };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext),
            new Gum.Forms.Data.Binding(nameof(GameVm.Player)) { FallbackValue = fallback });
        parent.AddChild(playerPanel);

        parent.BindingContext = null;

        playerPanel.BindingContext.ShouldBe(fallback);
    }

    [Fact]
    public void NestedBoundContexts_OuterSourceBecomesNull_InnerContextIsNull()
    {
        PlayerVm player = new() { Name = "a" };
        TeamVm team = new() { Game = new GameVm { Player = player } };
        StackPanel screen = new() { BindingContext = team };
        StackPanel gamePanel = new();
        gamePanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(TeamVm.Game));
        screen.AddChild(gamePanel);
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        gamePanel.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBe(player);

        team.Game = null;

        gamePanel.BindingContext.ShouldBeNull();
        playerPanel.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void OwnContextThenBound_RemovedFromParent_IsNullAndClearBindingRestoresOwn()
    {
        PlayerVm own = new() { Name = "own" };
        PlayerVm fromParent = new() { Name = "parent" };
        StackPanel parent = new() { BindingContext = new GameVm { Player = fromParent } };
        StackPanel playerPanel = new() { BindingContext = own };
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        parent.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBe(fromParent);

        parent.RemoveChild(playerPanel);

        playerPanel.BindingContext.ShouldBeNull();
        playerPanel.ClearBinding(nameof(FrameworkElement.BindingContext));
        playerPanel.BindingContext.ShouldBe(own);
    }

    [Fact]
    public void ScreenLeavesRoot_BoundDescendantContextIsNull()
    {
        PlayerVm player = new() { Name = "a" };
        StackPanel screen = new() { BindingContext = new GameVm { Player = player } };
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        screen.AddChild(playerPanel);
        screen.AddToRoot();
        playerPanel.BindingContext.ShouldBe(player);

        screen.RemoveFromRoot();

        playerPanel.BindingContext.ShouldBeNull();
    }

    [Fact]
    public void BoundElementReparented_ResolvesAgainstEachNewParent()
    {
        PlayerVm first = new() { Name = "first" };
        PlayerVm second = new() { Name = "second" };
        StackPanel firstParent = new() { BindingContext = new GameVm { Player = first } };
        StackPanel secondParent = new() { BindingContext = new GameVm { Player = second } };
        StackPanel parentWithoutContext = new();
        StackPanel playerPanel = new();
        playerPanel.SetBinding(nameof(FrameworkElement.BindingContext), nameof(GameVm.Player));
        firstParent.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBe(first);

        secondParent.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBe(second);

        parentWithoutContext.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBeNull();

        firstParent.AddChild(playerPanel);
        playerPanel.BindingContext.ShouldBe(first);
    }
}
