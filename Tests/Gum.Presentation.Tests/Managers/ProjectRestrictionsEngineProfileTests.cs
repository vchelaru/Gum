using Gum.DataTypes;
using Gum.Managers;
using Moq;
using Shouldly;
using System;
using System.IO;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// ProjectRestrictions reads the engine profile the loaded project references, warns once when it cannot,
/// and re-reads it on Reload (#7720).
/// </summary>
public class ProjectRestrictionsEngineProfileTests : BaseTestClass
{
    private readonly string _folder;
    private readonly string _profilePath;
    private readonly GumProjectSave _project;
    private readonly Mock<IOutputManager> _output = new Mock<IOutputManager>();
    private readonly ProjectRestrictions _sut;

    public ProjectRestrictionsEngineProfileTests()
    {
        _folder = Path.Combine(Path.GetTempPath(), "gum-restrictions-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_folder);
        _profilePath = Path.Combine(_folder, "Engine.gumengine");

        _project = new GumProjectSave
        {
            Version = GumProjectSave.NativeVersion,
            FullFileName = Path.Combine(_folder, "Test.gumx"),
        };
        StandardElementsManager.Self.PopulateProjectWithDefaultStandards(_project);
        ObjectFinder.Self.GumProjectSave = _project;

        _sut = new ProjectRestrictions(new EngineProfileLoader(), _output.Object);
    }

    public override void Dispose()
    {
        Directory.Delete(_folder, recursive: true);
        base.Dispose();
    }

    [Fact]
    public void Queries_ShouldFollowTheReferencedProfile()
    {
        File.WriteAllText(_profilePath, """{ "supportedStandardTypes": ["Sprite"], "disableComponents": true }""");
        _project.EngineProfile = "Engine.gumengine";

        _sut.IsStandardTypeBlocked("Text").ShouldBeTrue();
        _sut.IsStandardTypeBlocked("Sprite").ShouldBeFalse();
        _sut.AreComponentsDisabled.ShouldBeTrue();
        _sut.AreBehaviorsDisabled.ShouldBeFalse();
        _output.Verify(x => x.AddOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Queries_ShouldBeUnrestrictedWithoutOutput_WhenProjectHasNoReference()
    {
        _sut.IsStandardTypeBlocked("Text").ShouldBeFalse();
        _sut.AreComponentsDisabled.ShouldBeFalse();
        _sut.HasVariableRules.ShouldBeFalse();
        _sut.SupportedStandardTypes.ShouldBeEmpty();
        _output.Verify(x => x.AddOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void Queries_ShouldBeUnrestrictedAndWarnOnce_WhenReferencedFileIsMissing()
    {
        _project.EngineProfile = "Engine.gumengine";

        _sut.AreComponentsDisabled.ShouldBeFalse();
        _sut.IsStandardTypeBlocked("Text").ShouldBeFalse();
        _sut.AreBehaviorsDisabled.ShouldBeFalse();

        _output.Verify(x => x.AddOutput(It.Is<string>(text => text.Contains("Engine.gumengine"))), Times.Once);
    }

    [Fact]
    public void Queries_ShouldLoadAgain_WhenTheProjectChangesItsReference()
    {
        File.WriteAllText(_profilePath, """{ "disableComponents": true }""");
        _project.EngineProfile = "Engine.gumengine";
        _sut.AreComponentsDisabled.ShouldBeTrue();

        _project.EngineProfile = "";

        _sut.AreComponentsDisabled.ShouldBeFalse();
    }

    [Fact]
    public void Reload_ShouldReadTheEditedFileAndRaiseChanged()
    {
        File.WriteAllText(_profilePath, """{ "disableComponents": true }""");
        _project.EngineProfile = "Engine.gumengine";
        _sut.AreComponentsDisabled.ShouldBeTrue();
        int changedCount = 0;
        _sut.Changed += (_, _) => changedCount++;
        File.WriteAllText(_profilePath, """{ "disableBehaviors": true }""");

        _sut.Reload();

        _sut.AreComponentsDisabled.ShouldBeFalse();
        _sut.AreBehaviorsDisabled.ShouldBeTrue();
        changedCount.ShouldBe(1);
    }

    [Fact]
    public void Reload_ShouldWarnAndBecomeUnrestricted_WhenTheFileWasDeleted()
    {
        File.WriteAllText(_profilePath, """{ "disableComponents": true }""");
        _project.EngineProfile = "Engine.gumengine";
        _sut.AreComponentsDisabled.ShouldBeTrue();
        File.Delete(_profilePath);

        _sut.Reload();

        _sut.AreComponentsDisabled.ShouldBeFalse();
        _output.Verify(x => x.AddOutput(It.Is<string>(text => text.Contains("Engine.gumengine"))), Times.Once);
    }

    [Fact]
    public void ProfileFilePath_ShouldBeTheResolvedFile()
    {
        File.WriteAllText(_profilePath, "{}");
        _project.EngineProfile = "Engine.gumengine";

        _sut.ProfileFilePath.ShouldNotBeNull();
        _sut.ProfileFilePath!.FileNameNoPath.ShouldBe("Engine.gumengine");
    }
}
