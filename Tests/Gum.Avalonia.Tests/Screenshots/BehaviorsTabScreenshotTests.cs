using Avalonia.Controls;
using Avalonia.Styling;
using Avalonia.Threading;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Tests.Harness;
using Gum.Plugins.Behaviors;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// The Behaviors tab's normal list (left) and Edit checklist (right), with one missing behavior in
/// each, in both themes at base font sizes 9, 12, 16 and 24.
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class BehaviorsTabScreenshotTests
{
    [Fact]
    public void BehaviorsTab() => PrScreenshot.Run(() =>
    {
        foreach (ThemeVariant theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
        {
            BehaviorsViewModel list = BehaviorsViewForegroundTests.CreateViewModel();
            BehaviorsViewModel edit = BehaviorsViewForegroundTests.CreateViewModel();
            list.IsEditing = false;
            edit.IsEditing = true;
            Grid content = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*") };
            BehaviorsView listView = new BehaviorsView { DataContext = list };
            BehaviorsView editView = new BehaviorsView { DataContext = edit };
            Grid.SetColumn(editView, 1);
            content.Children.Add(listView);
            content.Children.Add(editView);
            using ScreenshotWindow window = PrScreenshot.Show(content, 640, 240, theme);
            Dispatcher.UIThread.RunJobs();
            PrScreenshot.SaveContactSheet(window.Window, $"behaviors-{theme.Key.ToString()!.ToLowerInvariant()}");
        }
    });
}
