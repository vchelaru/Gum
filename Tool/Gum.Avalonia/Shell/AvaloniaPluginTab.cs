using System;
using Gum.Mvvm;
using Gum.Plugins;

namespace Gum.Avalonia.Shell;

/// <summary>
/// One tab in the shell. <see cref="Content"/> is whatever the plugin handed to
/// <see cref="ITabManager.AddControl"/>: an Avalonia control is shown as is; anything else is
/// shown through the head's data templates (a ViewModel gets its view).
/// </summary>
public class AvaloniaPluginTab : ViewModel, IPluginTab, ITabDockingCandidate, ITabAutoSelectCandidate
{
    /// <summary>Creates a hidden-by-default tab; the manager makes it visible when added.</summary>
    public AvaloniaPluginTab(object content)
    {
        Content = content;
        Title = "";
        Location = TabLocation.CenterBottom;
        IsVisible = true;
        IsSelected = false;
        CanClose = false;
    }

    /// <summary>The tab's content, a control or a ViewModel.</summary>
    public object Content { get; }

    /// <summary>A custom header shown in place of <see cref="Title"/>, or null for the plain title.</summary>
    public global::Avalonia.Controls.Control? HeaderContent { get; init; }

    /// <inheritdoc/>
    public event Action? TabShown;

    /// <inheritdoc/>
    public event Action? TabHidden;

    /// <inheritdoc/>
    public event Action? GotFocus;

    /// <inheritdoc/>
    public string Title { get => Get<string>(); set => Set(value); }

    /// <inheritdoc/>
    public TabLocation Location { get => Get<TabLocation>(); set => Set(value); }

    /// <inheritdoc/>
    public bool IsVisible
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                if (value)
                {
                    TabShown?.Invoke();
                }
                else
                {
                    TabHidden?.Invoke();
                }
            }
        }
    }

    /// <inheritdoc/>
    public bool IsSelected
    {
        get => Get<bool>();
        set
        {
            if (Set(value) && value)
            {
                GotFocus?.Invoke();
            }
        }
    }

    /// <inheritdoc/>
    public bool CanClose { get => Get<bool>(); set => Set(value); }

    /// <inheritdoc/>
    public void Show() => IsVisible = true;

    /// <inheritdoc/>
    public void Hide() => IsVisible = false;
}
