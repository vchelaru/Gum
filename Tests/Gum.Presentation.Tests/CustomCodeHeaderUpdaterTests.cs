using CodeOutputPlugin.Manager;
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
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for <see cref="CustomCodeHeaderUpdater"/>, which rewrites custom code headers after a Code
/// tab edit changes the namespace or inheritance settings (issue #5855).
/// </summary>
public class CustomCodeHeaderUpdaterTests : BaseTestClass
{
    private const string Title = "Update Custom Code";

    private readonly string _tempDirectory;
    private readonly FilePath _projectFile;
    private readonly CodeFileBackupService _backupService;
    private readonly Mock<IElementCodeRegenerator> _regenerator = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly GumProjectSave _project;

    public CustomCodeHeaderUpdaterTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumHeaderUpdaterTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
        _projectFile = new FilePath(Path.Combine(_tempDirectory, "Game.gumx"));
        _backupService = new CodeFileBackupService(Path.Combine(_tempDirectory, "Backups"), () => DateTime.UtcNow);

        _project = new GumProjectSave();
        _project.Components.Add(CreateComponent("Controls/ButtonClose"));
        _project.Components.Add(CreateComponent("Controls/Icon"));
        _project.Components.Add(CreateComponent("Controls/Never"));
    }

    [Fact]
    public void Update_RewritesStaleCustomCode_BacksItUp_AndRegenerates_WhenConfirmed()
    {
        string oldCode = """
            namespace OldGame.Components.Controls
            {
                partial class ButtonClose
                {
                    partial void CustomInitialize()
                    {
                        Width = 10;
                    }
                }
            }
            """;
        FilePath stale = WriteFile("Components/Controls/ButtonClose.cs", oldCode);
        // Already in the new namespace, so it is neither listed nor touched.
        string current = "namespace MyGame.Components.Controls\n{\n    partial class Icon\n    {\n    }\n}\n";
        FilePath matching = WriteFile("Components/Controls/Icon.cs", current);
        // Never generated, so its generated half would not follow; left alone.
        string neverCode = "namespace OldGame.Components.Controls\n{\n    partial class Never\n    {\n    }\n}\n";
        FilePath never = WriteFile("Components/Controls/Never.cs", neverCode);
        WriteFile("Components/Controls/Never.codsj", "{ \"GenerationBehavior\": 0 }");
        CodeOutputProjectSettings settings = CreateProjectSettings();
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), Title, It.Is<MessageDialogStyle>(s => s != null && s.AffirmativeText == "Update")))
            .Returns(MessageDialogResult.Affirmative);

        CreateUpdater().Update(_project, _projectFile, settings, "Root Namespace from OldGame to MyGame");

        _dialogService.Verify(d => d.ShowMessage(
            It.Is<string>(text => text.StartsWith("You changed Root Namespace from OldGame to MyGame.")
                && text.Contains("  Components/Controls/ButtonClose.cs") && !text.Contains("Icon.cs")),
            Title, It.IsAny<MessageDialogStyle>()));
        string rewritten = File.ReadAllText(stale.FullPath);
        rewritten.ShouldContain("namespace MyGame.Components.Controls");
        rewritten.ShouldContain("Width = 10;");
        File.ReadAllText(matching.FullPath).ShouldBe(current);
        File.ReadAllText(never.FullPath).ShouldBe(neverCode);
        _regenerator.Verify(r => r.Regenerate("Controls/ButtonClose", settings), Times.Once);
        _regenerator.Verify(r => r.Regenerate("Controls/Icon", It.IsAny<CodeOutputProjectSettings>()), Times.Never);

        // Restore Last Code File Migration undoes it like a migration.
        _backupService.Restore(_backupService.List(_projectFile).ShouldHaveSingleItem());
        File.ReadAllText(stale.FullPath).ShouldBe(oldCode);
    }

    [Fact]
    public void Update_MovesTheBaseClassOutOfCustomCode_KeepingOtherBases()
    {
        FilePath custom = WriteFile("Components/Controls/ButtonClose.cs",
            "namespace MyGame.Components.Controls\n{\n    partial class ButtonClose : global::Gum.Forms.Controls.FrameworkElement, System.IDisposable\n    {\n    }\n}\n");
        CodeOutputProjectSettings settings = CreateProjectSettings();
        settings.InheritanceLocation = InheritanceLocation.InGeneratedCode;
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), Title, It.IsAny<MessageDialogStyle>()))
            .Returns(MessageDialogResult.Affirmative);

        CreateUpdater().Update(_project, _projectFile, settings, "Inheritance Location from InCustomCode to InGeneratedCode");

        File.ReadAllText(custom.FullPath).ShouldContain("partial class ButtonClose : System.IDisposable\n");
    }

    [Fact]
    public void Update_ChangesNothing_WhenDeclined_AndAsksNothing_WhenEveryFileMatches()
    {
        string oldCode = "namespace OldGame.Components.Controls\n{\n    partial class ButtonClose\n    {\n    }\n}\n";
        FilePath stale = WriteFile("Components/Controls/ButtonClose.cs", oldCode);
        CodeOutputProjectSettings settings = CreateProjectSettings();
        _dialogService.Setup(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle>()))
            .Returns(MessageDialogResult.Negative);

        CustomCodeHeaderUpdater updater = CreateUpdater();
        updater.Update(_project, _projectFile, settings, "Root Namespace from OldGame to MyGame");
        File.ReadAllText(stale.FullPath).ShouldBe(oldCode);

        settings.RootNamespace = "OldGame";
        updater.Update(_project, _projectFile, settings, "Root Namespace from MyGame to OldGame");

        _dialogService.Verify(d => d.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle>()), Times.Once);
        _regenerator.VerifyNoOtherCalls();
        _backupService.List(_projectFile).ShouldBeEmpty();
    }

    private CustomCodeHeaderUpdater CreateUpdater()
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
        CodeGenerationFileLocationsService fileLocations = new(codeGenerator, codeGenNameVerifier, directoryProvider);

        return new CustomCodeHeaderUpdater(codeGenerator, fileLocations, elementSettingsManager, rewriter,
            _backupService, _regenerator.Object, _dialogService.Object);
    }

    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private static CodeOutputProjectSettings CreateProjectSettings() => new CodeOutputProjectSettings
    {
        CodeProjectRoot = "./",
        RootNamespace = "MyGame",
        AppendFolderToNamespace = true,
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
