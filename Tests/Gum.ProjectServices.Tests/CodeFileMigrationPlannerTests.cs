using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Moq;
using Shouldly;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ToolsUtilities;

namespace Gum.ProjectServices.Tests;

/// <summary>
/// Tests for <see cref="CodeFileMigrationPlanner"/>, which turns orphaned code files whose element
/// still exists (left behind by a code settings change) into a plan: what to remove, what to move to
/// the element's current path, and what to leave alone (issue #5846).
/// </summary>
public class CodeFileMigrationPlannerTests : BaseTestClass
{
    private const string StubCustomCode = """
        namespace MyGame.Components.Controls
        {
            partial class ButtonCloseRuntime
            {
                partial void CustomInitialize()
                {

                }
            }
        }
        """;

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

    public CodeFileMigrationPlannerTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "GumMigrationPlannerTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void CreatePlan_RemovesTheOldGeneratedFile_AndAnUntouchedOldStub()
    {
        // The reported case: MonoGame named components ButtonCloseRuntime, Gum Forms names them ButtonClose.
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/ButtonCloseRuntime.cs", StubCustomCode);

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, CreateProjectSettings(),
            new[] { Generated(oldGenerated, "Controls/ButtonClose"), Custom(oldCustom, "Controls/ButtonClose") });

