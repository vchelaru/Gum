using Gum.Forms.DefaultVisuals.V3;
using Gum.GueDeriving;

namespace Gum.Themes.Editor;

/// <summary>
/// The Editor look shared by <see cref="TextBoxVisual"/> and <see cref="PasswordBoxVisual"/>: recessed
/// well, muted placeholder, and a hover/focus outline.
/// </summary>
internal static class EditorTextInputDecoration
{
    public static void Apply(TextBoxBaseVisual host)
    {
        var outline = new RectangleRuntime();
        host.AddChild(outline);
        outline.Dock(Gum.Wireframe.Dock.Fill);

        host.FocusedIndicator.Parent = null;

        host.PlaceholderColor = EditorStyling.ActiveStyle.Colors.TextMuted;
        host.ForegroundColor = EditorStyling.ActiveStyle.Colors.TextPrimary;
        host.BackgroundColor = EditorStyling.ActiveStyle.Colors.RecessedBackground;
        host.SelectionBackgroundColor = EditorStyling.ActiveStyle.Colors.Selection;
        host.SelectionInstance.Blend = Gum.RenderingLibrary.Blend.Additive;
        host.CaretColor = EditorStyling.ActiveStyle.Colors.Accent;

        var selectionParent = host.SelectionInstance.Parent!;
        selectionParent.Children.Move(selectionParent.Children.IndexOf(host.SelectionInstance), selectionParent.Children.Count - 1);

        host.States.Enabled.Apply += () =>
        {
            outline.Visible = false;
        };

        host.States.Disabled.Apply += () =>
        {
            outline.Visible = false;
        };

        host.States.Highlighted.Apply += () =>
        {
            outline.StrokeColor = EditorStyling.ActiveStyle.Colors.BorderHover;
            outline.Visible = true;
        };

        host.States.Focused.Apply += () =>
        {
            outline.StrokeColor = EditorStyling.ActiveStyle.Colors.Accent;
            outline.Visible = true;
        };
    }
}
