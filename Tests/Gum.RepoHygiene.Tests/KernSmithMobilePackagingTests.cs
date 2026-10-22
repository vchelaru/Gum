using System.Xml.Linq;
using Shouldly;

namespace Gum.RepoHygiene.Tests;

/// <summary>
/// The KernSmith packages ship iOS and Android builds behind the same opt-in as MonoGameGum
/// (<c>-p:IncludeIOS=true -p:IncludeAndroid=true</c>), and those builds must not carry FreeType,
/// whose native library does not link on iOS (#6435). They reference the managed StbTrueType
/// rasterizer instead. The mobile target frameworks cannot be built without the workloads, so this
/// reads the csproj files instead.
/// </summary>
public class KernSmithMobilePackagingTests
{
    private const string GumCommonProject = "Integrations/KernSmith/KernSmith.GumCommon/KernSmith.GumCommon.csproj";
    private const string MonoGameProject = "Integrations/KernSmith/KernSmith.MonoGameGum/KernSmith.MonoGameGum.csproj";

    [Theory]
    [InlineData(GumCommonProject)]
    [InlineData(MonoGameProject)]
    public void MobileTargets_AreBehindTheSameOptInAsMonoGameGum(string project)
    {
        List<XElement> targetFrameworks = Load(project).Descendants("TargetFrameworks").ToList();

        string baseline = targetFrameworks.Single(element => element.Attribute("Condition") == null).Value;
        baseline.ShouldBe("net8.0");

        XElement ios = targetFrameworks.Single(element => element.Value.Contains("net9.0-ios"));
        ((string?)ios.Attribute("Condition")).ShouldNotBeNull().ShouldContain("'$(IncludeIOS)' == 'true'");

        XElement android = targetFrameworks.Single(element => element.Value.Contains("net9.0-android"));
        ((string?)android.Attribute("Condition")).ShouldNotBeNull().ShouldContain("'$(IncludeAndroid)' == 'true'");
    }

    [Fact]
    public void GumCommon_ReferencesFreeTypeOnlyOffMobile_AndStbTrueTypeOnlyOnMobile()
    {
        List<XElement> references = Load(GumCommonProject).Descendants("PackageReference").ToList();

        string freeType = Condition(references, "KernSmith.Rasterizers.FreeType");
        freeType.ShouldContain("!$(TargetFramework.Contains('ios'))");
        freeType.ShouldContain("!$(TargetFramework.Contains('android'))");

        string stbTrueType = Condition(references, "KernSmith.Rasterizers.StbTrueType");
        stbTrueType.ShouldContain("$(TargetFramework.Contains('ios'))");
        stbTrueType.ShouldContain("$(TargetFramework.Contains('android'))");
        stbTrueType.ShouldNotContain("!$(TargetFramework");
    }

    [Fact]
    public void Connector_ReferencesTheAndroidMonoGamePackageOnlyOnAndroid()
    {
        List<XElement> references = Load(MonoGameProject).Descendants("PackageReference").ToList();

        Condition(references, "MonoGame.Framework.Android").ShouldContain("$(TargetFramework.Contains('android'))");
        Condition(references, "MonoGame.Framework.DesktopGL").ShouldContain("!$(TargetFramework.Contains('android'))");
    }

    private static string Condition(List<XElement> references, string id) =>
        (string?)references.Single(reference => (string?)reference.Attribute("Include") == id).Attribute("Condition")
        ?? string.Empty;

    private static XDocument Load(string relativePath) =>
        XDocument.Load(Path.Combine(RepoPaths.RepoRoot, relativePath));
}
