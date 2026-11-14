using Avalonia.Styling;
using Gum.Avalonia.Tests.Harness;
using Gum.Avalonia.Tests.VariableGrid;
using Gum.DataTypes;

namespace Gum.Avalonia.Tests.Screenshots;

/// <summary>
/// A VariableReferences line whose right side reads something the project does not have (#8022): the
/// textbox with the bad line typed, and the message that appears when it is applied. One contact sheet (four
/// zoom levels) per state and theme (<c>GUM_SCREENSHOT_DIR=&lt;folder&gt; dotnet test --filter ReferenceErrorDialogScreenshotTests</c>).
/// </summary>
[Trait("Category", PrScreenshot.Category)]
public class ReferenceErrorDialogScreenshotTests
{
    private static readonly ThemeVariant[] Themes = { ThemeVariant.Dark, ThemeVariant.Light };

    private static string Suffix(ThemeVariant theme) => theme.ToString().ToLowerInvariant();

    private static InstanceSave SelectLabel(VariableGridHarness grid)
    {
        ComponentSave button = grid.Project.AddComponent("Button");
        grid.Project.AddInstance(button, "Slider", "Container");
        InstanceSave label = grid.Project.AddInstance(button, "Label", "Text");
        grid.Select(label);
        grid.Input.TypeInto(grid.View.FilterTextBox, "VariableReferences");
        return label;
    }

    [SkippableFact]
    public void BadLineTypedInTheTextbox() => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        SelectLabel(grid);
        grid.TypeLines("VariableReferences", "Text = Typo");

        foreach (ThemeVariant theme in Themes)
        {
            grid.Input.Window.RequestedThemeVariant = theme;
            PrScreenshot.SaveContactSheet(grid.Input.Window, $"reference-textbox-bad-line-{Suffix(theme)}");
        }
    });

    [SkippableFact]
    public void UnknownNameDialog() => ShowDialogFor("Text = Typo", "reference-dialog-unknown-name");

    [SkippableFact]
    public void UnknownElementDialog() => ShowDialogFor("Width = Components/Nope.Width", "reference-dialog-unknown-element");

    private static void ShowDialogFor(string line, string name) => PrScreenshot.Run(() =>
    {
        using VariableGridHarness grid = new VariableGridHarness();
        SelectLabel(grid);
        grid.TypeLines("VariableReferences", line);
        grid.Dialogs.AnswerNextMessageInWindow(dialog =>
        {
            foreach (ThemeVariant theme in Themes)
            {
                dialog.Window.RequestedThemeVariant = theme;
                PrScreenshot.SaveContactSheet(dialog.Window, $"{name}-{Suffix(theme)}");
            }
            dialog.Click(dialog.AffirmativeButton);
        });
        grid.Input.Press(global::Avalonia.Input.Key.Enter, global::Avalonia.Input.PhysicalKey.Enter, global::Avalonia.Input.RawInputModifiers.Control);
    });
}
