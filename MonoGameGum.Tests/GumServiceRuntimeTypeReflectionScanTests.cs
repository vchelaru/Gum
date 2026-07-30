using Gum;
using Shouldly;
using System.Reflection;
using Xunit;

namespace MonoGameGum.Tests;

/// <summary>
/// Pins GumService's reflection-based runtime-type registration fallback (issue #4105): under
/// Native AOT, walking every loaded assembly's types via reflection can throw mid-scan when it
/// touches trimmed framework metadata. These tests exercise the same failure shape - an unguarded
/// <see cref="System.Type.GetMethod(string, BindingFlags)"/> call throwing during the scan -
/// using an ordinary JIT-reproducible trigger (an ambiguous overload set) so the regression
/// doesn't require a Native AOT publish to catch.
/// </summary>
public class GumServiceRuntimeTypeReflectionScanTests : BaseTestClass
{
    [Fact]
    public void RegisterRuntimeTypesThroughReflection_DoesNotThrow_WhenALoadedAssemblyHasAnAmbiguousRegisterRuntimeTypesOverloadSet()
    {
        // AmbiguousRuntimeTypesFixture (below) is compiled into this test assembly, which is
        // already loaded into AppDomain.CurrentDomain.GetAssemblies() by the time this runs - the
        // scan's extension-package pass reaches it like any other loaded assembly.
        GumService gumService = new GumService();

        Should.NotThrow(() => gumService.RegisterRuntimeTypesThroughReflection());
    }

    [Theory]
    [InlineData("System.Private.CoreLib", true)]
    [InlineData("System.Runtime", true)]
    [InlineData("mscorlib", true)]
    [InlineData("netstandard", true)]
    [InlineData("Microsoft.Extensions.DependencyInjection", true)]
    [InlineData("MonoGameGum.Tests", false)]
    [InlineData("Gum.Shapes.KNI", false)]
    [InlineData("MonoGame.Framework", false)]
    public void IsFrameworkAssembly_ClassifiesAssemblyByNamePrefix(string assemblyName, bool expected)
    {
        GumService gumService = new GumService();

        bool actual = gumService.IsFrameworkAssembly(new AssemblyName(assemblyName));

        actual.ShouldBe(expected);
    }
}

/// <summary>
/// Reflection fodder for <see cref="GumServiceRuntimeTypeReflectionScanTests"/>: two public
/// static overloads named "RegisterRuntimeTypes" make
/// <c>Type.GetMethod("RegisterRuntimeTypes", BindingFlags.Static | BindingFlags.Public)</c> throw
/// <see cref="AmbiguousMatchException"/>, mirroring the class of uncaught-reflection-exception bug
/// reported in issue #4105.
/// </summary>
public static class AmbiguousRuntimeTypesFixture
{
    public static void RegisterRuntimeTypes() { }

    public static void RegisterRuntimeTypes(int unused) { }
}
