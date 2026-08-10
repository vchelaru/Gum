using CodeOutputPlugin.Manager;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Characterization (pinning) test for RenameService, relocated out of Gum/CodeOutputPlugin/Manager
/// (Gum.csproj) into the headless Gum.Presentation assembly (#3905) - no WPF dependency of its own,
/// only interfaces (IDialogService) and engine types already headless. Bumped from internal to public
/// (class and the two Handle* methods) so the plugin, still in Gum.csproj, can call it across the new
/// assembly boundary.
/// </summary>
public class RenameServiceTests : BaseTestClass
{
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly RenameService _renameService;

    public RenameServiceTests()
    {
        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);

        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new(projectDirectory: null);
        CodeOutputElementSettingsManager elementSettingsManager = new(directoryProvider);
        LocalizationService localizationService = new();

        CodeGenerator codeGenerator = new(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);

        CustomCodeGenerator customCodeGenerator = new(codeGenerator, codeGenNameVerifier);

        // codeGenerationService is unreached by the paths these tests exercise, so null! is safe here
        // (mirrors ParentSetLogicTests's same null!-for-unreached-dependency pattern).
        _renameService = new RenameService(
            codeGenerationService: null!,
            codeGenerator,
            customCodeGenerator,
            codeGenNameVerifier,
            _dialogService.Object,
            directoryProvider);
    }

    [Fact]
    public void HandleRename_WithEmptyCodeProjectRoot_DoesNothing()
    {
        ComponentSave element = new ComponentSave { Name = "MyComponent" };

        _renameService.HandleRename(
            element,
            oldName: "OldName",
            codeOutputProjectSettings: new CodeOutputProjectSettings(), // CodeProjectRoot defaults to empty
            visualApi: VisualApi.Gum);

        _dialogService.VerifyNoOtherCalls();
    }

    private static ComponentSave CreateComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WhenElementMovedToNewFolder_UpdatesNamespaceAndKeepsClassName()
    {
        ComponentSave element = CreateComponent("NewFolder/MyComponent");
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            RootNamespace = "MyGame",
            AppendFolderToNamespace = true
        };
        string contents =
            "namespace MyGame.Components.OldFolder\n" +
            "{\n" +
            "    partial class MyComponent\n" +
            "    {\n" +
            "        partial void CustomInitialize()\n" +
            "        {\n" +
            "        }\n" +
            "    }\n" +
            "}\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("namespace MyGame.Components.NewFolder");
        updated.ShouldNotContain("OldFolder");
        updated.ShouldContain("partial class MyComponent");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithFileScopedNamespace_UpdatesNamespaceAndKeepsSemicolon()
    {
        ComponentSave element = CreateComponent("NewFolder/MyComponent");
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            RootNamespace = "MyGame",
            AppendFolderToNamespace = true
        };
        string contents =
            "namespace MyGame.Components.OldFolder;\n" +
            "\n" +
            "partial class MyComponent\n" +
            "{\n" +
            "}\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("namespace MyGame.Components.NewFolder;");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithNoRootNamespace_LeavesExistingNamespaceAlone()
    {
        ComponentSave element = CreateComponent("NewFolder/MyComponent");
        // RootNamespace is empty, so codegen would emit no namespace at all. Rewriting to an empty
        // namespace would produce invalid code, so the existing (hand-written) namespace must survive.
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings { AppendFolderToNamespace = true };
        string contents =
            "namespace MyHandWrittenNamespace\n" +
            "{\n" +
            "    partial class MyComponent\n" +
            "    {\n" +
            "    }\n" +
            "}\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("namespace MyHandWrittenNamespace");
    }
}
