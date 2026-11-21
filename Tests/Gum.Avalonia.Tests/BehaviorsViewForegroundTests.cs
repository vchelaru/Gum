using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.Avalonia.Panels;
using Gum.ToolStates;
using Gum.Avalonia.Tests.Harness;
using Moq;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The Behaviors tab's labels take the theme's text color, except a missing behavior, which is
/// error-colored. A label bound through a converter that returned "unset" stayed black instead.
/// </summary>
public class BehaviorsViewForegroundTests
{
    internal static BehaviorsViewModel CreateViewModel()
    {
        BehaviorsViewModel viewModel = new BehaviorsViewModel(new Mock<ISelectedState>().Object, new Mock<IProjectManager>().Object);
        viewModel.AddedBehaviors.Add(new CheckListBehaviorItem { Name = "ButtonBehavior", IsChecked = true });
        viewModel.AddedBehaviors.Add(new CheckListBehaviorItem { Name = "GoneBehavior", IsChecked = true, IsOrphaned = true });
        viewModel.AllBehaviors.Add(new CheckListBehaviorItem { Name = "ButtonBehavior", IsChecked = true });
        viewModel.AllBehaviors.Add(new CheckListBehaviorItem { Name = "GoneBehavior", IsChecked = true, IsOrphaned = true });
        viewModel.AllBehaviors.Add(new CheckListBehaviorItem { Name = "SliderBehavior" });
        return viewModel;
    }

    internal static ScreenshotWindow Host(BehaviorsViewModel viewModel, ThemeVariant theme, bool editing)
    {
        viewModel.IsEditing = editing;
        BehaviorsView view = new BehaviorsView { DataContext = viewModel };
        return new ScreenshotWindow(new Window { Content = new StackPanel { Children = { view } }, Width = 320, Height = 260 }, theme);
    }

    private static TextBlock Label(Window window, string text) =>
        window.GetVisualDescendants().OfType<TextBlock>().Where(block => block.IsEffectivelyVisible).First(block => block.Text == text);

    private static IBrush? Foreground(TextBlock block) => block.Foreground;

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Label_OfPresentBehavior_InheritsThemeForeground(bool editing)
    {
        using ScreenshotWindow shot = Host(CreateViewModel(), ThemeVariant.Dark, editing);
        // A plain label in the same kind of container is what "the theme foreground" means.
        TextBlock plain = new TextBlock { Text = "plain" };
        Control reference = editing ? new CheckBox { Content = plain } : new ListBox { Items = { plain } };
        ((Panel)shot.Window.Content!).Children.Add(reference);
        Dispatcher.UIThread.RunJobs();

        IBrush? expected = Foreground(plain);
        ((ISolidColorBrush)Foreground(Label(shot.Window, "ButtonBehavior"))!).Color.ShouldBe(((ISolidColorBrush)expected!).Color);
    }

    [AvaloniaTheory]
    [InlineData(true)]
    [InlineData(false)]
    public void Label_OfMissingBehavior_IsOrangeRedAndItalic(bool editing)
    {
        using ScreenshotWindow shot = Host(CreateViewModel(), ThemeVariant.Dark, editing);
        TextBlock label = Label(shot.Window, "GoneBehavior (missing)");
        ((ISolidColorBrush)Foreground(label)!).Color.ShouldBe(Colors.OrangeRed);
        label.FontStyle.ShouldBe(FontStyle.Italic);
    }
}
