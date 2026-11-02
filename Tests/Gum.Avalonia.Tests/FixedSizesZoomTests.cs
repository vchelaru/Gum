using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;
using AvaloniaDataUi;
using AvaloniaDataUi.Controls;
using FluentIcons.Avalonia;
using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Plugins.StateAnimation;
using Gum.Avalonia.Plugins.VariableGrid;
using Gum.Avalonia.Shell;
using Gum.Avalonia.Tests.Animations;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;
using Gum.Services;
using Gum.Services.Dialogs;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;

namespace Gum.Avalonia.Tests;

/// <summary>
/// The fixed-pixel chrome #7343 left behind (filter magnifier, group chevron, Animations view, timeline rows,
/// color picker, color swatch, dialog caption bar) follows the base font size like the text beside it, and is
/// unchanged at the default 12 (#7371).
/// </summary>
public class FixedSizesZoomTests
{
    [AvaloniaFact]
    public void CompactColorPicker_SizesFollowTheBaseFontSize()
    {
        CompactColorPicker picker = new CompactColorPicker();
        AssertFollowsZoom(picker, () => ColorPickerSizes(picker), expectedAtDefault: new List<double>
        {
            200, 140, 14, 14, 14, 14, 40, 22, 22, 40, 22, 22, 40, 22, 22, 32, 16, 32, 16,
        });
    }

    [AvaloniaFact]
    public void ColorDisplay_SwatchAndHexBoxFollowTheBaseFontSize()
    {
        ColorDisplay display = new ColorDisplay();
        AssertFollowsZoom(display, () => display.GetVisualDescendants().OfType<Border>()
                .Where(border => border.CornerRadius == new CornerRadius(2) && !double.IsNaN(border.Height)).Select(border => border.Height)
                .Concat(display.GetVisualDescendants().OfType<EditTrackingTextBox>().Select(box => box.Width)).ToList(),
            expectedAtDefault: new List<double> { 18, 68 });
    }

    [AvaloniaFact]
    public void DialogWindow_CaptionBarAndButtonsFollowTheBaseFontSize()
    {
        DeleteOptionsDialogViewModel viewModel = new DeleteOptionsDialogViewModel { Message = "Delete?" };
        DialogWindow dialog = new DialogWindow(viewModel, new TextBlock { Text = "Body" });
        AssertFollowsZoom(dialog, () => new List<double>
        {
            ((Border)dialog.GetVisualDescendants().OfType<TextBlock>().First(text => text.Name == DialogWindow.CaptionName).Parent!).Height,
            dialog.GetVisualDescendants().OfType<Button>().First().MinWidth,
        }, expectedAtDefault: new List<double> { 24, 64 });
    }

    [AvaloniaFact]
    public void AnimationsView_HeadersRowsAndButtonsFollowTheBaseFontSize()
    {
        using AnimationEditorHarness editor = new AnimationEditorHarness();
        ComponentSave button = editor.AddComponent("Button", "AnimationCategory", "Pressed", "Released");
        editor.Select(button);
        editor.AddAnimation("Walk", loops: true);
        editor.AddStateKeyframe("AnimationCategory/Pressed");
        editor.AddStateKeyframe("AnimationCategory/Released");

        AssertFollowsZoom(editor.Window, () => AnimationSizes(editor), expectedAtDefault: new List<double>
        {
            28, 28, 28, 72, 48, 64, 26, 14, 22,
        });
    }

    [AvaloniaFact]
    public void VariablesTab_FilterMagnifierAndGroupChevronFollowTheBaseFontSize()
    {
        using VariableGridHarness grid = new VariableGridHarness();
        ComponentSave card = grid.Project.AddComponent("Card");
        InstanceSave sprite = grid.Project.AddInstance(card, "Silhouette", "Sprite");
        grid.Select(sprite);
        grid.Settle();
        Window window = grid.Input.Window;

        AssertFollowsZoom(window, () =>
        {
            global::Avalonia.Controls.Shapes.Path chevron = window.GetVisualDescendants().OfType<DataUiCategoryView>().First()
                .GetVisualDescendants().OfType<global::Avalonia.Controls.Shapes.Path>().First();
            return new List<double>
            {
                window.GetVisualDescendants().OfType<FluentIcon>().First(icon => icon.Icon == FluentIcons.Common.Icon.Search).Width,
                chevron.Width,
                chevron.Height,
            };
        }, expectedAtDefault: new List<double> { 16, 9, 9 });
    }

