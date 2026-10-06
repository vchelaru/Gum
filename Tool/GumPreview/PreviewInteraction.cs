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
    /// enters <paramref name="text"/> one character at a time as typing would. Returns why it could not,
    /// or null on success.
    /// </summary>
    public static string? Apply(GraphicalUiElement root, string focusName, string? text)
    {
        GraphicalUiElement? visual = root.GetGraphicalUiElementByName(focusName);
        if (visual is not InteractiveGue interactive || interactive.FormsControlAsObject is not FrameworkElement control)
        {
            return $"no Forms control named '{focusName}' was found.";
        }

        if (!string.IsNullOrEmpty(text) && control is not TextBoxBase)
        {
            return $"'{focusName}' is not a text box, so --type cannot enter text into it.";
        }

        control.IsFocused = true;
        if (control is TextBoxBase textBox && text != null)
        {
            foreach (char character in text)
            {
                textBox.HandleCharEntered(character);
            }
        }
        return null;
    }
}
