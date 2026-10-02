using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Data;
using FluentIcons.Avalonia;
using Gum.Avalonia.Shell;

namespace Gum.Avalonia.Accessibility;

/// <summary>
/// Gives the head's controls the UI Automation name a screen reader should announce. A control
/// whose content is not a string (an icon, a panel, a view model) otherwise reports that object's
/// type name, and access-key text keeps its underscore (#5591). Installed once, before any control
/// exists, for every control of a kind rather than control by control.
/// </summary>
public static class AutomationNames
{
    private static readonly AttachedProperty<string?> DerivedNameProperty =
        AvaloniaProperty.RegisterAttached<Control, Control, string?>("DerivedName");

    private static bool _installed;

    /// <summary>Hooks buttons (name from text or tooltip) and tab items (name from the tab's title).</summary>
    public static void Install()
    {
        if (_installed)
        {
            return;
        }
        _installed = true;

        ContentControl.ContentProperty.Changed.AddClassHandler<Button>((button, _) => Refresh(button));
        ToolTip.TipProperty.Changed.AddClassHandler<Button>((button, _) => Refresh(button));
        Control.DataContextProperty.Changed.AddClassHandler<TabItem>((item, _) => BindTabName(item));
    }

    /// <summary>The text with access-key markers removed: "_Yes" is "Yes" and "__" is a literal underscore.</summary>
    public static string StripAccessKey(string text)
    {
        System.Text.StringBuilder result = new System.Text.StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '_')
            {
                if (i + 1 < text.Length && text[i + 1] == '_')
                {
                    result.Append('_');
                    i++;
                }
                continue;
            }
            result.Append(text[i]);
        }
        return result.ToString();
    }

    private static void Refresh(Button button)
    {
        string? current = AutomationProperties.GetName(button);
        string? derived = button.GetValue(DerivedNameProperty);
        if (current != null && current != derived)
        {
            // Someone named it on purpose.
            return;
        }

        string? name = button.Content is string text && !string.IsNullOrWhiteSpace(text)
            ? StripAccessKey(text)
            : ToolTip.GetTip(button) as string;
        if (string.IsNullOrWhiteSpace(name) && button.Content is FluentIcon icon)
        {
            // An icon-only button with no tooltip: the icon's own name beats its type name.
            name = Humanize(icon.Icon.ToString());
        }
        if (string.IsNullOrWhiteSpace(name))
        {
            name = null;
        }

        button.SetValue(DerivedNameProperty, name);
        if (name != null)
        {
            AutomationProperties.SetName(button, name);
        }
        else if (current != null)
        {
            button.ClearValue(AutomationProperties.NameProperty);
        }
    }

    private static string Humanize(string pascalCase) =>
        System.Text.RegularExpressions.Regex.Replace(pascalCase, "(?<=[a-z0-9])(?=[A-Z])", " ");

    private static void BindTabName(TabItem item)
    {
        if (item.DataContext is AvaloniaPluginTab && !item.IsSet(AutomationProperties.NameProperty))
        {
            item.Bind(AutomationProperties.NameProperty, new Binding(nameof(AvaloniaPluginTab.Title)));
        }
    }
}
