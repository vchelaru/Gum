using System.Xml.Linq;
using Shouldly;

namespace Gum.AssemblyNameGuard.Tests;

/// <summary>
/// Each Gum runtime packs its own buildTransitive\<c>PackageId</c>.targets wrapper (the file
/// name NuGet requires for auto-import) that sets this package's identity and imports the
/// shared <see cref="AssemblyNameCollisionGuardTests"/>-covered guard logic. This asserts each
/// wrapper is wired to the right assembly name / package id, without paying for a real
/// `dotnet build` per runtime. See #4311.
/// </summary>
public class RuntimeTargetsWrapperTests
{
    public static IEnumerable<object[]> Wrappers()
    {
        yield return new object[] { "MonoGameGum/build/Gum.MonoGame.targets", "MonoGameGum", "Gum.MonoGame" };
        yield return new object[] { "Runtimes/SkiaGum/build/Gum.SkiaSharp.targets", "SkiaGum", "Gum.SkiaSharp" };
        yield return new object[] { "Runtimes/RaylibGum/build/Gum.raylib.targets", "RaylibGum", "Gum.raylib" };
        yield return new object[] { "Runtimes/SilkNetGum/build/Gum.SilkNet.targets", "SilkNetGum", "Gum.SilkNet" };
        yield return new object[] { "Runtimes/SokolGum/build/Gum.sokol.targets", "SokolGum", "Gum.sokol" };
    }

    [Theory]
    [MemberData(nameof(Wrappers))]
    public void Wrapper_SetsExpectedRuntimeIdentityAndImportsSharedGuard(
        string relativePath, string expectedAssemblyName, string expectedPackageId)
    {
        string fullPath = Path.Combine(RepoPaths.RepoRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        File.Exists(fullPath).ShouldBeTrue($"{relativePath} should exist");

        XDocument document = XDocument.Load(fullPath);
        XNamespace ns = document.Root!.Name.Namespace;

        string? assemblyName = document.Descendants(ns + "GumRuntimeAssemblyName").FirstOrDefault()?.Value;
        string? packageId = document.Descendants(ns + "GumRuntimePackageId").FirstOrDefault()?.Value;
        bool importsSharedGuard = document.Descendants(ns + "Import")
            .Any(import => (string?)import.Attribute("Project") is string project
                && project.EndsWith("AssemblyNameCollisionGuard.targets", StringComparison.Ordinal));

        assemblyName.ShouldBe(expectedAssemblyName);
        packageId.ShouldBe(expectedPackageId);
        importsSharedGuard.ShouldBeTrue($"{relativePath} should import the shared AssemblyNameCollisionGuard.targets");
    }
}
