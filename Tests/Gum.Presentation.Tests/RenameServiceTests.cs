using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services;
using Gum.Services.Dialogs;
using Moq;
using Shouldly;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for RenameService, which reconciles an element's generated and custom code files on disk
/// when the element's identity (name, containing folder, BaseType) changes. The HandleRename tests
/// run against a real temporary directory so they exercise the whole path - file move, header
/// rewrite, and regeneration - rather than just the string rewriting.
/// </summary>
public class RenameServiceTests : BaseTestClass
{
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly Mock<IGuiCommands> _guiCommands = new();
    private readonly RenameService _renameService;
    private readonly string _codeProjectRoot;

    public RenameServiceTests()
    {
        _codeProjectRoot = Path.Combine(Path.GetTempPath(), "GumRenameServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_codeProjectRoot);

        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);

        Mock<IRetryService> retryService = new();
        retryService
            .Setup(r => r.TryMultipleTimes(It.IsAny<Action>(), It.IsAny<int>()))
            .Callback<Action, int>((action, _) => action());

        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider = new(_codeProjectRoot);
        CodeOutputElementSettingsManager elementSettingsManager = new(directoryProvider);
        LocalizationService localizationService = new();

        CodeGenerator codeGenerator = new(
            codeGenNameVerifier,
            localizationService,
            elementSettingsManager,
            directoryProvider);

        CustomCodeGenerator customCodeGenerator = new(codeGenerator, codeGenNameVerifier);

        CodeGenerationService codeGenerationService = new(
            _guiCommands.Object,
            codeGenerator,
            _dialogService.Object,
            customCodeGenerator,
            codeGenNameVerifier,
            directoryProvider,
            retryService.Object);

        _renameService = new RenameService(
            codeGenerationService,
            codeGenerator,
            customCodeGenerator,
            codeGenNameVerifier,
            _dialogService.Object,
            directoryProvider);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_codeProjectRoot))
        {
            Directory.Delete(_codeProjectRoot, recursive: true);
        }
    }

    /// <summary>
    /// Builds an element with a Default state and registers it in ObjectFinder, which the rename and
    /// generation paths both consult.
    /// </summary>
    private ComponentSave GivenComponentInProject(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });

        GumProjectSave project = new GumProjectSave { FullFileName = Path.Combine(_codeProjectRoot, "Test.gumx") };
        project.Components.Add(component);
        ObjectFinder.Self.GumProjectSave = project;

        return component;
    }

    private CodeOutputProjectSettings GivenProjectSettings(string rootNamespace, bool appendFolderToNamespace)
    {
        return new CodeOutputProjectSettings
        {
            CodeProjectRoot = _codeProjectRoot + Path.DirectorySeparatorChar,
            RootNamespace = rootNamespace,
            AppendFolderToNamespace = appendFolderToNamespace,
            // MonoGameForms names a component's class after the element, with no "Runtime" suffix,
            // so the file names below read the same as the element names.
            OutputLibrary = OutputLibrary.MonoGameForms
        };
    }

    /// <summary>
    /// Writes a file under the temp code project root. The relative path is given with forward
    /// slashes and split here - a backslash is a legal file name character on macOS and Linux, so a
    /// "Components\MyComponent.cs" literal would create one oddly named file in the root rather than
    /// the nested file the test means.
    /// </summary>
    private void GivenCustomCodeFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(_codeProjectRoot, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
    }

    [Fact]
    public void HandleRename_WithEmptyCodeProjectRoot_DoesNothing()
    {
        ComponentSave element = GivenComponentInProject("MyComponent");

        _renameService.HandleRename(
            element,
            oldName: "OldName",
            codeOutputProjectSettings: new CodeOutputProjectSettings(), // CodeProjectRoot defaults to empty
            visualApi: VisualApi.Gum);

        _dialogService.VerifyNoOtherCalls();
    }

    [Fact]
    public void HandleRename_WhenElementMovedToFolder_MovesCustomFileAndUpdatesNamespaceToMatchGeneratedFile()
    {
        GivenCustomCodeFile("Components/MyComponent.cs",
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent\r\n" +
            "    {\r\n" +
            "        partial void CustomInitialize()\r\n" +
            "        {\r\n" +
            "            HandWrittenMarker();\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n");
        GivenCustomCodeFile("Components/MyComponent.Generated.cs", "//stale generated file");

        // The tool renames the element (folder moves arrive as a rename to "Folder/Name") before
        // notifying the rename service, so the element already carries its new identity here.
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);

        _renameService.HandleRename(element, oldName: "MyComponent", projectSettings, VisualApi.Gum);

        // HandleRename funnels every failure into an error dialog, so a quiet run is part of the assertion.
        _dialogService.Verify(x => x.ShowMessage(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<MessageDialogStyle?>()), Times.Never);

        File.Exists(Path.Combine(_codeProjectRoot, "Components", "MyComponent.cs")).ShouldBeFalse();
        File.Exists(Path.Combine(_codeProjectRoot, "Components", "MyComponent.Generated.cs")).ShouldBeFalse();

        string movedCustomCode = File.ReadAllText(Path.Combine(_codeProjectRoot, "Components", "Widgets", "MyComponent.cs"));
        movedCustomCode.ShouldContain("namespace MyGame.Components.Widgets");
        movedCustomCode.ShouldNotContain("namespace MyGame.Components\r");
        movedCustomCode.ShouldContain("partial class MyComponent");
        movedCustomCode.ShouldContain("HandWrittenMarker();");

        // The whole point of the namespace rewrite: the two partials must still declare the same type.
        string regeneratedCode = File.ReadAllText(Path.Combine(_codeProjectRoot, "Components", "Widgets", "MyComponent.Generated.cs"));
        regeneratedCode.ShouldContain("namespace MyGame.Components.Widgets");
    }

    [Fact]
    public void HandleRename_WhenElementRenamedInPlace_RenamesClassAndKeepsNamespace()
    {
        GivenCustomCodeFile("Components/OldName.cs",
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class OldName\r\n" +
            "    {\r\n" +
            "        partial void CustomInitialize()\r\n" +
            "        {\r\n" +
            "            HandWrittenMarker();\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n");

        ComponentSave element = GivenComponentInProject("NewName");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);

        _renameService.HandleRename(element, oldName: "OldName", projectSettings, VisualApi.Gum);

        File.Exists(Path.Combine(_codeProjectRoot, "Components", "OldName.cs")).ShouldBeFalse();

        string movedCustomCode = File.ReadAllText(Path.Combine(_codeProjectRoot, "Components", "NewName.cs"));
        movedCustomCode.ShouldContain("namespace MyGame.Components");
        movedCustomCode.ShouldContain("partial class NewName");
        movedCustomCode.ShouldNotContain("OldName");
        movedCustomCode.ShouldContain("HandWrittenMarker();");
    }

    [Fact]
    public void HandleRename_WhenElementMovedOutOfFolderToRoot_UpdatesNamespace()
    {
        GivenCustomCodeFile("Components/Widgets/MyComponent.cs",
            "namespace MyGame.Components.Widgets\r\n" +
            "{\r\n" +
            "    partial class MyComponent\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n");

        ComponentSave element = GivenComponentInProject("MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);

        _renameService.HandleRename(element, oldName: "Widgets/MyComponent", projectSettings, VisualApi.Gum);

        File.Exists(Path.Combine(_codeProjectRoot, "Components", "Widgets", "MyComponent.cs")).ShouldBeFalse();

        string movedCustomCode = File.ReadAllText(Path.Combine(_codeProjectRoot, "Components", "MyComponent.cs"));
        movedCustomCode.ShouldContain("namespace MyGame.Components\r");
        movedCustomCode.ShouldNotContain("Widgets");
    }

    [Fact]
    public void HandleRename_WithAppendFolderToNamespaceOff_LeavesNamespaceUnchangedAcrossFolderMove()
    {
        GivenCustomCodeFile("Components/MyComponent.cs",
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n");

        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: false);

        _renameService.HandleRename(element, oldName: "MyComponent", projectSettings, VisualApi.Gum);

        string movedCustomCode = File.ReadAllText(Path.Combine(_codeProjectRoot, "Components", "Widgets", "MyComponent.cs"));
        movedCustomCode.ShouldContain("namespace MyGame.Components\r");
        movedCustomCode.ShouldNotContain("Widgets");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithFileScopedNamespace_UpdatesNamespaceAndKeepsSemicolon()
    {
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);
        string contents =
            "namespace MyGame.Components;\r\n" +
            "\r\n" +
            "partial class MyComponent\r\n" +
            "{\r\n" +
            "}\r\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("namespace MyGame.Components.Widgets;");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithNoRootNamespace_LeavesExistingNamespaceAlone()
    {
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        // Without a RootNamespace codegen emits no namespace at all, so there is nothing to rename the
        // user's hand-written namespace to. Blanking it would produce uncompilable code.
        CodeOutputProjectSettings projectSettings = GivenProjectSettings(rootNamespace: string.Empty, appendFolderToNamespace: true);
        string contents =
            "namespace MyHandWrittenNamespace\r\n" +
            "{\r\n" +
            "    partial class MyComponent\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("namespace MyHandWrittenNamespace");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithElementNamespaceOverride_UsesTheOverride()
    {
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);
        CodeOutputElementSettings elementSettings = new CodeOutputElementSettings { Namespace = "MyGame.Overridden" };
        string contents =
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings, projectSettings);

        updated.ShouldContain("namespace MyGame.Overridden");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithInheritanceInGeneratedCode_KeepsUserAddedBaseType()
    {
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        // InheritanceLocation defaults to InGeneratedCode, so the generated partial owns the base list
        // and anything the user added to the custom partial (an interface, say) has to survive.
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);
        string contents =
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent : IDisposable\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("partial class MyComponent : IDisposable");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithInheritanceInCustomCode_DoesNotDoubleUpTheBaseList()
    {
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);
        projectSettings.InheritanceLocation = InheritanceLocation.InCustomCode;
        string contents =
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent : SomeOldBase\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        // Codegen owns the base list here, so its version replaces the old one instead of being
        // concatenated onto it (which would emit "X : New : Old" and not compile).
        updated.ShouldNotContain("SomeOldBase");
        updated.ShouldContain("partial class MyComponent : ");
    }

    [Fact]
    public void UpdateHeadersInCustomCode_WithWindowsLineEndings_DoesNotStripTheCarriageReturn()
    {
        ComponentSave element = GivenComponentInProject("Widgets/MyComponent");
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);
        string contents =
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent\r\n" +
            "    {\r\n" +
            "    }\r\n" +
            "}\r\n";

        string updated = _renameService.UpdateHeadersInCustomCode(contents, element, elementSettings: null, projectSettings);

        updated.ShouldContain("partial class MyComponent\r\n");
    }

    [Fact]
    public void HandleVariableSet_WhenBaseTypeChangedWithInheritanceInCustomCode_RewritesTheBaseType()
    {
        GivenCustomCodeFile("Components/MyComponent.cs",
            "namespace MyGame.Components\r\n" +
            "{\r\n" +
            "    partial class MyComponent : Container\r\n" +
            "    {\r\n" +
            "        partial void CustomInitialize()\r\n" +
            "        {\r\n" +
            "            HandWrittenMarker();\r\n" +
            "        }\r\n" +
            "    }\r\n" +
            "}\r\n");

        ComponentSave element = GivenComponentInProject("MyComponent");
        element.BaseType = "Rectangle";
        CodeOutputProjectSettings projectSettings = GivenProjectSettings("MyGame", appendFolderToNamespace: true);
        projectSettings.InheritanceLocation = InheritanceLocation.InCustomCode;

        _renameService.HandleVariableSet(element, instance: null, "BaseType", oldValue: "Container", projectSettings);

        string customCode = File.ReadAllText(Path.Combine(_codeProjectRoot, "Components", "MyComponent.cs"));
        customCode.ShouldContain("HandWrittenMarker();");
        customCode.ShouldNotContain(": Container");
    }
}
