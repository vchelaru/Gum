using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Shouldly;
using WpfDataUi;
using WpfDataUi.Controls;

namespace GumToolUnitTests.Controls;

/// <summary>
/// A grid must render each member through a <see cref="SingleDataUiContainer"/>, both with
/// WpfDataUi's own theme and with the tool's styles, whose default grid style re-templates the
/// categories. The row template used to be an implicit <c>DataTemplate</c> keyed by the member type
/// in the library's Generic.xaml; once the member types moved to DataUi.Core, WPF stopped finding it
/// there (it looks for a type-keyed template in the theme of the assembly that defines the type) and
/// rows fell back to <c>ToString()</c> text.
/// </summary>
public class DataUiGridDefaultTemplateTests
{
    private class SampleSettings
    {
        public string Name { get; set; } = "Sample";
        public bool IsEnabled { get; set; } = true;
    }

    [StaFact]
    public void Rows_RenderAsDataUiContainers_WithTheLibraryTheme()
    {
        DataUiGrid grid = new DataUiGrid { Instance = new SampleSettings() };
        // Templates only apply inside a presentation source, so the grid needs a window.
        Window window = new Window { Content = grid, Width = 400, Height = 600, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000 };
        try
        {
            window.Show();
            window.UpdateLayout();

            Descendants(grid).OfType<SingleDataUiContainer>().Count().ShouldBe(2);
        }
        finally
        {
            window.Close();
        }
    }

    [StaFact]
    public void Rows_RenderAsDataUiContainers_WithTheToolStyles()
    {
        DataUiGrid grid = new DataUiGrid { Instance = new SampleSettings() };
        Window window = new Window { Content = grid, Width = 400, Height = 600, ShowInTaskbar = false, WindowStartupLocation = WindowStartupLocation.Manual, Left = -10000, Top = -10000 };
        window.Resources.MergedDictionaries.Add(LoadToolStyles());
        try
        {
            window.Show();
            window.UpdateLayout();

            Descendants(grid).OfType<SingleDataUiContainer>().Count().ShouldBe(2);
        }
        finally
        {
            window.Close();
        }
    }

    // The tool's root style dictionary, as App.xaml merges it.
    private static ResourceDictionary LoadToolStyles()
    {
        // Registers the pack:// scheme, which only an Application normally does.
        _ = System.IO.Packaging.PackUriHelper.UriSchemePack;
        return new ResourceDictionary { Source = new Uri("pack://application:,,,/Gum;component/Themes/Frb.Styles.xaml") };
    }

    private static System.Collections.Generic.IEnumerable<DependencyObject> Descendants(DependencyObject parent)
    {
        int count = VisualTreeHelper.GetChildrenCount(parent);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(parent, i);
            yield return child;
            foreach (DependencyObject grandChild in Descendants(child))
            {
                yield return grandChild;
            }
        }
    }
}
