using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Gum.Avalonia.Panels;
using Gum.Managers;
using Gum.Plugins.Behaviors;
using Gum.Plugins.Errors;
using Gum.Plugins.FileWatchPlugin;
using Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;
using Gum.Plugins.Undos;
using PerformanceMeasurementPlugin.ViewModels;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;

namespace Gum.Avalonia.Shell;

/// <summary>
/// Maps a tab's ViewModel type to the Avalonia view that presents it, and optionally to a custom tab
/// header. This is the Avalonia half of the phase 40 rule "tab content is a ViewModel, resolved to a
/// view by the head": a plugin in Gum.Presentation passes a ViewModel to
/// <see cref="Gum.Managers.ITabManager.AddControl"/> and <see cref="AvaloniaTabManager"/> asks this
/// registry for the view. Twin of the WPF head's <c>TabViewRegistry</c>.
/// </summary>
public class TabViewRegistry
{
    private readonly Dictionary<Type, Registration> _registrations;

    /// <summary>Creates the registry with every tab view this head has.</summary>
    public TabViewRegistry()
    {
        _registrations = new Dictionary<Type, Registration>();

        // Built-in plugins shared with the WPF head (Gum.Presentation).
        Register<MainOutputViewModel>(() => new OutputView());
        Register<HotkeyViewModel>(() => new HotkeyView());
        Register<FileWatchViewModel>(() => new FileWatchView());
        Register<AllErrorsViewModel>(() => new ErrorsView(), header: () => new ErrorTabHeaderView());
        Register<UndosViewModel>(() => new UndosView());
        Register<AlignmentViewModel>(() => new AlignmentView());
        Register<BehaviorsViewModel>(() => new BehaviorsView());

        // Plugins in their own projects that ship no views.
        Register<PerformanceViewModel>(() => new PerformanceView());
    }

    /// <summary>The ViewModel types with a registered view.</summary>
    public IReadOnlyCollection<Type> RegisteredViewModelTypes => _registrations.Keys;

    /// <summary>Registers (or replaces) the view, and optional header, for <typeparamref name="TViewModel"/>.</summary>
    public void Register<TViewModel>(Func<Control> content, Func<Control>? header = null) where TViewModel : class =>
        _registrations[typeof(TViewModel)] = new Registration(content, header);

    /// <summary>True when <paramref name="viewModelType"/> or one of its base types has a registered view.</summary>
    public bool HasView(Type viewModelType) => Find(viewModelType) != null;

    /// <summary>Creates the view for <paramref name="viewModel"/> with its DataContext set, or null if none is registered.</summary>
    public Control? CreateView(object viewModel) => Create(Find(viewModel.GetType())?.Content, viewModel);

    /// <summary>Creates the custom tab header for <paramref name="viewModel"/>, or null when the tab shows its title.</summary>
    public Control? CreateHeader(object viewModel) => Create(Find(viewModel.GetType())?.Header, viewModel);

    private static Control? Create(Func<Control>? factory, object viewModel)
    {
        if (factory == null)
        {
            return null;
        }
        Control control = factory();
        control.DataContext = viewModel;
        return control;
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

    private sealed record Registration(Func<Control> Content, Func<Control>? Header);
}
