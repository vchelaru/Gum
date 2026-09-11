using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using CodeOutputPlugin;
using CodeOutputPlugin.Manager;
using CodeOutputPlugin.ViewModels;
using Gum.Avalonia.Plugins.CodeOutput;
using Gum.Commands;
using Gum.Managers;
using Gum.Plugins;
using Gum.ProjectServices.CodeGeneration;
using Gum.Services.Dialogs;
using Gum.ToolStates;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Code Output plugin under the Avalonia head: the shared plugin loads with this head's Code
/// view, and the view shows the shared settings rows and raises Generate.
/// </summary>
public class CodeOutputTabTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void PluginManager_LoadsTheSharedCodeOutputPlugin()
    {
        PluginManager pluginManager = Services.GetRequiredService<PluginManager>();
        if (!pluginManager.IsInitialized)
        {
            pluginManager.Initialize();
        }

        pluginManager.Plugins.Select(plugin => plugin.GetType()).ShouldContain(typeof(MainCodeOutputPlugin));
    }

    [AvaloniaFact]
    public void CodeOutputView_ShowsTheSettingsRows_RaisesGenerate_AndShowsTheCode()
    {
        IProjectState projectState = Services.GetRequiredService<IProjectState>();
        CodeWindowViewModel viewModel = new CodeWindowViewModel(
            projectState,
            Services.GetRequiredService<IFileCommands>(),
            Services.GetRequiredService<IDialogService>(),
            Services.GetRequiredService<IGuiCommands>(),
            new CodeGenerationAutoSetupService());
        CodeOutputSettingsMembers members = new CodeOutputSettingsMembers(
            projectState,
            new SyntaxVersionDetectionService(new ToolCodeGenLogger(Services.GetRequiredService<IOutputManager>())),
            viewModel);
        CodeOutputView view = new CodeOutputView(viewModel, members);
        ICodeOutputTabHost host = view;
        host.CodeOutputProjectSettings = new CodeOutputProjectSettings { CodeProjectRoot = "Code\\" };
        host.CodeOutputElementSettings = new CodeOutputElementSettings { GenerationBehavior = GenerationBehavior.GenerateManually };
        Window window = new Window { Content = view, Width = 900, Height = 700 };
        window.Show();
        window.UpdateLayout();
        int generates = 0;
        host.GenerateCodeClicked += (_, _) => generates++;

        viewModel.NeedsSetup.ShouldBeFalse();
        view.SettingsGrid.IsVisible.ShouldBeTrue();
        view.SettingsGrid.Categories.SelectMany(category => category.Members).Select(member => member.Name).ShouldContain("Output Library");
        view.GenerateButton.IsEffectivelyVisible.ShouldBeTrue();
        view.GenerateButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
        generates.ShouldBe(1);

        viewModel.Code = "class Sample {}";
        view.CodeTextBox.Text.ShouldBe("class Sample {}");
        window.Close();
    }
}
