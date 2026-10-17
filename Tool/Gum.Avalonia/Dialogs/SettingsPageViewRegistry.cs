using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Gum.Avalonia.Dialogs.Views;
using Gum.Avalonia.Panels;
using Gum.Avalonia.Settings;
using Gum.Dialogs;
using Gum.Plugins.FontsPlugin;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.Services.Dialogs;
using Gum.SettingsWindow;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// Maps the view model a <see cref="SettingsPage"/> shows to the Avalonia view that presents it, the
/// settings counterpart of <see cref="DialogViewRegistry"/> and the tab registry. A page's view fills
/// the area to the right of the Settings window's page list; a short view is scrolled by the host, a
/// tall one (a grid) scrolls itself.
/// </summary>
public class SettingsPageViewRegistry
{
    private readonly Dictionary<Type, Func<Control>> _factories;

    /// <summary>Creates the registry with every page view this head has.</summary>
    public SettingsPageViewRegistry()
    {
        _factories = new Dictionary<Type, Func<Control>>();

        Register<GeneralSettingsViewModel>(() => Scrolled(new GeneralSettingsView()));
        Register<ThemingDialogViewModel>(() => Scrolled(new ThemingDialogView()));
        Register<PluginsDialogViewModel>(() => WithDialogButtons(new PluginsDialogView()));
    }

    /// <summary>The view-model types with a registered page view.</summary>
    public IReadOnlyCollection<Type> RegisteredViewModelTypes => _factories.Keys;

    /// <summary>Registers (or replaces) the view for <typeparamref name="TViewModel"/>.</summary>
    public void Register<TViewModel>(Func<Control> factory) where TViewModel : class =>
        _factories[typeof(TViewModel)] = factory;

    /// <summary>True when <paramref name="viewModelType"/> or one of its base types has a registered view.</summary>
    public bool HasView(Type viewModelType) => FindFactory(viewModelType) != null;

    /// <summary>Creates the view for <paramref name="content"/> with its DataContext set.</summary>
    public Control CreateView(object content)
    {
        Control view = FindFactory(content.GetType())?.Invoke() ?? new TextBlock
        {
            Text = $"No Avalonia view is registered for {content.GetType().Name}.",
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
        };
        view.DataContext = content;
        return view;
    }

    private Func<Control>? FindFactory(Type viewModelType)
    {
        for (Type? type = viewModelType; type != null; type = type.BaseType)
        {
            if (_factories.TryGetValue(type, out Func<Control>? factory))
            {
                return factory;
            }
        }
        return null;
    }

    private static Control Scrolled(Control view) => new ScrollViewer
    {
        Content = view,
        HorizontalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        VerticalScrollBarVisibility = global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
    };

    // A dialog view reused as a page: it sizes itself for a dialog window and puts its extra buttons
    // on that window's button row, so drop the fixed width and place the buttons under the view.
    private static Control WithDialogButtons(Control view)
    {
        view.Width = double.NaN;
        view.HorizontalAlignment = HorizontalAlignment.Stretch;

        DockPanel host = new DockPanel();
        if (DialogWindow.GetAuxiliaryActions(view) is { } actions)
        {
            actions.HorizontalAlignment = HorizontalAlignment.Left;
            actions.Margin = new Thickness(0, 8, 0, 0);
            DockPanel.SetDock(actions, Dock.Bottom);
            host.Children.Add(actions);
        }
        host.Children.Add(view);
        return host;
    }
}
