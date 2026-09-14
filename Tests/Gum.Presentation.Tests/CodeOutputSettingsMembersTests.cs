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
using WpfDataUi;
using WpfDataUi.DataTypes;

namespace Gum.Presentation.Tests;

/// <summary>
/// The Code tab's settings rows, shared by both heads' Code views: which rows exist, and how a
/// value written through a row lands in the settings and tells the view to save or rebuild.
/// </summary>
public class CodeOutputSettingsMembersTests
{
    private readonly Mock<IProjectState> _projectState;
    private readonly CodeWindowViewModel _viewModel;
    private readonly CodeOutputSettingsMembers _sut;

    public CodeOutputSettingsMembersTests()
    {
        _projectState = new Mock<IProjectState>();
        _viewModel = new CodeWindowViewModel(
            _projectState.Object,
            Mock.Of<IFileCommands>(),
            Mock.Of<IDialogService>(),
            Mock.Of<IGuiCommands>(),
            Mock.Of<ICodeGenerationAutoSetupService>());
        _sut = new CodeOutputSettingsMembers(
            _projectState.Object,
            new SyntaxVersionDetectionService(Mock.Of<ICodeGenLogger>()),
            _viewModel);
    }

    private InstanceMember Member(string name) =>
        _sut.BuildCategories().SelectMany(category => category.Members).First(member => member.Name == name);

    [Fact]
    public void BuildCategories_ListsTheProjectRowsThenTheElementRows()
    {
        _sut.ProjectSettings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms };
        _sut.ElementSettings = new CodeOutputElementSettings();

        List<MemberCategory> categories = _sut.BuildCategories();

        categories.Select(category => category.Name).ShouldBe(new[] { "Project-Wide Code Generation", "Element Code Generation" });
        categories[0].Members.Select(member => member.Name).ShouldBe(new[]
        {
            "Code Project Root", "Output Library", "Object Instantiation Type", "Project-wide Using Statements",
            "Root Namespace", "Append Folder to Namespace", "Default Screen Base", "Syntax Version",
        });
        categories[1].Members.Select(member => member.Name).ShouldBe(new[]
        {
            "Generation Behavior", "Using Statements", "Namespace", "Generated File Name", "Localize Element",
        });
    }

    [Fact]
    public void BuildCategories_AddsTheDensityRows_ForMaui()
    {
        _sut.ProjectSettings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.Maui };

        _sut.BuildCategories()[0].Members.Select(member => member.Name).ShouldContain("Adjust Pixel Values for Density");
    }

    [Fact]
    public void CodeProjectRoot_GetsATrailingSlash_AndReportsTheChange()
    {
        _sut.ProjectSettings = new CodeOutputProjectSettings();
        int changes = 0;
        _sut.SettingsChanged += (_, _) => changes++;

        Member("Code Project Root").SetValue("Code/Project", SetPropertyCommitType.Full);

        _sut.ProjectSettings.CodeProjectRoot.ShouldBe("Code/Project\\");
        changes.ShouldBe(1);
    }

    [Fact]
    public void OutputLibrary_WritesTheLibrary_AndAsksForARebuild()
    {
        _sut.ProjectSettings = new CodeOutputProjectSettings { OutputLibrary = OutputLibrary.MonoGameForms };
        int rebuilds = 0;
        _sut.RebuildRequested += (_, _) => rebuilds++;
        InstanceMember library = Member("Output Library");

        library.SetValue("Raylib", SetPropertyCommitType.Full);

        _sut.ProjectSettings.OutputLibrary.ShouldBe(OutputLibrary.Raylib);
        library.Value.ShouldBe("Raylib");
        rebuilds.ShouldBe(1);
    }

    [Fact]
    public void GenerationBehavior_DecidesWhetherCodeCanBeGenerated()
    {
        _sut.ProjectSettings = new CodeOutputProjectSettings();
        _sut.ElementSettings = new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.NeverGenerate };
        _sut.BuildCategories();
        _viewModel.CanGenerateCode.ShouldBeFalse();

        Member("Generation Behavior").SetValue(GenerationBehavior.GenerateManually, SetPropertyCommitType.Full);
        _sut.BuildCategories();

        _viewModel.CanGenerateCode.ShouldBeTrue();
    }

    [Fact]
    public void ChooseManualSetup_HidesTheSetupPrompt_AndAsksForARebuild()
    {
        _sut.ProjectSettings = new CodeOutputProjectSettings();
        int rebuilds = 0;
        _sut.RebuildRequested += (_, _) => rebuilds++;

        _sut.ChooseManualSetup();
        _sut.BuildCategories();

        rebuilds.ShouldBe(1);
        _sut.HasClickedManualSetup.ShouldBeTrue();
        _viewModel.NeedsSetup.ShouldBeFalse();
    }

    [Fact]
    public void SyntaxVersion_IsReadOnly_AndNotApplicableWithoutSettings()
    {
        InstanceMember syntax = Member("Syntax Version");

        syntax.IsReadOnly.ShouldBeTrue();
        syntax.Value.ShouldBe("N/A");
    }
}
