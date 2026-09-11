using System;
using System.Collections.Generic;
using System.Windows;
using Gum.Managers;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.Behaviors;
using Gum.Plugins.Errors;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Plugins.InternalPlugins.Errors.Views;
using Gum.Plugins.Undos;
using Gum.Plugins.FileWatchPlugin;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.Plugins.InternalPlugins.Hotkey.Views;
using Gum.Plugins.InternalPlugins.Output;

namespace Gum.Controls;

/// <summary>
/// Maps a tab's ViewModel type to the WPF view that presents it, and optionally to a custom tab
/// header. This is the WPF half of the phase 40 rule "tab content is a ViewModel, resolved to a view
/// by the head" (Direction/avalonia-migration/phase-40-plugin-panel-contract.md): a plugin in
/// Gum.Presentation passes a ViewModel to <see cref="Managers.ITabManager.AddControl"/> and
/// <see cref="MainPanelViewModel"/> asks this registry for the view. Twin of the Avalonia head's
/// <c>TabViewRegistry</c>.
/// </summary>
public class TabViewRegistry
{
    private readonly Dictionary<Type, Registration> _registrations;

    /// <summary>Creates the registry with every tab view this head has.</summary>
    public TabViewRegistry()
    {
        _registrations = new Dictionary<Type, Registration>();

        // Built-in plugins shared with the Avalonia head (Gum.Presentation).
        Register<MainOutputViewModel>(() => new MainOutputPluginView { Margin = new Thickness(4) });
        Register<HotkeyViewModel>(() => new HotkeyView());
        Register<FileWatchViewModel>(() => new FileWatchControl());
        Register<AllErrorsViewModel>(() => new ErrorDisplay(), header: () => new ErrorTabHeader());
        Register<UndosViewModel>(() => new UndoDisplay());
        Register<AlignmentViewModel>(() => new AlignmentPluginControl());
        Register<BehaviorsViewModel>(() => new BehaviorsControl());
    }

    /// <summary>The ViewModel types with a registered view.</summary>
    public IReadOnlyCollection<Type> RegisteredViewModelTypes => _registrations.Keys;

    /// <summary>Registers (or replaces) the view, and optional header, for <typeparamref name="TViewModel"/>.</summary>
    public void Register<TViewModel>(Func<FrameworkElement> content, Func<FrameworkElement>? header = null) where TViewModel : class =>
        _registrations[typeof(TViewModel)] = new Registration(content, header);

    /// <summary>True when <paramref name="viewModelType"/> or one of its base types has a registered view.</summary>
    public bool HasView(Type viewModelType) => Find(viewModelType) != null;

    /// <summary>Creates the view for <paramref name="viewModel"/> with its DataContext set, or null if none is registered.</summary>
    public FrameworkElement? CreateView(object viewModel) => Create(Find(viewModel.GetType())?.Content, viewModel);

    /// <summary>Creates the custom tab header for <paramref name="viewModel"/>, or null when the tab shows its title.</summary>
    public FrameworkElement? CreateHeader(object viewModel) => Create(Find(viewModel.GetType())?.Header, viewModel);

    private static FrameworkElement? Create(Func<FrameworkElement>? factory, object viewModel)
    {
        if (factory == null)
        {
            return null;
        }
        FrameworkElement element = factory();
        element.DataContext = viewModel;
        return element;
    }

    private Registration? Find(Type viewModelType)
    {
        for (Type? type = viewModelType; type != null; type = type.BaseType)
        {
            if (_registrations.TryGetValue(type, out Registration? registration))
            {
                return registration;
            }
        }
        return null;
    }

    private sealed record Registration(Func<FrameworkElement> Content, Func<FrameworkElement>? Header);
}
