using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
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
/// Tests for <see cref="CodeFileMigrationApplier"/>, which carries out a migration plan after
/// backing up every file it touches, and undoes itself if any step fails (issue #5846).
/// </summary>
public class CodeFileMigrationApplierTests : BaseTestClass
{
    private const string EditedCustomCode = """
        namespace MyGame.Components.Controls
        {
            partial class ButtonCloseRuntime
            {
                partial void CustomInitialize()
                {
                    Width = 10;
                }
            }
        }
        """;

    private readonly string _tempDirectory;
    private readonly FilePath _projectFile;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly CodeFileBackupService _backupService;
    private readonly GumProjectSave _project;

    public CodeFileMigrationApplierTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumMigrationApplierTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _projectFile = new FilePath(Path.Combine(_tempDirectory, "Game.gumx"));
        _backupService = new CodeFileBackupService(Path.Combine(_tempDirectory, "Backups"), () => DateTime.UtcNow);

        // The real recycle bin removes the files, so the fake does too.
        _fileCommands
            .Setup(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()))
            .Callback<IReadOnlyList<FilePath>>(files => files.ToList().ForEach(file => File.Delete(file.FullPath)));

        _project = new GumProjectSave();
        ComponentSave component = new ComponentSave { Name = "Controls/ButtonClose", BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        _project.Components.Add(component);
    }

    [Fact]
    public void Apply_RemovesRecyclesAndMovesWithARewrittenHeader_AndLeavesSkippedFiles()
    {
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/ButtonCloseRuntime.cs", EditedCustomCode);
        FilePath newStub = WriteFile("Components/Controls/ButtonClose.cs", "partial class ButtonClose { partial void CustomInitialize() { } }");
        FilePath otherStub = WriteFile("Components/Controls/IconRuntime.cs", "partial class IconRuntime { }");
        FilePath conflict = WriteFile("Components/Controls/DenyRuntime.cs", "partial class DenyRuntime { int x; }");
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, oldGenerated),
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.MoveCustomCode, oldCustom, newStub),
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveUntouchedStub, otherStub),
            new CodeFileMigrationStep("Controls/Deny", CodeFileMigrationAction.SkipConflict, conflict),
        });

        CodeFileMigrationResult result = CreateApplier().Apply(_projectFile, CreateProjectSettings(), plan);

        result.Error.ShouldBeNull();
        File.Exists(oldGenerated.FullPath).ShouldBeFalse();
        File.Exists(oldCustom.FullPath).ShouldBeFalse();
        File.Exists(otherStub.FullPath).ShouldBeFalse();
        string moved = File.ReadAllText(newStub.FullPath);
        moved.ShouldContain("partial class ButtonClose");
        moved.ShouldContain("Width = 10;");
        File.Exists(conflict.FullPath).ShouldBeTrue();
        // The replaced stub and the removed custom stub went to the recycle bin, never a plain delete.
        _fileCommands.Verify(x => x.MoveToRecycleBin(It.Is<IReadOnlyList<FilePath>>(files =>
            files.Count == 2 && files.Contains(newStub) && files.Contains(otherStub))));
        result.Backup!.Entries.Select(entry => entry.OriginalPath)
            .ShouldBe(new[] { oldGenerated, oldCustom, newStub, otherStub }, ignoreOrder: true);
        result.Backup.CreatedFiles.Single().Path.ShouldBe(newStub);
    }

    [Fact]
    public void Apply_PutsEverythingBack_WhenAStepFails()
    {
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath otherStub = WriteFile("Components/Controls/IconRuntime.cs", "partial class IconRuntime { }");
        _fileCommands
            .Setup(x => x.MoveToRecycleBin(It.IsAny<IReadOnlyList<FilePath>>()))
            .Throws(new IOException("The file is open in another program."));
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, oldGenerated),
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveUntouchedStub, otherStub),
        });

        CodeFileMigrationResult result = CreateApplier().Apply(_projectFile, CreateProjectSettings(), plan);

        result.Error.ShouldNotBeNull().ShouldContain("The file is open in another program.");
        File.ReadAllText(oldGenerated.FullPath).ShouldBe("//Code for Controls/ButtonClose (Container)");
        File.Exists(otherStub.FullPath).ShouldBeTrue();
    }

    [Fact]
    public void Apply_RemovesTheFoldersItEmptied_ButNotTheCodeProjectRoot()
    {
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/ButtonCloseRuntime.cs", EditedCustomCode);
        FilePath oldStub = WriteFile("Components/Controls/IconRuntime.cs", "partial class IconRuntime { }");
        FilePath newCustom = new FilePath(Path.Combine(_tempDirectory, "GumCodeGen", "Components", "Controls", "ButtonClose.cs"));
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, oldGenerated),
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.MoveCustomCode, oldCustom, newCustom),
            new CodeFileMigrationStep("Controls/Icon", CodeFileMigrationAction.RemoveUntouchedStub, oldStub),
        });

        CodeFileMigrationResult result = CreateApplier().Apply(_projectFile, CreateProjectSettings(), plan);

        result.Error.ShouldBeNull();
        Directory.Exists(Path.Combine(_tempDirectory, "Components")).ShouldBeFalse("Components/Controls and Components were left empty");
        File.Exists(newCustom.FullPath).ShouldBeTrue();
        Directory.Exists(_tempDirectory).ShouldBeTrue();
    }

    [Fact]
    public void Apply_KeepsAFolderThatStillHoldsAFile()
    {
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath handWritten = WriteFile("Components/Controls/ButtonClose.Input.cs", "partial class ButtonClose { }");
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, oldGenerated),
        });

        CodeFileMigrationResult result = CreateApplier().Apply(_projectFile, CreateProjectSettings(), plan);

        result.Error.ShouldBeNull();
        File.Exists(handWritten.FullPath).ShouldBeTrue();
        File.Exists(oldGenerated.FullPath).ShouldBeFalse();
    }

    [Fact]
    public void Apply_DoesNotRemoveEmptyFoldersOutsideTheCodeProjectRoot()
    {
        CodeOutputProjectSettings settings = CreateProjectSettings();
        settings.CodeProjectRoot = "Game/";
        FilePath outside = WriteFile("Elsewhere/Components/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, outside),
        });

        CreateApplier().Apply(_projectFile, settings, plan);

        File.Exists(outside.FullPath).ShouldBeFalse();
        Directory.Exists(Path.Combine(_tempDirectory, "Elsewhere", "Components")).ShouldBeTrue();
    }

    [Fact]
    public void Apply_DoesNotRemoveFolders_WhenNoCodeProjectRootIsConfigured()
    {
        // Without a root there is no boundary to stop at, so no folder may be removed.
        CodeOutputProjectSettings settings = CreateProjectSettings();
        settings.CodeProjectRoot = string.Empty;
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, oldGenerated),
        });

        CreateApplier().Apply(_projectFile, settings, plan);

        File.Exists(oldGenerated.FullPath).ShouldBeFalse();
        Directory.Exists(Path.Combine(_tempDirectory, "Components", "Controls")).ShouldBeTrue();
    }

    [Fact]
    public void Restore_BringsBackTheFoldersApplyRemoved()
    {
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        CodeFileMigrationPlan plan = new CodeFileMigrationPlan(new[]
        {
            new CodeFileMigrationStep("Controls/ButtonClose", CodeFileMigrationAction.RemoveGenerated, oldGenerated),
        });
        CodeFileMigrationResult result = CreateApplier().Apply(_projectFile, CreateProjectSettings(), plan);
        Directory.Exists(Path.Combine(_tempDirectory, "Components")).ShouldBeFalse();

        _backupService.Restore(result.Backup!);

        File.ReadAllText(oldGenerated.FullPath).ShouldBe("//Code for Controls/ButtonClose (Container)");
    }

    private CodeFileMigrationApplier CreateApplier()
    {
        ObjectFinder.Self.GumProjectSave = _project;

        Mock<INameVerifier> nameVerifier = new();
        string? whyNotValid;
        CommonValidationError error;
        nameVerifier.Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error)).Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new(_tempDirectory + Path.DirectorySeparatorChar);
        CodeOutputElementSettingsManager elementSettingsManager = new(directoryProvider);
        CodeGenerator codeGenerator = new(codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
        CustomCodeHeaderRewriter rewriter = new(codeGenerator, new CustomCodeGenerator(codeGenerator, codeGenNameVerifier));

        return new CodeFileMigrationApplier(_backupService, _fileCommands.Object, rewriter, elementSettingsManager);
    }

    private static CodeOutputProjectSettings CreateProjectSettings() => new CodeOutputProjectSettings
    {
        CodeProjectRoot = "./",
        RootNamespace = "MyGame",
        OutputLibrary = OutputLibrary.MonoGameForms
    };

    private FilePath WriteFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(_tempDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return new FilePath(Path.GetFullPath(fullPath));
    }

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }
}
