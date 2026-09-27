using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Harness;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.Errors;
using Gum.Services;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Errors tab in the head's main panel.</summary>
public class ErrorsTabTests
{
    private static IServiceProvider Services => TestAppBuilder.Services;

    [AvaloniaFact]
    public void Rows_ShowErrorsThatArrivedWhileAnotherTabWasSelected()
    {
        // The Errors plugin refreshes on selection and project load, usually while another bottom
        // tab is the one showing.
        AvaloniaTabManager tabs = ActivatorUtilities.CreateInstance<AvaloniaTabManager>(Services);
        AllErrorsViewModel viewModel = new AllErrorsViewModel(
            Services.GetRequiredService<IClipboardService>(),
            Services.GetRequiredService<IFileSystemRevealService>());
        AvaloniaPluginTab other = (AvaloniaPluginTab)tabs.AddControl(new TextBlock { Text = "Other" }, "Other", TabLocation.RightBottom);
        AvaloniaPluginTab errorsTab = (AvaloniaPluginTab)tabs.AddControl(viewModel, "Errors", TabLocation.RightBottom);
        Window window = new Window { Content = new MainPanelView(tabs), Width = 1200, Height = 800 };
        window.Show();
        TabControl region = window.GetVisualDescendants().OfType<TabControl>()
            .Single(tabControl => tabControl.Items.Contains(errorsTab));
        region.SelectedItem = other;
        window.UpdateLayout();

        viewModel.Errors.Add(new ErrorViewModel { Message = "First" });
        viewModel.Errors.Add(new ErrorViewModel { Message = "Second" });
        region.SelectedItem = errorsTab;
        window.UpdateLayout();

        ErrorsView errors = window.GetVisualDescendants().OfType<ErrorsView>().Single();
        errors.GetRealizedContainers().Count().ShouldBe(2);
        window.Close();
    }

    [AvaloniaFact]
    public void Rows_ListProjectLevelCaseMismatch_WhateverIsSelected()
    {
        // The localization file belongs to the project, not to any element (#5262).
        using ToolProjectFixture fixture = new ToolProjectFixture("GumErrorsTabProjectErrors");
        ComponentSave component = fixture.AddComponent("Button");
        File.WriteAllText(Path.Combine(fixture.ProjectFolder, "strings.csv"), "String ID,English\nT_Hi,Hi\n");
        fixture.Project.LocalizationFile = "Strings.csv";

        fixture.SaveAndReload();

        AllErrorsViewModel viewModel = GetHeadErrorsViewModel();
        viewModel.Errors.ShouldContain(error => error.Code == "GUM0008" && error.Message.Contains("Strings.csv"),
            "with nothing selected");
        fixture.SelectedState.SelectedElement = fixture.Project.Components.Single(item => item.Name == component.Name);
        viewModel.Errors.ShouldContain(error => error.Code == "GUM0008" && error.Message.Contains("Strings.csv"),
            "with an element selected");
    }

    private static AllErrorsViewModel GetHeadErrorsViewModel()
    {
        AvaloniaPluginTab tab = Services.GetRequiredService<AvaloniaTabManager>().AllTabs.Single(item => item.Title == "Errors");
        return tab.Content as AllErrorsViewModel ?? (AllErrorsViewModel)((Control)tab.Content).DataContext!;
    }
}
