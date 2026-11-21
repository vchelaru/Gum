using System.Reflection;
using Avalonia.Controls;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.Avalonia.Tests.Harness;
using Shouldly;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Tests;

/// <summary>The keyframe glyph is error-colored for a missing reference and takes the theme color otherwise.</summary>
public class AnimationKeyframeGlyphForegroundTests
{
    private static TextBlock Glyph(string stateName, ThemeVariant theme, out ScreenshotWindow shot, out TextBlock plain)
    {
        MethodInfo m = typeof(AnimationsView).GetMethod("CreateKeyframeRow", BindingFlags.NonPublic | BindingFlags.Static)!;
        Control row = (Control)m.Invoke(null, null)!;
        row.DataContext = new AnimatedKeyframeViewModel { StateName = stateName };
        plain = new TextBlock { Text = "plain" };
        shot = new ScreenshotWindow(new Window { Content = new StackPanel { Children = { row, plain } }, Width = 320, Height = 100 }, theme);
        Dispatcher.UIThread.RunJobs();
        return row.GetVisualDescendants().OfType<TextBlock>().First(b => b.MinWidth >= 14);
    }

    [AvaloniaFact]
    public void Glyph_Missing_IsOrangeRed()
    {
        TextBlock g = Glyph("NoSuchState", ThemeVariant.Dark, out var shot, out _);
        using (shot) ((ISolidColorBrush)g.Foreground!).Color.ShouldBe(Colors.OrangeRed);
    }

    [AvaloniaFact]
    public void Glyph_NotMissing_IsThemeColor()
    {
        TextBlock g = Glyph("", ThemeVariant.Dark, out var shot, out var plain);
        using (shot) ((ISolidColorBrush)g.Foreground!).Color.ShouldBe(((ISolidColorBrush)plain.Foreground!).Color);
    }
}
