using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Gum.Avalonia.Panels;
using Gum.DataTypes;
using Gum.Plugins.PropertiesWindowPlugin;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>The Project Properties tab is the shared grid layout with a Close button.</summary>
public class ProjectPropertiesViewTests
{
    [AvaloniaFact]
    public void View_ShowsTheSharedGridLayout_AndCloseAsksTheViewModel()
    {
        ProjectPropertiesViewModel viewModel = new ProjectPropertiesViewModel();
        viewModel.SetFrom(autoSave: true, new GumProjectSave());
        bool closeRequested = false;
        viewModel.CloseRequested += () => closeRequested = true;
        ProjectPropertiesView view = new ProjectPropertiesView { DataContext = viewModel };
        Window window = new Window { Content = view, Width = 500, Height = 800 };
        window.Show();
        window.UpdateLayout();

        view.Grid.Categories.Select(category => category.Name).ShouldContain("Font Generation");
        view.Grid.GetInstanceMember(nameof(ProjectPropertiesViewModel.IsUpdatingFromModel)).ShouldBeNull();
        view.GetVisualDescendants().OfType<CheckBox>().Count().ShouldBeGreaterThan(5);

        Button close = view.GetVisualDescendants().OfType<Button>().Single(button => Equals(button.Content, "Close"));
        close.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));

        closeRequested.ShouldBeTrue();
        window.Close();
    }
}
