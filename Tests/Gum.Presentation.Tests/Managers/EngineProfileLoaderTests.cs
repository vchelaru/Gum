using Gum.Managers;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// Reading the .gumengine a project references: parsing, path resolution, and falling back to an
/// unrestricted project with a warning when the file is unusable (#7720).
/// </summary>
public class EngineProfileLoaderTests : BaseTestClass
{
    private readonly EngineProfileLoader _sut = new EngineProfileLoader();
    private readonly string _root;
    private readonly string _projectFolder;
    private readonly string _projectFile;

    public EngineProfileLoaderTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "gum-engine-" + Guid.NewGuid().ToString("N"));
        _projectFolder = Path.Combine(_root, "game", "Content");
        Directory.CreateDirectory(_projectFolder);
        _projectFile = Path.Combine(_projectFolder, "Test.gumx");
    }

    public override void Dispose()
    {
        Directory.Delete(_root, recursive: true);
        base.Dispose();
    }

    private void WriteProfile(string relativeToRoot, string json)
    {
        string path = Path.Combine(_root, relativeToRoot);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, json);
    }

    [Fact]
    public void Load_ShouldReadEverySetting()
    {
        WriteProfile("game/Content/Sprites.gumengine", """
            {
              "supportedStandardTypes": [ "Sprite", "Text" ],
              "hideVariablesByDefault": true,
              "variableRules": [
                { "pattern": "Sprite.*", "action": "Hide" },
                { "pattern": "Sprite.X", "action": "show" }
              ],
              "disableComponents": true,
              "disableBehaviors": true
            }
            """);

        EngineProfileLoadResult result = _sut.Load(_projectFile, "Sprites.gumengine");

        result.Warning.ShouldBeNull();
        result.Profile.SupportedStandardTypes.ShouldBe(new[] { "Sprite", "Text" });
        result.Profile.HideVariablesByDefault.ShouldBeTrue();
        result.Profile.DisableComponents.ShouldBeTrue();
        result.Profile.DisableBehaviors.ShouldBeTrue();
        result.Profile.VariableRules.Count.ShouldBe(2);
        result.Profile.VariableRules[0].Pattern.ShouldBe("Sprite.*");
        result.Profile.VariableRules[0].Action.ShouldBe(VariableRuleAction.Hide);
        result.Profile.VariableRules[1].Pattern.ShouldBe("Sprite.X");
        result.Profile.VariableRules[1].Action.ShouldBe(VariableRuleAction.Show);
    }

    [Fact]
    public void Load_ShouldDefaultMissingSettingsAndIgnoreUnknownOnes()
    {
        WriteProfile("game/Content/Sparse.gumengine", """
            { "notes": "hand written", "disableBehaviors": true, "variableRules": null, "supportedStandardTypes": null }
            """);

        EngineProfileLoadResult result = _sut.Load(_projectFile, "Sparse.gumengine");

        result.Warning.ShouldBeNull();
        result.Profile.DisableBehaviors.ShouldBeTrue();
        result.Profile.DisableComponents.ShouldBeFalse();
        result.Profile.HideVariablesByDefault.ShouldBeFalse();
        result.Profile.SupportedStandardTypes.ShouldBeEmpty();
        result.Profile.VariableRules.ShouldBeEmpty();
    }

    [Fact]
    public void Load_ShouldResolveAPathOutsideTheProjectFolder()
    {
        WriteProfile("engine/MonoGameSprites.gumengine", """{ "disableComponents": true }""");

        EngineProfileLoadResult result = _sut.Load(_projectFile, "../../engine/MonoGameSprites.gumengine");

        result.Warning.ShouldBeNull();
        result.Profile.DisableComponents.ShouldBeTrue();
        result.FilePath.ShouldNotBeNull();
        Path.GetFullPath(result.FilePath!.FullPath).ShouldBe(
            Path.GetFullPath(Path.Combine(_root, "engine", "MonoGameSprites.gumengine")), StringCompareShould.IgnoreCase);
    }

    [Fact]
    public void Load_ShouldAcceptBackslashesInTheReference()
    {
        WriteProfile("game/Content/Sub/Sprites.gumengine", """{ "disableComponents": true }""");

        EngineProfileLoadResult result = _sut.Load(_projectFile, "Sub\\Sprites.gumengine");

        result.Warning.ShouldBeNull();
        result.Profile.DisableComponents.ShouldBeTrue();
    }

    [Fact]
    public void Load_ShouldBeUnrestrictedWithoutWarning_WhenReferenceIsEmpty()
    {
        EngineProfileLoadResult result = _sut.Load(_projectFile, "");

        result.Warning.ShouldBeNull();
        result.FilePath.ShouldBeNull();
        result.Profile.DisableComponents.ShouldBeFalse();
    }

    [Fact]
    public void Load_ShouldBeUnrestrictedWithoutWarning_WhenProjectHasNoFileYet()
    {
        EngineProfileLoadResult result = _sut.Load(null, "Sprites.gumengine");

        result.Warning.ShouldBeNull();
        result.FilePath.ShouldBeNull();
    }

    [Fact]
    public void Load_ShouldWarnNamingThePath_WhenFileIsMissing()
    {
        EngineProfileLoadResult result = _sut.Load(_projectFile, "Missing.gumengine");

        result.Profile.DisableComponents.ShouldBeFalse();
        result.Warning.ShouldNotBeNull();
        result.Warning!.ShouldContain("Missing.gumengine");
        result.FilePath.ShouldNotBeNull();
    }

    [Fact]
    public void Load_ShouldWarnAndBeUnrestricted_WhenJsonIsInvalid()
    {
        WriteProfile("game/Content/Broken.gumengine", "{ \"disableComponents\": tru");

        EngineProfileLoadResult result = _sut.Load(_projectFile, "Broken.gumengine");

        result.Profile.DisableComponents.ShouldBeFalse();
        result.Warning.ShouldNotBeNull();
        result.Warning!.ShouldContain("Broken.gumengine");
    }

    [Fact]
    public void Load_ShouldWarnAndBeUnrestricted_WhenAnEnumValueIsUnknown()
    {
        WriteProfile("game/Content/BadRule.gumengine", """
            { "disableComponents": true, "variableRules": [ { "pattern": "X", "action": "Maybe" } ] }
            """);

        EngineProfileLoadResult result = _sut.Load(_projectFile, "BadRule.gumengine");

        result.Profile.DisableComponents.ShouldBeFalse();
        result.Warning.ShouldNotBeNull();
    }
}
