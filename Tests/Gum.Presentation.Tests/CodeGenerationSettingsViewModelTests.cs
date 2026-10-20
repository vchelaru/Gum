using System.Collections.Generic;
using System.Linq;
using CodeOutputPlugin;
using CodeOutputPlugin.ViewModels;
using Gum.Commands;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Moq;
using Shouldly;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>The Code generation page of the Settings window: the project-wide rows, rebuilt when a setting changes which rows exist.</summary>
public class CodeGenerationSettingsViewModelTests
{
    private readonly CodeOutputSettingsMembers _members;
    private readonly CodeGenerationSettingsViewModel _sut;

    public CodeGenerationSettingsViewModelTests()
    {
        Mock<IProjectState> projectState = new Mock<IProjectState>();
        CodeWindowViewModel viewModel = new CodeWindowViewModel(
            projectState.Object,
            Mock.Of<IFileCommands>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<ICodeGenerationAutoSetupService>());
        _members = new CodeOutputSettingsMembers(
            projectState.Object,
            new SyntaxVersionDetectionService(Mock.Of<ICodeGenLogger>()),
            viewModel);
        _members.ProjectSettings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.GumForms };
        _sut = new CodeGenerationSettingsViewModel(_members);
    }

    [Fact]
    public void BuildCategories_HasOnlyTheProjectWideCategory()
    {
        List<MemberCategory> categories = _sut.BuildCategories();

        categories.Select(category => category.Name).ShouldBe(new[] { "Project-Wide Code Generation" });
    }

    [Fact]
    public void Reload_RaisesReloaded()
    {
        int raised = 0;
        _sut.Reloaded += () => raised++;

        _sut.Reload();

        raised.ShouldBe(1);
    }

    [Fact]
    public void WhenAMemberAsksForARebuild_RaisesReloaded()
    {
        int raised = 0;
        _sut.Reloaded += () => raised++;

        // Choosing manual setup changes which rows exist.
        _members.ChooseManualSetup();

        raised.ShouldBe(1);
    }
}
