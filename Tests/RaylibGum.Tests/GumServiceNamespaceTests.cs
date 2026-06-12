using Shouldly;
using System;
using System.Reflection;
using Xunit;

namespace RaylibGum.Tests;

/// <summary>
/// Covers the GumService namespace migration (issue #3119) on Raylib: the real type is
/// Gum.GumService; RaylibGum.GumService is a permanent [Obsolete] subclass shim,
/// and both Default properties resolve to the same singleton whose declared and
/// runtime type is RaylibGum.GumService (soft migration, mirroring MonoGame).
/// </summary>
public class GumServiceNamespaceTests : BaseTestClass
{
#pragma warning disable CS0618 // Type or member is obsolete — these tests intentionally exercise the legacy shim surface
    [Fact]
    public void Default_ShouldBeDeclaredAsRaylibGumGumService_ForSoftMigration()
    {
        PropertyInfo? defaultProperty = typeof(Gum.GumService).GetProperty(
            "Default",
            BindingFlags.Public | BindingFlags.Static);

        defaultProperty.ShouldNotBeNull();
        defaultProperty.PropertyType.ShouldBe(typeof(RaylibGum.GumService));
    }

    [Fact]
    public void Default_ShouldBeSameSingleton_ForGumAndRaylibGumNamespaces()
    {
        Gum.GumService fromGumNamespace = Gum.GumService.Default;
        RaylibGum.GumService fromLegacyNamespace = RaylibGum.GumService.Default;

        fromLegacyNamespace.ShouldBeSameAs(fromGumNamespace);
    }

    [Fact]
    public void Default_ShouldReturnRaylibGumGumServiceInstance()
    {
        Gum.GumService instance = Gum.GumService.Default;

        instance.ShouldBeOfType<RaylibGum.GumService>();
    }

    [Fact]
    public void RaylibGumGumService_ShouldBeObsolete()
    {
        Attribute.IsDefined(typeof(RaylibGum.GumService), typeof(ObsoleteAttribute))
            .ShouldBeTrue();
    }

    [Fact]
    public void RaylibGumGumService_ShouldDeriveFromGumGumService()
    {
        typeof(RaylibGum.GumService).BaseType.ShouldBe(typeof(Gum.GumService));
    }
#pragma warning restore CS0618
}
