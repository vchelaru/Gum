using Gum.Forms.Controls;
using Gum.Wireframe;

namespace GumPreview;

/// <summary>
/// Puts a shown element's Forms control into an interactive state for an unattended capture
/// (<c>--focus</c>, <c>--type</c>), so states that only exist at runtime can be screenshotted.
/// </summary>
public static class PreviewInteraction
{
    /// <summary>
    /// Focuses the Forms control named <paramref name="focusName"/> under <paramref name="root"/>, then
    /// enters <paramref name="text"/> one character at a time as typing would, then selects all when asked. Returns why it could not,
    /// or null on success.
    /// </summary>
    public static string? Apply(GraphicalUiElement root, string focusName, string? text, bool selectAll = false)
    {
        GraphicalUiElement? visual = root.GetGraphicalUiElementByName(focusName);
        if (visual is not InteractiveGue interactive || interactive.FormsControlAsObject is not FrameworkElement control)
        {
            return $"no Forms control named '{focusName}' was found.";
        }

        if ((!string.IsNullOrEmpty(text) || selectAll) && control is not TextBoxBase)
        {
            return $"'{focusName}' is not a text box, so --type and --select-all cannot be used on it.";
        }

        control.IsFocused = true;
        if (control is TextBoxBase textBox && text != null)
        {
            foreach (char character in text)
            {
                textBox.HandleCharEntered(character);
            }
        }
        if (selectAll)
        {
            ((TextBoxBase)control).SelectAll();
        }
        return null;
    }
}
