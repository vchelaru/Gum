using CodeOutputPlugin.Manager;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// Tests for CodeFileDeleteService (issue #4422 gap 2/5): the .Generated.cs and .codsj go without a
/// prompt, the user-authored custom .cs is a separate decision that isn't even asked when the file
/// is still an untouched stub, and one delete of many elements yields one confirmation.
/// </summary>
public class CodeFileDeleteServiceTests : BaseTestClass
{
    private const string StubCustomCode = """
        namespace MyGame.Components
        {
            partial class MyComponent
            {
                partial void CustomInitialize()
                {

                }
            }
        }
        """;

    private const string EditedCustomCode = """
        namespace MyGame.Components
        {
            partial class MyComponent
            {
                partial void CustomInitialize()
                {
                    Text.Text = "Hand written";
                }
            }
        }
        """;

    private readonly string _projectDirectory;
    private readonly Mock<IFileCommands> _fileCommands = new();
    private readonly Mock<IDialogService> _dialogService = new();
    private readonly CodeOutputElementSettingsManager _elementSettingsManager;
    private readonly CodeFileDeleteService _service;

    public CodeFileDeleteServiceTests()
    {
        _projectDirectory = Path.Combine(
            Path.GetTempPath(), "GumCodeFileDeleteServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDirectory);

        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier
            .Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error))
            .Returns(true);

        CodeGenerationNameVerifier codeGenNameVerifier = new(nameVerifier.Object);
        FixedProjectDirectoryProvider directoryProvider =
            new(_projectDirectory + Path.DirectorySeparatorChar);
        _elementSettingsManager = new CodeOutputElementSettingsManager(directoryProvider);

        CodeGenerator codeGenerator = new(
            codeGenNameVerifier,
            new LocalizationService(),
            _elementSettingsManager,
            directoryProvider);

        CodeGenerationFileLocationsService fileLocationsService = new(
            codeGenerator, codeGenNameVerifier, directoryProvider);

