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
        view.GetVisualDescendants().OfType<CheckBox>().Count().ShouldBeGreaterThanOrEqualTo(3, "show outlines, canvas outline and checker background");
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

    [AvaloniaFact]
    public void GeneralPage_CustomBoundsToggle_ShowsTheFourFieldsOnlyWhileOn()
    {
        GumProjectSave project = new GumProjectSave();
        ProjectPropertiesViewModel properties = new ProjectPropertiesViewModel();
        properties.SetFrom(project);
        ProjectPropertiesPageView view = new ProjectPropertiesPageView
        {
            DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.General),
        };
        Window window = new Window { Content = view, Width = 500, Height = 600 };
        window.Show();
        window.UpdateLayout();
        string left = nameof(ProjectPropertiesViewModel.SinglePixelTextureLeft);
        view.Grid.GetInstanceMember(left).ShouldBeNull();

        view.Grid.GetInstanceMember("UseCustomBounds")!.Value = true;
        window.UpdateLayout();

        view.Grid.GetInstanceMember(left).ShouldNotBeNull();
        properties.SinglePixelTextureRight.ShouldBe(1);

        view.Grid.GetInstanceMember("UseCustomBounds")!.Value = false;
        window.UpdateLayout();

        view.Grid.GetInstanceMember(left).ShouldBeNull();
        properties.SinglePixelTextureRight.ShouldBeNull();
        window.Close();
    }

    [AvaloniaFact]
    public void CanvasPage_IsBuiltFromRows_WithAHintUnderEveryRow_AndWritesTheProject()
    {
        GumProjectSave project = new GumProjectSave();
        ProjectPropertiesViewModel properties = new ProjectPropertiesViewModel();
        properties.SetFrom(project);
        ProjectPropertiesPageView view = new ProjectPropertiesPageView
        {
            DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.Canvas),
        };
        Window window = new Window { Content = view, Width = 500, Height = 600 };
        window.Show();
        window.UpdateLayout();

        view.Grid.Categories.Select(category => category.Name).ShouldBe(new[] { "Canvas size", "Guides" });
        view.Grid.Categories.SelectMany(category => category.Members).ShouldAllBe(member => !string.IsNullOrEmpty(member.DetailText) && member.IsSpaciousRow);

        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.CanvasWidth))!.Value = 1280;
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.ShowCheckerBackground))!.Value = false;
        properties.ApplyToModelObjects();

        project.DefaultCanvasWidth.ShouldBe(1280);
        project.ShowCheckerBackground.ShouldBeFalse();
        window.Close();
    }

    [AvaloniaFact]
    public void LocalizationPage_IsBuiltFromRows_AndEditsReachTheProject()
    {
        GumProjectSave project = new GumProjectSave();
        ProjectPropertiesViewModel properties = new ProjectPropertiesViewModel();
        properties.SetFrom(project);
        properties.AvailableLanguages = new[] { "English", "French" };
        ProjectPropertiesPageView view = new ProjectPropertiesPageView
        {
            DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.Localization),
        };
        Window window = new Window { Content = view, Width = 500, Height = 600 };
        window.Show();
        window.UpdateLayout();

        view.Grid.Categories.Select(category => category.Name).ShouldBe(new[] { "Files", "Language" });
        view.Grid.Categories.SelectMany(category => category.Members).ShouldAllBe(member => !string.IsNullOrEmpty(member.DetailText) && member.IsSpaciousRow);
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.CanvasWidth)).ShouldBeNull();

        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.LocalizationFiles))!.Value = new List<string> { "Strings.csv" };
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.LanguageName))!.Value = "French";
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.ShowLocalization))!.Value = false;
        properties.ApplyToModelObjects();

        project.LocalizationFiles.ShouldBe(new[] { "Strings.csv" });
        properties.LanguageName.ShouldBe("French");
        project.ShowLocalizationInGum.ShouldBeFalse();
        window.Close();
    }

    [AvaloniaFact]
    public void LocalizationPage_WhenLanguagesLoad_AddsTheLanguageRow()
    {
        ProjectPropertiesViewModel properties = new ProjectPropertiesViewModel();
        properties.SetFrom(new GumProjectSave());
        ProjectPropertiesPageView view = new ProjectPropertiesPageView
        {
            DataContext = new ProjectPropertiesPageViewModel(properties, ProjectPropertiesSection.Localization),
        };
        Window window = new Window { Content = view, Width = 500, Height = 600 };
        window.Show();
        window.UpdateLayout();
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.LanguageName)).ShouldBeNull();

        properties.AvailableLanguages = new[] { "English", "French" };
        properties.NotifyReloaded();
        window.UpdateLayout();

        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.LanguageName)).ShouldNotBeNull();
        window.Close();
    }
}
