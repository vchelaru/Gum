using Avalonia.Controls;
using Avalonia.Styling;
using Gum.Avalonia.Plugins.EditorTab;
using Gum.Avalonia.Tests.Harness;
using Xunit;

namespace Gum.Avalonia.Tests;

[Trait("Category", PrScreenshot.Category)]
public class GridSnapNoteScreenshotTests
{
    private class NoteContext
    {
        public bool HasGridSnapWarning => true;
        public string GridSnapWarningText => "Snap to Grid: one or more selected objects use non-pixel units and won't fully snap";
    }

    [SkippableFact]
    public void NoteDark() => PrScreenshot.Run(() =>
    {
        TextBlock note = AvaloniaEditorTabPlugin.CreateGridSnapNote();
        note.DataContext = new NoteContext();
        using ScreenshotWindow window = PrScreenshot.Show(note, 700, 30, ThemeVariant.Dark);
        window.Save("grid-snap-note-dark");
    });

    [SkippableFact]
    public void NoteLight() => PrScreenshot.Run(() =>
    {
        TextBlock note = AvaloniaEditorTabPlugin.CreateGridSnapNote();
        note.DataContext = new NoteContext();
        using ScreenshotWindow window = PrScreenshot.Show(note, 700, 30, ThemeVariant.Light);
        window.Save("grid-snap-note-light");
    });
}