        plan.Steps.Select(step => (step.Action, step.Source)).ShouldBe(new[]
        {
            (CodeFileMigrationAction.RemoveGenerated, oldGenerated),
            (CodeFileMigrationAction.RemoveUntouchedStub, oldCustom),
        });
    }

    [Fact]
    public void CreatePlan_MovesCustomCodeWithRealCode_ToTheElementsCurrentPath_ReplacingAStub()
    {
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/ButtonCloseRuntime.cs", EditedCustomCode);
        // Codegen already wrote a fresh stub at the new path; it holds nothing worth keeping.
        FilePath newCustom = WriteFile("Components/Controls/ButtonClose.cs", StubCustomCode);

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, CreateProjectSettings(),
            new[] { Generated(oldGenerated, "Controls/ButtonClose"), Custom(oldCustom, "Controls/ButtonClose") });

        CodeFileMigrationStep move = plan.Steps.Single(step => step.Source == oldCustom);
        move.Action.ShouldBe(CodeFileMigrationAction.MoveCustomCode);
        move.Destination.ShouldBe(newCustom);
    }

    [Theory]
    [InlineData("GumCodeGen/", "GumCodeGen/Components/Controls")]
    [InlineData("Gum", "GumComponents/Controls")]
    public void CreatePlan_MovesCustomCodeToThePrefixedFolder_WhenAPrefixIsSet(string prefix, string newFolder)
    {
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        CodeOutputProjectSettings projectSettings = CreateProjectSettings();
        projectSettings.GeneratedCodeFolderPrefix = prefix;
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonClose.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/ButtonClose.cs", EditedCustomCode);

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, projectSettings,
            new[] { Generated(oldGenerated, "Controls/ButtonClose"), Custom(oldCustom, "Controls/ButtonClose") });

        plan.Steps.Single(step => step.Source == oldGenerated).Action.ShouldBe(CodeFileMigrationAction.RemoveGenerated);
        CodeFileMigrationStep move = plan.Steps.Single(step => step.Source == oldCustom);
        move.Action.ShouldBe(CodeFileMigrationAction.MoveCustomCode);
        move.Destination.ShouldBe(new FilePath(Path.Combine(_tempDirectory, Path.Combine(newFolder.Split('/')), "ButtonClose.cs")));
    }

    [Fact]
    public void CreatePlan_MovesCustomCodeBackToTheDefaultFolder_WhenThePrefixIsCleared()
    {
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        FilePath oldGenerated = WriteFile("GumCodeGen/Components/Controls/ButtonClose.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("GumCodeGen/Components/Controls/ButtonClose.cs", EditedCustomCode);

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, CreateProjectSettings(),
            new[] { Generated(oldGenerated, "Controls/ButtonClose"), Custom(oldCustom, "Controls/ButtonClose") });

        CodeFileMigrationStep move = plan.Steps.Single(step => step.Source == oldCustom);
        move.Action.ShouldBe(CodeFileMigrationAction.MoveCustomCode);
        move.Destination.ShouldBe(new FilePath(Path.Combine(_tempDirectory, "Components", "Controls", "ButtonClose.cs")));
    }

    [Fact]
    public void CreatePlan_SkipsCustomCode_WhenTheCurrentPathAlsoHasRealCode()
    {
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        FilePath oldGenerated = WriteFile("Components/Controls/ButtonCloseRuntime.Generated.cs", "//Code for Controls/ButtonClose (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/ButtonCloseRuntime.cs", EditedCustomCode);
        FilePath newCustom = WriteFile("Components/Controls/ButtonClose.cs", EditedCustomCode.Replace("ButtonCloseRuntime", "ButtonClose"));

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, CreateProjectSettings(),
            new[] { Generated(oldGenerated, "Controls/ButtonClose"), Custom(oldCustom, "Controls/ButtonClose") });

        // Neither file is touched; the user merges by hand.
        CodeFileMigrationStep conflict = plan.Steps.Single(step => step.Source == oldCustom);
        conflict.Action.ShouldBe(CodeFileMigrationAction.SkipConflict);
        conflict.Destination.ShouldBe(newCustom);
    }

    [Fact]
    public void CreatePlan_MovesOnlyTheFirstOfTwoOldFiles_ForTheSameElement()
    {
        // The project switched settings twice, leaving real code at two old paths for one element.
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        FilePath firstCustom = WriteFile("Components/Controls/ButtonCloseRuntime.cs", EditedCustomCode);
        FilePath secondCustom = WriteFile("Old/Controls/ButtonClose.cs", EditedCustomCode);

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, CreateProjectSettings(),
            new[] { Custom(firstCustom, "Controls/ButtonClose"), Custom(secondCustom, "Controls/ButtonClose") });

        plan.Steps.Select(step => step.Action).ShouldBe(new[]
        {
            CodeFileMigrationAction.MoveCustomCode,
            CodeFileMigrationAction.SkipConflict,
        });
    }

    [Fact]
    public void CreatePlan_SkipsFiles_WhoseElementNoLongerExists()
    {
        // A deleted element's files are not a migration; the orphan rows' Delete File handles them.
        GumProjectSave project = CreateProject(CreateComponent("Controls/ButtonClose"));
        FilePath oldGenerated = WriteFile("Components/Controls/Deleted.Generated.cs", "//Code for Controls/Deleted (Container)");
        FilePath oldCustom = WriteFile("Components/Controls/Deleted.cs", EditedCustomCode);

        CodeFileMigrationPlan plan = CreatePlanner(project).CreatePlan(project, CreateProjectSettings(),
            new[] { Generated(oldGenerated, "Controls/Deleted"), Custom(oldCustom, "Controls/Deleted") });

        plan.Steps.Select(step => step.Action).ShouldBe(new[]
        {
            CodeFileMigrationAction.SkipNoElement,
            CodeFileMigrationAction.SkipNoElement,
        });
    }

    #region Helpers

    private CodeFileMigrationPlanner CreatePlanner(GumProjectSave project)
    {
        ObjectFinder.Self.GumProjectSave = project;

        Mock<INameVerifier> mockNameVerifier = new Mock<INameVerifier>();
        string? whyNotValid;
        CommonValidationError error;
        mockNameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);
        CodeGenerationNameVerifier codeGenNameVerifier = new CodeGenerationNameVerifier(mockNameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider =
            new FixedProjectDirectoryProvider(_tempDirectory + Path.DirectorySeparatorChar);
        CodeOutputElementSettingsManager elementSettingsManager =
            new CodeOutputElementSettingsManager(directoryProvider);
        CodeGenerator codeGenerator = new CodeGenerator(
            codeGenNameVerifier, new LocalizationService(), elementSettingsManager, directoryProvider);
        CodeGenerationFileLocationsService fileLocationsService = new CodeGenerationFileLocationsService(
            codeGenerator, codeGenNameVerifier, directoryProvider);

        return new CodeFileMigrationPlanner(
            codeGenerator, fileLocationsService, elementSettingsManager, new CustomCodeStubDetector());
    }

    private static CodeOutputProjectSettings CreateProjectSettings() => new CodeOutputProjectSettings
    {
        CodeProjectRoot = "./",
        RootNamespace = "MyGame",
        OutputLibrary = OutputLibrary.MonoGameForms
    };

    private static GumProjectSave CreateProject(ComponentSave component)
    {
        GumProjectSave project = new GumProjectSave();
        project.Components.Add(component);
        return project;
    }

    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        StateSave defaultState = new StateSave { Name = "Default" };
        defaultState.ParentContainer = component;
        component.States.Add(defaultState);
        return component;
    }

    private static OrphanCodeFile Generated(FilePath path, string elementName) =>
        new OrphanCodeFile(path, OrphanCodeFileKind.Generated, elementName);

    private static OrphanCodeFile Custom(FilePath path, string elementName) =>
        new OrphanCodeFile(path, OrphanCodeFileKind.CustomCode, elementName);

    private FilePath WriteFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(_tempDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return new FilePath(Path.GetFullPath(fullPath));
    }

    #endregion

    public override void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
        base.Dispose();
    }
}
