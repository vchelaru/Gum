using Gum.GueDeriving;
using Gum.Renderables;
using Raylib_cs;

namespace RaylibGum.Tests.Rendering;

/// <summary>
/// Adds a TextRuntime to the root and lays it out so a test can measure or wrap its Text.
/// </summary>
internal static class TextMeasureHelpers
{
    public static (TextRuntime Runtime, Text Text) Make(string content, float? width = null, Font? font = null)
    {
        TextRuntime runtime = new();
        Text text = (Text)runtime.RenderableComponent!;
        if (font != null)
        {
            text.Font = font.Value;
        }
        if (width != null)
        {
            runtime.WidthUnits = Gum.DataTypes.DimensionUnitType.Absolute;
            runtime.Width = width.Value;
        }
        runtime.Text = content;
        Gum.GumService.Default.Root.Children!.Add(runtime);
        Gum.GumService.Default.Root.UpdateLayout();
        return (runtime, text);
    }

    public static void Remove(TextRuntime runtime) => Gum.GumService.Default.Root.Children!.Remove(runtime);
}
