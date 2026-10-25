using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// The opt-in .editorconfig in the generated code folder (#6776): when it is written, when it is
/// never touched, and how it follows the settings.
/// </summary>
public class GeneratedFolderEditorConfigTests : BaseTestClass
{
    private const string ExpectedText =
        "# Written by Gum. Marks the generated files in this folder as generated code.\n[*.cs]\ngenerated_code = true\n";

    private readonly string _root;
    private readonly RecordingLogger _logger = new RecordingLogger();

    public GeneratedFolderEditorConfigTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "GumEditorConfig_" + Guid.NewGuid().ToString("N")) + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(_root);
    }

    private string EditorConfigIn(string folder) => Path.Combine(_root, folder, ".editorconfig");

    private CodeOutputProjectSettings Settings(string generated = "Gen", string custom = "Mine", bool mark = true) =>
        new CodeOutputProjectSettings
        {
            CodeProjectRoot = _root, GeneratedCodeFolder = generated, CustomCodeFolder = custom,
            OutputLibrary = OutputLibrary.GumForms, MarkGeneratedCodeFolder = mark,
        };

    private GeneratedFolderEditorConfig CreateSut()
    {
        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        CodeGenerationNameVerifier nameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new FixedProjectDirectoryProvider(_root);
        CodeOutputElementSettingsManager elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);
        CodeGenerator codeGenerator = new CodeGenerator(nameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
        return new GeneratedFolderEditorConfig(
            new CodeGenerationFileLocationsService(codeGenerator, nameVerifier, directoryProvider), _logger);
    }

    private static string Normalized(string text) => text.Replace("\r\n", "\n");

    [Fact]
    public void Sync_WritesNothing_WhenTheOptionIsOff()
    {
        CreateSut().Sync(Settings(mark: false));

        File.Exists(EditorConfigIn("Gen")).ShouldBeFalse();
        Directory.Exists(Path.Combine(_root, "Gen")).ShouldBeFalse();
        _logger.Messages.ShouldBeEmpty();
    }

    [Fact]
    public void Sync_WritesTheFileIntoTheGeneratedFolder_WhenTheOptionIsOn()
    {
        CreateSut().Sync(Settings());

        Normalized(File.ReadAllText(EditorConfigIn("Gen"))).ShouldBe(ExpectedText);
        File.Exists(EditorConfigIn("Mine")).ShouldBeFalse("the custom folder is never marked");
    }

    [Fact]
    public void Sync_LeavesAnUntouchedFileOfItsOwnAlone()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());
        DateTime written = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        File.SetLastWriteTimeUtc(EditorConfigIn("Gen"), written);

        sut.Sync(Settings());

        File.GetLastWriteTimeUtc(EditorConfigIn("Gen")).ShouldBe(written, "an identical file is not rewritten");
    }

    [Fact]
    public void Sync_RecreatesTheFile_WhenItWasDeleted()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());
        File.Delete(EditorConfigIn("Gen"));

        sut.Sync(Settings());

        File.Exists(EditorConfigIn("Gen")).ShouldBeTrue();
    }

    [Fact]
    public void Sync_NeverTouchesAForeignFile_AndReportsItOnce()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Gen"));
        File.WriteAllText(EditorConfigIn("Gen"), "root = true\n[*.cs]\nindent_size = 2\n");
        GeneratedFolderEditorConfig sut = CreateSut();

        sut.Sync(Settings());
        sut.Sync(Settings());

        File.ReadAllText(EditorConfigIn("Gen")).ShouldBe("root = true\n[*.cs]\nindent_size = 2\n");
        _logger.Messages.ShouldHaveSingleItem().ShouldContain("not written by Gum");
    }

    [Fact]
    public void Sync_TreatsAFileWhoseHeaderWasEdited_AsForeign()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Gen"));
        string edited = "# Mine now.\n[*.cs]\ngenerated_code = true\n";
        File.WriteAllText(EditorConfigIn("Gen"), edited);

        CreateSut().Sync(Settings());

        File.ReadAllText(EditorConfigIn("Gen")).ShouldBe(edited);
        _logger.Messages.ShouldHaveSingleItem().ShouldContain("not written by Gum");
    }

    [Fact]
    public void Sync_LeavesAGumFileTheUserAddedTo_AndSaysSo()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());
        File.AppendAllText(EditorConfigIn("Gen"), "indent_size = 2\n");
        string edited = File.ReadAllText(EditorConfigIn("Gen"));

        sut.Sync(Settings());

        File.ReadAllText(EditorConfigIn("Gen")).ShouldBe(edited);
        _logger.Messages.ShouldContain(message => message.Contains("was edited"));
    }

    [Fact]
    public void Sync_RemovesItsFile_WhenTheOptionIsTurnedOff()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());

        sut.Sync(Settings(mark: false), previous: Settings());

        File.Exists(EditorConfigIn("Gen")).ShouldBeFalse();
    }

    [Fact]
    public void Sync_KeepsAnEditedFile_WhenTheOptionIsTurnedOff_AndSaysSo()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());
        File.AppendAllText(EditorConfigIn("Gen"), "indent_size = 2\n");

        sut.Sync(Settings(mark: false), previous: Settings());

        File.Exists(EditorConfigIn("Gen")).ShouldBeTrue();
        _logger.Messages.ShouldContain(message => message.Contains("changed after Gum wrote it"));
    }

    [Fact]
    public void Sync_KeepsAForeignFile_WhenTheOptionIsTurnedOff()
    {
        Directory.CreateDirectory(Path.Combine(_root, "Gen"));
        File.WriteAllText(EditorConfigIn("Gen"), "root = true\n");

        CreateSut().Sync(Settings(mark: false), previous: Settings());

        File.ReadAllText(EditorConfigIn("Gen")).ShouldBe("root = true\n");
    }

    [Fact]
    public void Sync_MovesItsFile_WhenTheGeneratedFolderChanges()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());

        sut.Sync(Settings(generated: "Gen2"), previous: Settings());

        File.Exists(EditorConfigIn("Gen")).ShouldBeFalse();
        Normalized(File.ReadAllText(EditorConfigIn("Gen2"))).ShouldBe(ExpectedText);
    }

    [Fact]
    public void Sync_RemovesItsOldFile_WhenTheFolderChangesToOneThatCannotBeMarked()
    {
        GeneratedFolderEditorConfig sut = CreateSut();
        sut.Sync(Settings());

        sut.Sync(Settings(generated: ""), previous: Settings());

        File.Exists(EditorConfigIn("Gen")).ShouldBeFalse();
        File.Exists(Path.Combine(_root, ".editorconfig")).ShouldBeFalse();
    }

    [Theory]
    [InlineData("", "Mine", "Needs a Generated Code Folder")]
    [InlineData("Gen", "Gen", "Custom Code Folder")]
    [InlineData("Gen", "", "Custom Code Folder")]
    [InlineData("Gen", "Gen/Sub", "inside")]
    public void Sync_WritesNothing_WhenTheGeneratedFolderIsNotItsOwn(string generated, string custom, string reasonPart)
    {
        CodeOutputProjectSettings settings = Settings(generated, custom);
        GeneratedFolderEditorConfig sut = CreateSut();

        GeneratedFolderEditorConfig.GetUnavailableReason(settings, _root)!.ShouldContain(reasonPart);
        sut.Sync(settings);

        Directory.GetFiles(_root, ".editorconfig", SearchOption.AllDirectories).ShouldBeEmpty();
        _logger.Messages.ShouldHaveSingleItem().ShouldContain("skipped");
    }

    [Theory]
    [InlineData(".", "Mine", "project root")]
    [InlineData("./", "Mine", "project root")]
    [InlineData("Gen/../", "Mine", "project root")]
    [InlineData("Gen", "Mine/../Gen", "separate Custom Code Folder")]
    [InlineData("Gen", "Gen/./Sub", "inside")]
    [InlineData("Gen", "Gen//Sub", "inside")]
    [InlineData("Gen/./Sub", "Gen/Sub/Deep", "inside")]
    public void Sync_WritesNothing_WhenPathSegmentsHideThatTheFoldersOverlap(string generated, string custom, string reasonPart)
    {
        CodeOutputProjectSettings settings = Settings(generated, custom);

        GeneratedFolderEditorConfig.GetUnavailableReason(settings, _root)!.ShouldContain(reasonPart);
        CreateSut().Sync(settings);

        Directory.GetFiles(_root, ".editorconfig", SearchOption.AllDirectories).ShouldBeEmpty();
    }

    [Fact]
    public void GetUnavailableReason_IsNull_WhenTheGeneratedFolderIsWrittenWithDotSegments()
    {
        GeneratedFolderEditorConfig.GetUnavailableReason(Settings("./Gen/", "Other/../Mine"), _root).ShouldBeNull();
    }

    [Fact]
    public void GetUnavailableReason_IsNull_WhenTheGeneratedFolderIsSeparate()
    {
        GeneratedFolderEditorConfig.GetUnavailableReason(Settings(), _root).ShouldBeNull();
        GeneratedFolderEditorConfig.GetUnavailableReason(Settings("Gen", "Gen2"), _root).ShouldBeNull("a sibling with the same prefix is not nested");
    }

    [Fact]
    public void GetUnavailableReason_NamesTheRoot_WhenNoneIsSet()
    {
        GeneratedFolderEditorConfig.GetUnavailableReason(new CodeOutputProjectSettings(), _root)!.ShouldContain("Code Project Root");
    }

    [Fact]
    public void Serialize_ShouldOmitMarkGeneratedCodeFolder_WhenFalse()
    {
        string off = JsonConvert.SerializeObject(new CodeOutputProjectSettings());
        string on = JsonConvert.SerializeObject(new CodeOutputProjectSettings { MarkGeneratedCodeFolder = true });

        off.ShouldNotContain("MarkGeneratedCodeFolder");
        JsonConvert.DeserializeObject<CodeOutputProjectSettings>(on)!.MarkGeneratedCodeFolder.ShouldBeTrue();
    }

    public override void Dispose()
    {
        if (Directory.Exists(_root))
        {
            Directory.Delete(_root, recursive: true);
        }
        base.Dispose();
    }

    private class RecordingLogger : ICodeGenLogger
    {
        public List<string> Messages { get; } = new List<string>();

        public void PrintOutput(string message)
        {
            if (!message.StartsWith("Wrote ") && !message.StartsWith("Removed "))
            {
                Messages.Add(message);
            }
        }

        public void PrintError(string message) => Messages.Add(message);
    }
}
