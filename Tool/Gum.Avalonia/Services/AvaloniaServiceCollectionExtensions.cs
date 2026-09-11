using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Plugins.TreeView;
using Gum.Avalonia.Shell;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Avalonia.Services;

/// <summary>
/// This head's implementations of every contract in
/// <see cref="GumCoreServiceCollectionExtensions.HeadProvidedContracts"/>, plus the shell.
/// </summary>
public static class AvaloniaServiceCollectionExtensions
{
    /// <summary>Registers the Avalonia head. Call after <see cref="GumCoreServiceCollectionExtensions.AddGumCore"/>.</summary>
    public static IServiceCollection AddGumAvalonia(this IServiceCollection services)
    {
        // Framework seams.
        services.AddSingleton<IDispatcher, AvaloniaDispatcher>();
        services.AddSingleton<IClipboardService, AvaloniaClipboardService>();
        services.AddSingleton<IAppScaleProvider, AvaloniaAppScaleProvider>();
        services.AddSingleton<IThemingService, AvaloniaThemingService>();
        services.AddSingleton<AvaloniaModifierKeyState>();
        services.AddSingleton<IModifierKeyState>(provider => provider.GetRequiredService<AvaloniaModifierKeyState>());
        services.AddSingleton<IRecycleBinService, AvaloniaRecycleBinService>();
        services.AddSingleton<ISpinnerFactory, AvaloniaSpinnerFactory>();
        services.AddSingleton<IGuiCommands, AvaloniaGuiCommands>();

        // Dialogs.
        services.AddSingleton<DialogViewRegistry>();
        services.AddSingleton<IDialogService, AvaloniaDialogService>();
        services.AddSingleton<IDeleteDialogService, AvaloniaDeleteDialogService>();

        // Plugins: the shared MEF host, configured with this head's built-in plugins and exports.
        services.AddSingleton<IPluginHostConfiguration, AvaloniaPluginHostConfiguration>();
        services.AddSingleton<PluginManager>();
        services.AddSingleton<IPluginManager>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<IUndoPluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<IDeletePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<ICopyPastePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<IRenamePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());

        // Property-grid-coupled contracts: placeholders until phase 70 re-authors the grid.
        services.AddSingleton<IStandardElementsManagerGumTool, NullStandardElementsManagerGumTool>();
        services.AddSingleton<IBehaviorVariablePropertyGridSink, NullBehaviorVariablePropertyGridSink>();
        services.AddSingleton<IFilePickingFolderProvider, NullFilePickingFolderProvider>();
        services.AddSingleton<IVariableTypeConverterProvider, DefaultVariableTypeConverterProvider>();
        services.AddSingleton<ICompositeMemberRegistry, EmptyCompositeMemberRegistry>();

        // Element tree: the shared manager over this head's Project panel.
        services.AddSingleton<IElementTreeViewFactory, AvaloniaElementTreeViewFactory>();
        services.AddSingleton<ElementTreeViewManager>();

        // Shell.
        services.AddSingleton<AvaloniaTabManager>();
        services.AddSingleton<ITabManager>(provider => provider.GetRequiredService<AvaloniaTabManager>());
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<AvaloniaHeadStartup>();

        return services;
    }
}
