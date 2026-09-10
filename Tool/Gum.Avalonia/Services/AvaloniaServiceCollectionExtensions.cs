using Gum.Avalonia.Dialogs;
using Gum.Avalonia.Shell;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Logic;
using Gum.Managers;
using Gum.Menus;
using Gum.Plugins;
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

        // Plugins: none load in this head yet (phase 40 brings the plugin host across), so every
        // plugin-facing port resolves to a no-op host.
        services.AddSingleton<NullPluginManager>();
        services.AddSingleton<IPluginManager>(provider => provider.GetRequiredService<NullPluginManager>());
        services.AddSingleton<IUndoPluginNotifier>(provider => provider.GetRequiredService<NullPluginManager>());
        services.AddSingleton<IDeletePluginNotifier>(provider => provider.GetRequiredService<NullPluginManager>());
        services.AddSingleton<ICopyPastePluginNotifier>(provider => provider.GetRequiredService<NullPluginManager>());
        services.AddSingleton<IRenamePluginNotifier>(provider => provider.GetRequiredService<NullPluginManager>());

        // Property-grid-coupled contracts: placeholders until phase 70 re-authors the grid.
        services.AddSingleton<IStandardElementsManagerGumTool, NullStandardElementsManagerGumTool>();
        services.AddSingleton<IBehaviorVariablePropertyGridSink, NullBehaviorVariablePropertyGridSink>();
        services.AddSingleton<IFilePickingFolderProvider, NullFilePickingFolderProvider>();
        services.AddSingleton<IVariableTypeConverterProvider, DefaultVariableTypeConverterProvider>();
        services.AddSingleton<ICompositeMemberRegistry, EmptyCompositeMemberRegistry>();

        // Shell.
        services.AddSingleton<AvaloniaTabManager>();
        services.AddSingleton<ITabManager>(provider => provider.GetRequiredService<AvaloniaTabManager>());
        services.AddSingleton<StandardMenuModelBuilder>();
        services.AddSingleton<ShellViewModel>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<AvaloniaHeadStartup>();

        return services;
    }
}
