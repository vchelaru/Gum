using System;
using System.IO;
using CodeOutputPlugin;
using CodeOutputPlugin.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Localization;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.CodeGeneration;
using Gum.Reflection;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Newtonsoft.Json;
using Shouldly;
using ToolsUtilities;

namespace Gum.Presentation.Tests;

/// <summary>
/// The shared Code Output plugin's part in the neutral delete dialog: it offers to delete a
/// hand-edited custom code file, and recycles it only when the user ticks the option.
/// </summary>
public class CodeOutputPluginDeleteOptionsTests : BaseTestClass
{
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
    private readonly TestCodeOutputPlugin _plugin;

    public CodeOutputPluginDeleteOptionsTests()
    {
        _projectDirectory = Path.Combine(Path.GetTempPath(), "GumCodeOutputPluginDeleteOptionsTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_projectDirectory);

        Mock<INameVerifier> nameVerifier = new();
        string whyNotValid;
        CommonValidationError error;
        nameVerifier.Setup(v => v.IsValidCSharpName(It.IsAny<string>(), out whyNotValid, out error)).Returns(true);
        Mock<IProjectState> projectState = new();
        projectState.Setup(p => p.ProjectDirectory).Returns(_projectDirectory + Path.DirectorySeparatorChar);

        _plugin = new TestCodeOutputPlugin(
            new Mock<IGuiCommands>().Object,
            new Mock<IDialogService>().Object,
            nameVerifier.Object,
            new LocalizationService(),
            projectState.Object,
            new Mock<ITypeManager>().Object,
            new Mock<IOutputManager>().Object,
            new Mock<ISelectedState>().Object,
            new Mock<IRetryService>().Object,
            new WeakReferenceMessenger(),
            _fileCommands.Object);

        // The plugin reads its project settings when a project loads.
        CodeOutputProjectSettings projectSettings = new CodeOutputProjectSettings
        {
            CodeProjectRoot = Path.Combine(_projectDirectory, "Code") + Path.DirectorySeparatorChar,
            RootNamespace = "MyGame",
            OutputLibrary = OutputLibrary.MonoGameForms,
        };
        File.WriteAllText(Path.Combine(_projectDirectory, "ProjectCodeSettings.codsj"), JsonConvert.SerializeObject(projectSettings));
        _plugin.CallProjectLoad(new GumProjectSave());
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
    public void DeleteDialog_OffersTheCustomCodeOption_AndRecyclesTheFileOnlyWhenTicked()
    {
        ComponentSave component = GivenComponent("MyComponent");
        FilePath customFile = GivenFile("Code/Components/MyComponent.cs", EditedCustomCode);
        object[] objects = { component };

        DeleteOptionsDialogViewModel declined = new DeleteOptionsDialogViewModel();
        _plugin.CallDeleteOptionsShow(declined, objects);
        declined.CheckBoxes.Count.ShouldBe(1);
        declined.CheckBoxes[0].Label.ShouldBe("Delete custom code file (contains your code)");
        declined.CheckBoxes[0].IsChecked.ShouldBeFalse();
        _plugin.CallDeleteOptionsConfirmed(declined, objects);
        _fileCommands.Verify(x => x.MoveToRecycleBin(customFile), Times.Never);

        DeleteOptionsDialogViewModel accepted = new DeleteOptionsDialogViewModel();
        _plugin.CallDeleteOptionsShow(accepted, objects);
        accepted.CheckBoxes[0].IsChecked = true;
        _plugin.CallDeleteOptionsConfirmed(accepted, objects);

        _fileCommands.Verify(x => x.MoveToRecycleBin(customFile), Times.Once);
    }

    private static ComponentSave GivenComponent(string name)
    {
        ComponentSave component = new ComponentSave { Name = name, BaseType = "Container" };
        component.States.Add(new StateSave { Name = "Default", ParentContainer = component });
        return component;
    }

    private FilePath GivenFile(string relativePath, string contents)
    {
        string fullPath = Path.Combine(_projectDirectory, relativePath.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return new FilePath(fullPath);
    }

    private sealed class TestCodeOutputPlugin : CodeOutputPluginBase
    {
        public TestCodeOutputPlugin(
            IGuiCommands guiCommands,
            IDialogService dialogService,
            INameVerifier nameVerifier,
            LocalizationService localizationService,
            IProjectState projectState,
            ITypeManager typeManager,
            IOutputManager outputManager,
            ISelectedState selectedState,
            IRetryService retryService,
            IMessenger messenger,
            IFileCommands fileCommands)
            : base(guiCommands, dialogService, nameVerifier, localizationService, projectState, typeManager,
                outputManager, selectedState, retryService, messenger, fileCommands)
        {
        }

        protected override ICodeOutputTabHost CreateTabHost(CodeWindowViewModel viewModel, CodeOutputSettingsMembers settingsMembers) =>
            throw new NotSupportedException("The tab is not built in this test.");
    }
}
