using Gum.GueDeriving;
using Gum.Mvvm;
using Gum.Wireframe;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests.VisualClone;

/// <summary>
/// A cloned visual (a row template cloned per item) carries its bindings but not its BindingContext: each clone
/// follows the context it is given, and the original keeps following its own.
/// </summary>
public class VisualCloneBindingTests : BaseTestClass
{
    private class ItemViewModel : ViewModel
    {
        public string? Name
        {
            get => Get<string?>();
            set => Set(value);
        }
    }

    [Fact]
    public void Clone_GivenContext_BindsToIt()
    {
        TextRuntime template = new();
        template.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));

        TextRuntime clone = (TextRuntime)template.Clone();
        clone.BindingContext = new ItemViewModel { Name = "sword" };

        clone.Text.ShouldBe("sword");
    }

    [Fact]
    public void Clone_ContextChanges_OriginalUnaffected()
    {
        ItemViewModel originalVm = new() { Name = "original" };
        TextRuntime template = new() { BindingContext = originalVm };
        template.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));

        TextRuntime clone = (TextRuntime)template.Clone();
        ItemViewModel cloneVm = new() { Name = "clone" };
        clone.BindingContext = cloneVm;
        cloneVm.Name = "clone2";

        template.Text.ShouldBe("original");
        clone.Text.ShouldBe("clone2");
    }

    [Fact]
    public void Clone_OriginalViewModelChanges_CloneUnaffected()
    {
        ItemViewModel originalVm = new() { Name = "original" };
        TextRuntime template = new() { BindingContext = originalVm };
        template.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));

        TextRuntime clone = (TextRuntime)template.Clone();
        clone.BindingContext = new ItemViewModel { Name = "clone" };
        originalVm.Name = "changed";

        clone.Text.ShouldBe("clone");
        template.Text.ShouldBe("changed");
    }

    [Fact]
    public void Clone_AddedToParentWithContext_InheritsIt()
    {
        TextRuntime template = new();
        template.SetBinding(nameof(TextRuntime.Text), nameof(ItemViewModel.Name));
        ContainerRuntime row = new() { BindingContext = new ItemViewModel { Name = "shield" } };

        TextRuntime clone = (TextRuntime)template.Clone();
        row.Children.Add(clone);

        clone.Text.ShouldBe("shield");
    }
}
