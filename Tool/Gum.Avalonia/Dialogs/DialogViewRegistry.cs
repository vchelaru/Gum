using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Gum.Avalonia.Dialogs.Views;
using Gum.Dialogs;
using Gum.Plugins.ImportPlugin.ViewModel;
using Gum.Plugins.InternalPlugins.LoadRecentFilesPlugin.ViewModels;
using Gum.Services.Dialogs;
using StateAnimationPlugin.ViewModels;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// Maps a <see cref="DialogViewModel"/> type to the Avalonia control that presents it. Views
/// register by view-model type; a view model with no registered view (and none for any of its
/// base types) gets a placeholder that names the missing view, so the gap is visible instead of
/// silent. This is the Avalonia head's counterpart of the WPF <c>DialogViewResolver</c>; the
/// explicit list replaces its assembly scan.
/// </summary>
public class DialogViewRegistry
{
    private readonly Dictionary<Type, Func<Control>> _factories;

    /// <summary>Creates the registry with every dialog view this head has registered.</summary>
    public DialogViewRegistry()
    {
        _factories = new Dictionary<Type, Func<Control>>();

        // Generic dialogs (phase 30).
        Register<MessageDialogViewModel>(() => new MessageDialogView());
        Register<GetUserStringDialogBaseViewModel>(() => new GetUserStringDialogView());
        Register<ChoiceDialogViewModel>(() => new ChoiceDialogView());
        Register<PluginsDialogViewModel>(() => new PluginsDialogView());

        // Phase 80.
        Register<NewProjectDialogViewModel>(() => new NewProjectDialogView());
        Register<ExposeColorDialogViewModel>(() => new ExposeColorDialogView());
        Register<DisplayReferencesDialog>(() => new DisplayReferencesDialogView());
        Register<ThemingDialogViewModel>(() => new ThemingDialogView());
        Register<LoadRecentViewModel>(() => new LoadRecentDialogView());
        Register<ImportBaseDialogViewModel>(() => new ImportFileDialogView());
        Register<AddAnimationDialogViewModel>(() => new AddAnimationDialogView());
        Register<AddStateKeyframeDialog>(() => new AddStateKeyframeDialogView());
        Register<SubAnimationSelectionDialogViewModel>(() => new SubAnimationSelectionDialogView());
        Register<DeleteOptionsDialogViewModel>(() => new DeleteOptionsDialogView());
        // The Variables tab's dialogs (phase 70).
        Register<Gum.Plugins.InternalPlugins.VariableGrid.ViewModels.AddVariableViewModel>(() => new Plugins.VariableGrid.AddVariableView());
        // Plugin dialogs whose view models live in Gum.Presentation (phase 70).
        Register<global::GumFormsPlugin.ViewModels.AddFormsViewModel>(() => new Plugins.PluginDialogs.AddFormsView());
        Register<global::ImportFromGumxPlugin.ViewModels.ImportFromGumxViewModel>(() => new Plugins.PluginDialogs.ImportFromGumxView());
        Register<global::ImportFromGumxPlugin.ViewModels.StandardDiffDetailsViewModel>(() => new Plugins.PluginDialogs.StandardDiffDetailsView());
    }

    /// <summary>The view-model types with a registered view (subclasses resolve through these).</summary>
    public IReadOnlyCollection<Type> RegisteredViewModelTypes => _factories.Keys;

    /// <summary>Registers (or replaces) the view for <typeparamref name="TViewModel"/> and its subclasses.</summary>
    public void Register<TViewModel>(Func<Control> factory) where TViewModel : DialogViewModel =>
        _factories[typeof(TViewModel)] = factory;

    /// <summary>True when <paramref name="viewModelType"/> or one of its base types has a registered view.</summary>
    public bool HasView(Type viewModelType) => FindFactory(viewModelType) != null;

    /// <summary>Creates the view for <paramref name="viewModel"/>, with its DataContext set.</summary>
    public Control CreateView(DialogViewModel viewModel)
    {
        if (FindFactory(viewModel.GetType()) is { } factory)
        {
            Control view = factory();
            view.DataContext = viewModel;
            return view;
        }

        return new TextBlock
        {
            Text = $"No Avalonia view is registered for {viewModel.GetType().Name}.",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16),
            DataContext = viewModel,
        };
    }

    /// <summary>Creates the registered view for <paramref name="viewModelType"/> with no DataContext, for tests.</summary>
    internal Control? CreateViewWithoutContext(Type viewModelType) => FindFactory(viewModelType)?.Invoke();

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
}
