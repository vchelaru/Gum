using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;
using OrphanCodeFilePlugin;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// End to end through the real scan, planner, applier, backup and migrator: setting a Custom Code
/// Folder moves only the custom file, and Restore Last Code File Migration puts it back (issue #6775).
/// </summary>
public class CustomCodeFolderMigrationTests : BaseTestClass
{
    private const string EditedCustomCode = """
        namespace MyGame.Components
        {
            partial class Button
            {
                partial void CustomInitialize()
                {
                    Width = 10;
                }
            }
        }
        """;

    private readonly string _tempDirectory;
    private readonly string _customDirectory;
    private readonly FilePath _projectFile;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IElementCodeRegenerator> _regenerator = new();
    private readonly GumProjectSave _project;
    private readonly OrphanCodeFileScanService _scanService;
    private readonly CodeFileMigrator _migrator;

    public CustomCodeFolderMigrationTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumCustomFolderMigration_" + Guid.NewGuid().ToString("N"));
        _customDirectory = Path.Combine(Path.GetTempPath(), "GumCustomFolderOutside_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _projectFile = new FilePath(Path.Combine(_tempDirectory, "Game.gumx"));
        _fileCommands
            .Setup(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()))
            .Callback<IReadOnlyList<FilePath>>(files => files.ToList().ForEach(file => File.Delete(file.FullPath)));
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle>()))
            .Returns(MessageDialogResult.Affirmative);

        _project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "Button", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = _project;

        Mock<INameVerifier> nameVerifier = new();
        string? whyNotValid;
        CommonValidationError error;
        nameVerifier.Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error)).Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new(_tempDirectory + Path.DirectorySeparatorChar);
        CodeOutputElementSettingsManager elementSettingsManager = new(directoryProvider);
        CodeGenerator codeGenerator = new(codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
        CodeGenerationFileLocationsService locations = new(codeGenerator, codeGenNameVerifier, directoryProvider);
        CodeFileBackupService backupService = new(Path.Combine(_tempDirectory, "Backups"), () => DateTime.UtcNow);
        CustomCodeHeaderRewriter rewriter = new(codeGenerator, new CustomCodeGenerator(codeGenerator, codeGenNameVerifier));

        _scanService = new OrphanCodeFileScanService(codeGenerator, locations, elementSettingsManager, directoryProvider);
        _migrator = new CodeFileMigrator(
            new CodeFileMigrationPlanner(codeGenerator, locations, elementSettingsManager, new CustomCodeStubDetector()),
            new CodeFileMigrationPlanFormatter(),
            new CodeFileMigrationApplier(backupService, _fileCommands.Object, rewriter, elementSettingsManager),
            backupService, _regenerator.Object, _dialogService.Object);
    }

    [Fact]
    public void Migrate_MovesTheCustomFileToAnAbsoluteCustomFolder_AndRestoreBringsItBack()
    {
        CodeOutputProjectSettings previous = Settings();
        CodeOutputProjectSettings current = Settings();
        current.CustomCodeFolder = _customDirectory;
        string generated = Write(Path.Combine(_tempDirectory, "Components", "Button.Generated.cs"), "//Code for Button (Container)");
        string oldCustom = Write(Path.Combine(_tempDirectory, "Components", "Button.cs"), EditedCustomCode);
        string newCustom = Path.Combine(_customDirectory, "Components", "Button.cs");

        _migrator.Migrate(_project, _projectFile, current, _scanService.Scan(_project, current, previous), "Custom Code Folder");

        File.Exists(newCustom).ShouldBeTrue();
        File.ReadAllText(newCustom).ShouldContain("Width = 10;");
        File.Exists(oldCustom).ShouldBeFalse();
        File.Exists(generated).ShouldBeTrue("the generated file does not move with the custom file");

        _migrator.RestoreLast(_projectFile);

        File.ReadAllText(oldCustom).ShouldContain("Width = 10;");
        File.Exists(newCustom).ShouldBeFalse();
    }

    [Fact]
    public void Migrate_MovesTheCustomFileBesideTheGeneratedFile_WhenTheCustomFolderIsCleared()
    {
        CodeOutputProjectSettings previous = Settings();
        previous.CustomCodeFolder = "Mine/";
        CodeOutputProjectSettings current = Settings();
        Write(Path.Combine(_tempDirectory, "Components", "Button.Generated.cs"), "//Code for Button (Container)");
        string oldCustom = Write(Path.Combine(_tempDirectory, "Mine", "Components", "Button.cs"), EditedCustomCode);

        _migrator.Migrate(_project, _projectFile, current, _scanService.Scan(_project, current, previous), "Custom Code Folder");

        File.ReadAllText(Path.Combine(_tempDirectory, "Components", "Button.cs")).ShouldContain("Width = 10;");
        File.Exists(oldCustom).ShouldBeFalse();
    }

    [Fact]
    public void Migrate_AsksBeforeMovingTheCustomFile_AndLeavesItWhenDeclined()
    {
        CodeOutputProjectSettings current = Settings();
        current.CustomCodeFolder = "Mine/";
        Write(Path.Combine(_tempDirectory, "Components", "Button.Generated.cs"), "//Code for Button (Container)");
        string oldCustom = Write(Path.Combine(_tempDirectory, "Components", "Button.cs"), EditedCustomCode);
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle>()))
            .Returns(MessageDialogResult.Negative);

        _migrator.Migrate(_project, _projectFile, current, _scanService.Scan(_project, current, Settings()), "Custom Code Folder");

        _dialogService.Verify(d => d.ShowMessage(
            It.Is<string>(text => text.Contains("Custom Code Folder") && text.Contains("Button.cs")),
            "Migrate Code Files", It.IsAny<MessageDialogStyle>()), Times.Once);
        File.Exists(oldCustom).ShouldBeTrue();
        File.Exists(Path.Combine(_tempDirectory, "Mine", "Components", "Button.cs")).ShouldBeFalse();
    }

    [Fact]
    public void Migrate_MovesTheCustomFile_OnlyAfterTheUserConfirms()
    {
        CodeOutputProjectSettings current = Settings();
        current.CustomCodeFolder = "Mine/";
        Write(Path.Combine(_tempDirectory, "Components", "Button.Generated.cs"), "//Code for Button (Container)");
        string oldCustom = Write(Path.Combine(_tempDirectory, "Components", "Button.cs"), EditedCustomCode);
        bool? askedBeforeMoving = null;
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), "Migrate Code Files", It.IsAny<MessageDialogStyle>()))
            .Callback(() => askedBeforeMoving ??= File.Exists(oldCustom))
            .Returns(MessageDialogResult.Affirmative);

        _migrator.Migrate(_project, _projectFile, current, _scanService.Scan(_project, current, Settings()), "Custom Code Folder");

        askedBeforeMoving.ShouldBe(true, "the file was still in place when the prompt appeared");
        File.Exists(oldCustom).ShouldBeFalse();
        File.ReadAllText(Path.Combine(_tempDirectory, "Mine", "Components", "Button.cs")).ShouldContain("Width = 10;");
    }

    private CodeOutputProjectSettings Settings() => new CodeOutputProjectSettings
    {
        CodeProjectRoot = "./",
        RootNamespace = "MyGame",
        OutputLibrary = OutputLibrary.GumForms
    };

    private static string Write(string path, string contents)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, contents);
        return path;
    }

    public override void Dispose()
    {
        base.Dispose();
        foreach (string directory in new[] { _tempDirectory, _customDirectory })
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }
}
