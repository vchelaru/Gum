using Gum.GueDeriving;
using Shouldly;

namespace RaylibGum.Tests;

public class BaseTestClassTests
{
    [Fact]
    public void Dispose_ClearsPopupRootAndModalRootChildren()
    {
        // A popup left open (e.g. a ComboBox dropdown) keeps drawing in later tests with a texture
        // Dispose has already freed. A later test's fresh texture can then reuse that GPU id and
        // batch into the leaked popup's draw call, hiding its own (#4901).
        BaseTestClass test = new BaseTestClass();
        Gum.GumService.Default.PopupRoot.Children.Add(new ContainerRuntime());
        Gum.GumService.Default.ModalRoot.Children.Add(new ContainerRuntime());

        test.Dispose();

        Gum.GumService.Default.PopupRoot.Children.ShouldBeEmpty();
        Gum.GumService.Default.ModalRoot.Children.ShouldBeEmpty();
    }
}
