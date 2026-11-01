#pragma warning disable xUnit1031 // The helpers create projects synchronously; nothing here can deadlock (no synchronization context).
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp;
using Shouldly;

namespace Gum.ProjectServices.Tests;

/// <summary>Which project names are accepted, and the namespace they produce.</summary>
public class NewProjectNameValidationTests : NewProjectServiceTestBase
{
    [Theory]
    [InlineData("Gum-Game")]
    [InlineData("a--b")]
    [InlineData("X.1")]
    [InlineData("namespace")]
    [InlineData("default")]
    [InlineData("_")]
    [InlineData("My.Game.Name")]
    public async Task Execute_AcceptedPlatformNames_GenerateAValidNamespace(string name)
    {
        // The namespace comes from the name alone, so every platform's host is checked with a name in turn.
        foreach ((string platformName, HostPlatform platform) in Platforms.Where((_, index) => index == name.Sum(character => character) % Platforms.Count()))
        {
            // The folder name is the project name, so use a sub-folder with exactly the name.
            string path = Path.Combine(_folder, platformName, name);
            NewProjectResult result = await CreateService().ExecuteAsync(new NewProjectRequest
            {
                Path = path, Platform = platformName, Restore = false, Template = "empty", SourceLinked = platformName == "kniskia"
            }, null, CancellationToken.None);

            result.Files.Success.ShouldBeTrue($"{platformName} {name}: {result.Files.ErrorMessage}");
            string host = File.ReadAllText(Path.Combine(path, NewProjectLayout.WritesGame1(platform) ? "Game1.cs" : "Program.cs"));
            MatchCollection namespaces = Regex.Matches(host, @"^namespace\s+(\S+?)\s*;", RegexOptions.Multiline);
            foreach (Match ns in namespaces)
            {
                foreach (string segment in ns.Groups[1].Value.Split('.'))
                {
                    SyntaxFacts.IsValidIdentifier(segment.TrimStart('@')).ShouldBeTrue($"{platformName} {name} -> {ns.Groups[1].Value}");
                }
            }
        }
    }

    [Theory]
    [InlineData("Program")]
    [InlineData("MonoGameGum")]
    [InlineData("GumCommon")]
    public void Validate_PlatformNamesThatClashWithTheRuntime_AreRefusedBeforeAnythingIsWritten(string name)
    {
        NewProjectValidation validation = CreateService().Validate(Request("monogame", name));

        validation.Error.ShouldNotBeNull();
        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
        Directory.Exists(Game(name)).ShouldBeFalse();
    }

    [Theory]
    [InlineData("My Game")]
    [InlineData("1Game")]
    [InlineData("Jeué")]
    [InlineData("Game!")]
    [InlineData("Game$")]
    public void Validate_PlatformNamesThatAreNotProjectNames_AreRefusedWithAMessageNamingTheName(string name)
    {
        NewProjectValidation validation = CreateService().Validate(Request("raylib", name));

        validation.Error.ShouldNotBeNull();
        validation.Error!.Message.ShouldContain(name);
    }

    [Theory]
    [InlineData("CON")]
    [InlineData("prn")]
    [InlineData("Aux")]
    [InlineData("NUL")]
    [InlineData("nul")]
    [InlineData("NUL.txt")]
    [InlineData("COM1")]
    [InlineData("com9")]
    [InlineData("LPT1")]
    [InlineData("lpt9")]
    [InlineData("CON.txt")]
    [InlineData("Nul.Game")]
    [InlineData("Game.")]
    [InlineData("Game ")]
    public async Task Execute_ANameNoOperatingSystemShouldBeGiven_IsRefusedOnEveryOsAndWritesNothing(string name)
    {
        foreach (string? platform in new string?[] { null, "monogame" })
        {
            NewProjectValidation validation = CreateService().Validate(Request(platform, name));
            NewProjectResult result = await Run(Request(platform, name));

            validation.Resolved.ShouldBeNull($"platform={platform}");
            validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName, $"platform={platform}");
            validation.Error.Message.ShouldBe(NewProjectNameRules.GetProblem(name, platform != null), $"platform={platform}");
            result.Files.Success.ShouldBeFalse($"platform={platform}");
            result.Files.ErrorMessage.ShouldBe(validation.Error.Message, $"platform={platform}");
            Directory.Exists(Game(name)).ShouldBeFalse($"platform={platform}");
        }
    }

    [Theory]
    [InlineData("COM10")]
    [InlineData("Console")]
    [InlineData("Game")]
    public void Validate_ANameThatOnlyLooksReserved_IsAccepted(string name)
    {
        CreateService().Validate(Request("monogame", name)).Resolved.ShouldNotBeNull();
        CreateService().Validate(Request(null, name)).Resolved.ShouldNotBeNull();
    }

    [Fact]
    public void Validate_ANameTooLongForTheFiles_IsRefusedWithTheMessageTheDialogShows()
    {
        NewProjectValidation validation = CreateService().Validate(Request(null, new string('a', 251)));

        validation.Resolved.ShouldBeNull();
        validation.Error!.Kind.ShouldBe(NewProjectErrorKind.InvalidProjectName);
    }
}
