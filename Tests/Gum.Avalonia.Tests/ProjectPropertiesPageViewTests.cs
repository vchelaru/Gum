using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.VisualTree;
using Gum.Avalonia.Settings;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>Each Project page of the Settings window is the shared grid layout, limited to its own settings.</summary>
public class ProjectPropertiesPageViewTests
{
    [AvaloniaFact]
    public void CanvasPage_ShowsTheCanvasSettings_AndNotTheOthers()
    {
        ProjectPropertiesViewModel properties = new ProjectPropertiesViewModel();
        properties.SetFrom(new GumProjectSave());
        ProjectPropertiesPageView view = new ProjectPropertiesPageView
        {
            DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.Canvas),
        };
        Window window = new Window { Content = view, Width = 500, Height = 600 };
        window.Show();
        window.UpdateLayout();

        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.CanvasWidth)).ShouldNotBeNull();
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.ShowCheckerBackground)).ShouldNotBeNull();
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.TextureFilter)).ShouldBeNull();
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.LocalizationFiles)).ShouldBeNull();
        view.GetVisualDescendants().OfType<CheckBox>().Count().ShouldBe(3, "show outlines, canvas outline and checker background");
        window.Close();
    }

    [AvaloniaFact]
    public void Page_WhenRebound_ShowsTheNewSection()
    {
        ProjectPropertiesViewModel properties = new ProjectPropertiesViewModel();
        properties.SetFrom(new GumProjectSave());
        ProjectPropertiesPageView view = new ProjectPropertiesPageView
        {
            DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.Canvas),
        };
        Window window = new Window { Content = view, Width = 500, Height = 600 };
        window.Show();

        view.DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.General);
        window.UpdateLayout();

        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.CanvasWidth)).ShouldBeNull();
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.TextureFilter)).ShouldNotBeNull();
        window.Close();
    }
}
