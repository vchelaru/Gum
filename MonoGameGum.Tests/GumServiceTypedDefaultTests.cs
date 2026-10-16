#pragma warning disable CS0618 // the typed Default is the legacy-named back-compat subclass
using RenderingLibrary;
using Shouldly;
using Xunit;

namespace MonoGameGum.Tests;

// GumService.Default is a typed view of IGumService.Default, the one current-service pointer.
public class GumServiceTypedDefaultTests : BaseTestClass
{
    private readonly IGumService? _original = IGumService.Default;

    public override void Dispose()
    {
        IGumService.Default = _original;
        base.Dispose();
    }

    [Fact]
    public void Default_ShouldReturnTheService_AssignedToTheInterfaceDefault()
    {
        MonoGameGum.GumService other = new MonoGameGum.GumService();

        IGumService.Default = other;

        Gum.GumService.Default.ShouldBeSameAs(other);
    }

    [Fact]
    public void Default_SetterShouldAssignTheInterfaceDefault()
    {
        MonoGameGum.GumService other = new MonoGameGum.GumService();

        Gum.GumService.Default = other;

        IGumService.Default.ShouldBeSameAs(other);
        Gum.GumService.Default.ShouldBeSameAs(other);
    }

    [Fact]
    public void Default_ShouldKeepTheTypedInstance_WhenAPlainServiceBecomesTheInterfaceDefault()
    {
        MonoGameGum.GumService typed = new MonoGameGum.GumService();
        Gum.GumService.Default = typed;
        Gum.GumService plain = new Gum.GumService();

        IGumService.Default = plain;

        Gum.GumService.Default.ShouldBeSameAs(typed, "a plain GumService is current, but is not the back-compat type Default returns");
    }

    [Fact]
    public void Default_ShouldNotThrow_WhenInterfaceDefaultIsAPlainService()
    {
        IGumService.Default = new Gum.GumService();

        Should.NotThrow(() => Gum.GumService.Default);
    }
}