        _service = new CodeFileDeleteService(
            fileLocationsService,
            _elementSettingsManager,
            codeGenerator,
            new CustomCodeStubDetector(),
            _fileCommands.Object,
            _dialogService.Object);
    }

    public override void Dispose()
    {
        base.Dispose();
        if (Directory.Exists(_projectDirectory))
        {
            Directory.Delete(_projectDirectory, recursive: true);
        }
    }

    [Fact]
    public void HandleConfirmDelete_DoesNotRecycleEditedCustomCode_WhenCheckboxUnchecked()
    {
        ComponentSave component = GivenComponent("MyComponent");
        FilePath customFile = GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);

        _service.HandleConfirmDelete(
            new object[] { component }, deleteEditedCustomCode: false, GivenProjectSettings());

        _fileCommands.Verify(x => x.MoveToRecycleBin(customFile), Times.Never);
    }

    [Fact]
    public void HandleConfirmDelete_NeverPrompts()
    {
        // Reconciliation must never ask a question of its own - the DeleteOptionsWindow checkbox is
        // the only place the user is consulted, so nothing here can pop a dialog mid-edit.
        ComponentSave component = GivenComponent("MyComponent");
        GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);
        GivenFile("Code/Components/MyComponent.Generated.cs", "// generated");

        CodeOutputProjectSettings projectSettings = GivenProjectSettings();
        _service.HandleDeleteOptionsWindowShow(new object[] { component }, projectSettings);
        _service.HandleConfirmDelete(new object[] { component }, deleteEditedCustomCode: true, projectSettings);
        _service.ReconcileFilesForDeletedElement(component, projectSettings);

        _dialogService.Verify(
            x => x.ShowMessage(
                It.IsAny<string>(),
                It.IsAny<string?>(),
                It.Is<MessageDialogStyle?>(style => style != null)),
            Times.Never);
    }

    [Fact]
    public void HandleConfirmDelete_RecyclesEditedCustomCode_WhenCheckboxChecked()
    {
        ComponentSave component = GivenComponent("MyComponent");
        FilePath customFile = GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);

        _service.HandleConfirmDelete(
            new object[] { component }, deleteEditedCustomCode: true, GivenProjectSettings());

        _fileCommands.Verify(x => x.MoveToRecycleBin(customFile), Times.Once);
    }

    [Fact]
    public void HandleConfirmDelete_RecyclesUntouchedStub_EvenWhenCheckboxUnchecked()
    {
        ComponentSave component = GivenComponent("MyComponent");
        FilePath customFile = GivenFile("Code/Components/MyComponent.cs", StubCustomCode);

        _service.HandleConfirmDelete(
            new object[] { component }, deleteEditedCustomCode: false, GivenProjectSettings());

        _fileCommands.Verify(x => x.MoveToRecycleBin(customFile), Times.Once);
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsNull_WhenCustomCodeIsUntouchedStub()
    {
        ComponentSave component = GivenComponent("MyComponent");
        GivenFile("Code/Components/MyComponent.cs", StubCustomCode);

        DeleteOptionCheckboxViewModel? checkbox =
            _service.HandleDeleteOptionsWindowShow(new object[] { component }, GivenProjectSettings());

        checkbox.ShouldBeNull();
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsNull_WhenElementIsNeverGenerate()
    {
        ComponentSave component = GivenComponent("MyComponent");
        GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);
        GivenElementSettingsFile(component, GenerationBehavior.NeverGenerate);

        DeleteOptionCheckboxViewModel? checkbox =
            _service.HandleDeleteOptionsWindowShow(new object[] { component }, GivenProjectSettings());

        checkbox.ShouldBeNull();
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsOneUncheckedCheckbox_WhenCustomCodeIsEdited()
    {
        ComponentSave component = GivenComponent("MyComponent");
        GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);

        DeleteOptionCheckboxViewModel? checkbox =
            _service.HandleDeleteOptionsWindowShow(new object[] { component }, GivenProjectSettings());

        checkbox.ShouldNotBeNull();
        checkbox.IsChecked.ShouldBeFalse();
        checkbox.Label.ShouldBe("Delete custom code file (contains your code)");
    }

    [Fact]
    public void HandleDeleteOptionsWindowShow_ReturnsSingleCheckbox_ForTwentyElements()
    {
        // A folder's worth of elements deleted at once must still be one confirmation, not twenty.
        object[] components = new object[20];
        for (int i = 0; i < components.Length; i++)
        {
            components[i] = GivenComponent("Component" + i);
            GivenFile($"Code/Components/Component{i}.cs", EditedCustomCode);
        }

        DeleteOptionCheckboxViewModel? checkbox =
            _service.HandleDeleteOptionsWindowShow(components, GivenProjectSettings());

        checkbox.ShouldNotBeNull();
        checkbox.Label.ShouldBe("Delete 20 custom code files (contain your code)");
    }

    [Fact]
    public void ReconcileFilesForDeletedElement_LeavesAllFilesAlone_WhenNeverGenerate()
    {
        ComponentSave component = GivenComponent("MyComponent");
        GivenFile("Code/Components/MyComponent.Generated.cs", "// generated");
        GivenElementSettingsFile(component, GenerationBehavior.NeverGenerate);

        _service.ReconcileFilesForDeletedElement(component, GivenProjectSettings());

        _fileCommands.Verify(x => x.MoveToRecycleBin(It.IsAny<FilePath>()), Times.Never);
    }

    [Fact]
    public void ReconcileFilesForDeletedElement_RecyclesGeneratedAndSettingsFiles_ButNotCustomCode()
    {
        ComponentSave component = GivenComponent("MyComponent");
        FilePath generatedFile = GivenFile("Code/Components/MyComponent.Generated.cs", "// generated");
        FilePath customFile = GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);
        FilePath settingsFile = GivenElementSettingsFile(component, GenerationBehavior.GenerateManually);

        _service.ReconcileFilesForDeletedElement(component, GivenProjectSettings());

        _fileCommands.Verify(x => x.MoveToRecycleBin(generatedFile), Times.Once);
        _fileCommands.Verify(x => x.MoveToRecycleBin(settingsFile), Times.Once);
        _fileCommands.Verify(x => x.MoveToRecycleBin(customFile), Times.Never);
    }

    private ComponentSave GivenComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private FilePath GivenElementSettingsFile(ElementSave element, GenerationBehavior behavior)
    {
        CodeOutputElementSettings settings = new CodeOutputElementSettings { GenerationBehavior = behavior };
        return GivenFile(
            $"Components/{element.Name}.codsj", JsonConvert.SerializeObject(settings));
    }

    /// <summary>
    /// Writes a file under the temp project directory. The relative path is given with forward
    /// slashes and split here - a backslash is a legal file name character on macOS and Linux, so a
    /// "Code\Components\MyComponent.cs" literal would create one oddly named file in the root.
    /// </summary>
    private FilePath GivenFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(
            new[] { _projectDirectory }.Concat(relativePath.Split('/')).ToArray());
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return new FilePath(fullPath);
    }

    private CodeOutputProjectSettings GivenProjectSettings()
    {
        return new CodeOutputProjectSettings
        {
            CodeProjectRoot = Path.Combine(_projectDirectory, "Code") + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            // MonoGameForms names a component's class after the element with no "Runtime" suffix,
            // so the file names above read the same as the element names.
            OutputLibrary = OutputLibrary.MonoGameForms
        };
    }
}
