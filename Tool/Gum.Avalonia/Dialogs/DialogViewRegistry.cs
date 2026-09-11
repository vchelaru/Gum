using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Gum.Avalonia.Dialogs.Views;
using Gum.Services.Dialogs;

namespace Gum.Avalonia.Dialogs;

/// <summary>
/// Maps a <see cref="DialogViewModel"/> type to the Avalonia control that presents it. Views
/// register by view-model type; a view model with no registered view (and none for any of its
/// base types) gets a placeholder that names the missing view, so the gap is visible instead of
/// silent. Later phases register their views here as they port dialogs.
/// </summary>
public class DialogViewRegistry
{
    private readonly Dictionary<Type, Func<Control>> _factories;

    /// <summary>Creates the registry with the generic dialogs the shell needs registered.</summary>
    public DialogViewRegistry()
    {
        _factories = new Dictionary<Type, Func<Control>>();
        Register<MessageDialogViewModel>(() => new MessageDialogView());
        Register<GetUserStringDialogBaseViewModel>(() => new GetUserStringDialogView());
        Register<ChoiceDialogViewModel>(() => new ChoiceDialogView());
        Register<PluginsDialogViewModel>(() => new PluginsDialogView());
        // The Variables tab's dialogs (phase 70).
        Register<Gum.Plugins.InternalPlugins.VariableGrid.ViewModels.AddVariableViewModel>(() => new Plugins.VariableGrid.AddVariableView());
        Register<Gum.Dialogs.ExposeColorDialogViewModel>(() => new Plugins.VariableGrid.ExposeColorView());
    }

    /// <summary>Registers (or replaces) the view for <typeparamref name="TViewModel"/> and its subclasses.</summary>
    public void Register<TViewModel>(Func<Control> factory) where TViewModel : DialogViewModel =>
        _factories[typeof(TViewModel)] = factory;

    /// <summary>Creates the view for <paramref name="viewModel"/>, with its DataContext set.</summary>
    public Control CreateView(DialogViewModel viewModel)
    {
        for (Type? type = viewModel.GetType(); type != null; type = type.BaseType)
        {
            if (_factories.TryGetValue(type, out Func<Control>? factory))
            {
                Control view = factory();
                view.DataContext = viewModel;
                return view;
            }
        }

        return new TextBlock
        {
            Text = $"No Avalonia view is registered for {viewModel.GetType().Name} yet (phase 80).",
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(16),
            DataContext = viewModel,
        };
    }
}
