#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>What happens to the target folder: an existing empty folder, a failed creation, a Gum-only project.</summary>
public class NewProjectTargetFolderTests : NewProjectServiceTestBase
{
    [Fact]
    public async Task Execute_ExistingEmptyTargetFolder_IsUsedAndKept_WhenCreationFails()
    {
        Directory.CreateDirectory(Game("Empty"));
        // A directory named like the csproj makes the write fail after the folder already existed.
        Directory.CreateDirectory(Path.Combine(Game("Empty"), "Empty.csproj"));

        NewProjectResult result = await Run(Request("monogame", "Empty"));

        result.Files.Success.ShouldBeFalse();
        Directory.Exists(Game("Empty")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_GumOnlyIntoAnExistingEmptyFolder_CreatesTheProjectInIt()
    {
        Directory.CreateDirectory(Game("Existing"));

        NewProjectResult result = await Run(Request(null, "Existing"));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        File.Exists(Path.Combine(Game("Existing"), "Existing.gumj")).ShouldBeTrue();
    }

    [Fact]
    public async Task Execute_GumOnly_WritesNoCodeProjectFiles()
    {
        NewProjectResult result = await Run(Request(null, "OnlyGum", generate: false));

        result.Files.Success.ShouldBeTrue(result.Files.ErrorMessage);
        RelativeFiles(Game("OnlyGum")).ShouldNotContain(file => file.EndsWith(".csproj") || file.EndsWith(".cs") || file.EndsWith(".slnx"));
        result.Files.CsprojPath.ShouldBeNull();
        _calls.ShouldBeEmpty();
    }
}
