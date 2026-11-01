#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using System.Xml.Linq;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>The runtime project a source-linked new project references, and the Gum project file its host loads.</summary>
public class NewProjectSourceLinkedReferenceTests : NewProjectServiceTestBase
{
    [Theory]
    [MemberData(nameof(PlatformNames), MemberType = typeof(NewProjectServiceTestBase))]
    public async Task Execute_SourceLinkedFromAFolderWithSpaces_ReferencesARuntimeProjectThatResolvesFromTheCsproj(string platform)
    {
        NewProjectResult result = await Run(Request(platform, "My Spaced Game".Replace(" ", "_"), sourceLinked: true));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        XDocument document = XDocument.Load(result.Files.CsprojPath!);
        string include = document.Descendants("ProjectReference").Single().Attribute("Include")!.Value;
        string resolved = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(result.Files.CsprojPath!)!, include.Replace('\\', Path.DirectorySeparatorChar)));
        File.Exists(resolved).ShouldBeTrue(resolved);
        resolved.ShouldStartWith(_checkout.TrimEnd(Path.DirectorySeparatorChar));
    }

    [Fact]
    public async Task Execute_SourceLinkedFromADeeplyNestedFolder_StillResolvesTheRuntimeProject()
    {
        string nested = Path.Combine(_folder, "a b", "c", "d e", "f", "g");
        NewProjectResult result = await CreateService().ExecuteAsync(new NewProjectRequest
        {
            Path = Path.Combine(nested, "Deep"), Platform = "monogame", SourceLinked = true, Restore = false, Template = "empty"
        }, null, CancellationToken.None);

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        string include = XDocument.Load(result.Files.CsprojPath!).Descendants("ProjectReference").Single().Attribute("Include")!.Value;
        include.ShouldContain("MonoGameGum.csproj");
        File.Exists(Path.GetFullPath(Path.Combine(Path.GetDirectoryName(result.Files.CsprojPath!)!, include.Replace('\\', Path.DirectorySeparatorChar)))).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_SourceLinked_NamesTheGumProjectFileTheHostLoads_ForBothFormats()
    {
        foreach (bool xml in new[] { false, true })
        {
            string name = xml ? "LinkedX" : "LinkedJ";
            NewProjectResult result = await Run(Request("monogame", name, sourceLinked: true, xml: xml));

            result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
            string host = File.ReadAllText(Path.Combine(Game(name), "Game1.cs"));
            host.ShouldContain(xml ? "GumProject.gumx" : "GumProject.gumj");
            host.ShouldNotContain(xml ? "GumProject.gumj" : "GumProject.gumx");
        }
    }
}
