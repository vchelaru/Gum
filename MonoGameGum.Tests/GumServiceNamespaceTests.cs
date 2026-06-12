using Shouldly;
using System;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests;

/// <summary>
/// Covers the GumService namespace migration (issue #3119): the real type is
/// Gum.GumService; MonoGameGum.GumService is a permanent [Obsolete] subclass shim,
/// and both Default properties resolve to the same singleton whose declared and
/// runtime type is MonoGameGum.GumService (soft migration).
/// </summary>
public class GumServiceNamespaceTests : BaseTestClass
{
#pragma warning disable CS0618 // Type or member is obsolete — these tests intentionally exercise the legacy shim surface
    [Fact]
    public void Default_ShouldBeDeclaredAsMonoGameGumGumService_ForSoftMigration()
    {
        PropertyInfo? defaultProperty = typeof(Gum.GumService).GetProperty(
            "Default",
            BindingFlags.Public | BindingFlags.Static);

        defaultProperty.ShouldNotBeNull();
        defaultProperty.PropertyType.ShouldBe(typeof(MonoGameGum.GumService));
    }

    [Fact]
    public void Default_ShouldBeSameSingleton_ForGumAndMonoGameGumNamespaces()
    {
        Gum.GumService fromGumNamespace = Gum.GumService.Default;
        MonoGameGum.GumService fromLegacyNamespace = MonoGameGum.GumService.Default;

        fromLegacyNamespace.ShouldBeSameAs(fromGumNamespace);
    }

    [Fact]
    public void Default_ShouldReturnMonoGameGumGumServiceInstance()
    {
        Gum.GumService instance = Gum.GumService.Default;

        instance.ShouldBeOfType<MonoGameGum.GumService>();
    }

    [Fact]
    public void MonoGameGumGumService_ShouldBeObsolete()
    {
        Attribute.IsDefined(typeof(MonoGameGum.GumService), typeof(ObsoleteAttribute))
            .ShouldBeTrue();
    }

    [Fact]
    public void MonoGameGumGumService_ShouldDeriveFromGumGumService()
    {
        typeof(MonoGameGum.GumService).BaseType.ShouldBe(typeof(Gum.GumService));
    }
#pragma warning restore CS0618
}