    [AvaloniaFact]
    public void ContextMenuIcons_FollowTheBaseFontSize()
    {
        Control item = AvaloniaContextMenus.ToMenuItem(new Gum.ViewModels.ContextMenuItemViewModel { Text = "Sprite", IconKey = "Sprite_Instance.png" }, 16);
        Control icon = (Control)((MenuItem)item).Icon!;
        AssertFollowsZoom(new ContentControl { Content = item }, () => new List<double> { icon.Width, icon.Height },
            expectedAtDefault: new List<double> { 16, 16 });
    }

    private static List<double> ColorPickerSizes(CompactColorPicker picker)
    {
        List<double> sizes = new List<double>
        {
            ((Control)picker.Content!).Width,
            picker.GetVisualDescendants().OfType<ColorSpectrum>().First().Height,
        };
        sizes.AddRange(picker.GetVisualDescendants().OfType<ColorSlider>().Select(slider => slider.Height));
        foreach (EditTrackingTextBox box in picker.GetVisualDescendants().OfType<EditTrackingTextBox>())
        {
            sizes.AddRange(new[] { box.Width, box.Height, box.MinHeight });
        }
        foreach (Border swatch in picker.GetVisualDescendants().OfType<Border>().Where(border => ToolTip.GetTip(border) is string))
        {
            sizes.AddRange(new[] { swatch.Width, swatch.Height });
        }
        return sizes;
    }

    // The three column headers' MinHeight, then the play button, speed text, time box, loop toggle,
    // keyframe glyph and a timeline row.
    private static List<double> AnimationSizes(AnimationEditorHarness editor)
    {
        Window window = editor.Window;
        List<double> sizes = window.GetVisualDescendants().OfType<Grid>()
            .Where(grid => grid.ColumnDefinitions.Count == 2 && grid.MinHeight > 0).Select(grid => grid.MinHeight).ToList();
        sizes.Add(editor.PlayButton.MinWidth);
        sizes.Add(window.GetVisualDescendants().OfType<TextBlock>().First(text => text.TextAlignment == TextAlignment.Center && text.MinWidth > 0).MinWidth);
        sizes.Add(editor.Timeline.GetVisualDescendants().OfType<TextBox>().First().MinWidth);
        sizes.Add(window.GetVisualDescendants().OfType<ToggleButton>().First(toggle => toggle != editor.PlayButton && toggle.MinWidth > 0).MinWidth);
        sizes.Add(editor.Window.GetVisualDescendants().OfType<TextBlock>().First(text => text.Parent is StackPanel && (text.MinWidth == 14 || text.MinWidth == 7 || text.MinWidth == 28)).MinWidth);
        sizes.Add(editor.Timeline.GetVisualDescendants().OfType<TimelineTrack>().First().Height);
        return sizes;
    }

    private static void AssertFollowsZoom(Control content, Func<List<double>> sizes, List<double> expectedAtDefault)
    {
        // Its own window: the container's one MainWindow must not be left shown. A harness window or a
        // dialog is its own window and is closed by its owner or here.
        Window window = content as Window ?? new Window { Content = content, Width = 600, Height = 400 };
        bool closeWindow = content is not Window || content is DialogWindow;
        IAppScaleProvider scale = TestAppBuilder.Services.GetRequiredService<IAppScaleProvider>();
        double originalSize = scale.BaseFontSize;
        using IDisposable follower = AppWideWindowInput.FollowBaseFontSize(window, scale);
        try
        {
            // A harness window is already shown by its driver.
            if (content is not Window || content is DialogWindow)
            {
                window.Show();
            }
            scale.BaseFontSize = 12;
            if (content is not Window || content is DialogWindow)
            {
                ScreenshotWindow.Pump(window);
            }
            List<double> atDefault = sizes();
            atDefault.ShouldBe(expectedAtDefault);

            scale.BaseFontSize = 6;
            if (content is not Window || content is DialogWindow)
            {
                ScreenshotWindow.Pump(window);
            }
            sizes().ShouldBe(atDefault.Select(size => size * 0.5).ToList());

            scale.BaseFontSize = 24;
            if (content is not Window || content is DialogWindow)
            {
                ScreenshotWindow.Pump(window);
            }
            sizes().ShouldBe(atDefault.Select(size => size * 2).ToList());
        }
        finally
        {
            scale.BaseFontSize = originalSize;
            if (closeWindow)
            {
                window.Close();
            }
        }
    }
}
