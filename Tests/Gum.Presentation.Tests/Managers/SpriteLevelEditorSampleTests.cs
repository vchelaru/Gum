using Gum.DataTypes;
using Gum.Managers;
using Moq;
using Shouldly;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Gum.Presentation.Tests.Managers;

/// <summary>
/// The checked-in Samples/SpriteLevelEditor project: it points at its .gumengine, which loads without a warning and
/// leaves Sprite as the only addable type with most of its variables hidden (#7723).
/// </summary>
public class SpriteLevelEditorSampleTests : BaseTestClass
{
    private readonly Mock<IOutputManager> _output = new Mock<IOutputManager>();
    private readonly GumProjectSave _project;
    private readonly ProjectRestrictions _sut;

    public SpriteLevelEditorSampleTests()
    {
        string projectFile = Path.Combine(FindRepoRoot(), "Samples", "SpriteLevelEditor", "SpriteLevelEditor.gumx");
        _project = GumProjectSave.Load(projectFile);
        ObjectFinder.Self.GumProjectSave = _project;
        _sut = new ProjectRestrictions(new EngineProfileLoader(), _output.Object);
    }

    [Fact]
    public void Project_ReferencesItsProfile_WithoutAnyInlineRestrictions()
    {
        _project.EngineProfile.ShouldBe("MonoGameSprites.gumengine");
        _sut.ProfileFilePath.ShouldNotBeNull();
        _sut.ProfileFilePath!.Exists().ShouldBeTrue();
        _output.Verify(output => output.AddOutput(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public void SpriteIsTheOnlyAddableType()
    {
        string[] addable = _project.StandardElements
            .Where(standard => !_sut.IsInstanceTypeBlocked(standard))
            .Select(standard => standard.Name)
            .ToArray();

        addable.ShouldBe(new[] { "Sprite" });
        _sut.AreComponentsDisabled.ShouldBeTrue();
        _sut.AreBehaviorsDisabled.ShouldBeTrue();
    }

    [Fact]
    public void SpriteHidesEveryVariableButPositionWidthAndSourceFile()
    {
        foreach (string shown in new[] { "X", "Y", "Width", "SourceFile" })
        {
            _sut.IsVariableHidden("Sprite", shown).ShouldBeFalse(shown);
        }
        foreach (string hidden in new[] { "Rotation", "Height", "Alpha", "Parent", "TextureLeft" })
        {
            _sut.IsVariableHidden("Sprite", hidden).ShouldBeTrue(hidden);
        }
    }

    [Fact]
    public void Project_ShipsTheFontItsTextStandardNames_AndItsCachedFont()
    {
        string folder = Path.GetDirectoryName(_project.FullFileName)!;
        StandardElementSave text = _project.StandardElements.Single(standard => standard.Name == "Text");
        string fontFile = (string)text.DefaultState.Variables.Single(variable => variable.Name == "Font").Value!;

        File.Exists(Path.Combine(folder, fontFile)).ShouldBeTrue(fontFile);
        Directory.GetFiles(Path.Combine(folder, "FontCache"), "*.fnt").ShouldNotBeEmpty();
    }
    private static string FindRepoRoot()
    {
        string current = AppContext.BaseDirectory;
        for (int i = 0; i < 10; i++)
        {
            if (Directory.Exists(Path.Combine(current, "Gum")) && File.Exists(Path.Combine(current, "GumFull.sln")))
            {
                return current;
            }
            current = Path.GetFullPath(Path.Combine(current, ".."));
        }
        throw new DirectoryNotFoundException("Could not locate the repo root from " + AppContext.BaseDirectory);
    }
}