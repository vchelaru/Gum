using Gum.Commands;
using Gum.Controls;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ToolCommands;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Windows;
using Gum.Dialogs;
using Gum.Mvvm;
using Gum.Services.Dialogs;
using Gum.Plugins;
using Gum.ViewModels;
using Microsoft.Extensions.Configuration;
using Gum.Settings;
using Gum.Logic.FileWatch;

namespace Gum.Services;

internal static class GumBuilder
{
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        string appDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Gum");
        Directory.CreateDirectory(appDir);
        string settingsPath = Path.Combine(appDir, "appsettings.json");

        return Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration(cfg =>
            {

                if (!File.Exists(settingsPath))
                {
                    File.WriteAllText(settingsPath, "{}");
                }

                cfg.Sources.Clear();
                cfg.SetBasePath(appDir);
                cfg.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
            })
            .ConfigureServices((context, services) =>
            {
                services.AddOptions();
                services.ConfigureWritable<ThemeSettings>(context.Configuration, nameof(ThemeSettings), settingsPath);
                services.ConfigureWritable<LayoutSettings>(context.Configuration, nameof(LayoutSettings), settingsPath);
                services.AddGum();
            });
    }


}

file static class ServiceCollectionExtensions
{
    /// <summary>
    /// The WPF head's full graph: the headless core from <see cref="GumCoreServiceCollectionExtensions.AddGumCore"/>
    /// plus this head's implementations of every contract in
    /// <see cref="GumCoreServiceCollectionExtensions.HeadProvidedContracts"/>.
    /// </summary>
    public static void AddGum(this IServiceCollection services)
    {
        services.AddGumCore();
        services.AddGumWpf();
    }

    /// <summary>
    /// Registrations whose implementations are WPF/WinForms-specific, or still live in this project
    /// pending relocation (each of the latter is listed in Direction/avalonia-migration/coverage-matrix.md).
    /// </summary>
    private static void AddGumWpf(this IServiceCollection services)
    {
        // ViewModels declared in this assembly (the headless ones are scanned by AddGumCore).
        services.ForEachConcreteTypeAssignableTo<ViewModel>(
            typeof(GumBuilder).Assembly,
            static (isp, type) => isp.AddTransient(type));

        // PluginManager: DI-constructed (lightweight ctor taking only IPluginEnablementStore); Initialize()
        // does the heavy two-stage MEF setup. Still WPF-side because it hosts WpfPluginBase's menu and
        // delete-window calls. The narrow plugin-notifier ports all resolve to this one singleton.
        services.AddSingleton<IPluginHostConfiguration, WpfPluginHostConfiguration>();
        services.AddSingleton<PluginManager>();
        services.AddSingleton<IPluginManager>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<IUndoPluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<IDeletePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<ICopyPastePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<IRenamePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());

        // The Variables tab's WPF view and editor controls; PropertyGridManager itself is in AddGumCore.
        services.AddSingleton<IVariableGridHead, WpfVariableGridHead>();

        // ElementTreeViewManager / PropertyGridManager: concrete singletons needed for the Initialize()
        // calls in WpfHeadStartup (two-stage initialization); both are WPF view managers.
        services.AddSingleton<ElementTreeViewManager>();
        services.AddSingleton<Gum.Plugins.InternalPlugins.TreeView.IElementTreeViewFactory, WpfElementTreeViewFactory>();

        // OS / framework seams.
        services.AddSingleton<IModifierKeyState, WinFormsModifierKeyState>();
        services.AddSingleton<IRecycleBinService, RecycleBinService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        // ISpinnerFactory: narrow seam (#3956) so GuiCommands.ShowSpinner constructs its progress
        // indicator without naming the concrete WPF Gum.Controls.Spinner directly.
        services.AddSingleton<ISpinnerFactory, SpinnerFactory>();
        // GuiCommands: still here for its Win32 force-foreground call (coverage-matrix.md section 4).
        services.AddSingleton<IGuiCommands, GuiCommands>();
        services.AddSingleton<MenuStripManager>();

        services.AddSingleton<TabViewRegistry>();
        services.AddSingleton<MainPanelViewModel>();
        services.AddSingleton<ITabManager>(provider => provider.GetRequiredService<MainPanelViewModel>());
        services.AddSingleton<Gum.Plugins.InternalPlugins.HideShowTools.IToolsVisibility>(provider => provider.GetRequiredService<MainPanelViewModel>());
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        // other
        services.AddDialogs();
        services.AddViewModelFuncFactories(typeof(ServiceCollectionExtensions).Assembly);
        services.AddSingleton<IDispatcher>(_ => new AppDispatcher(() => Application.Current.Dispatcher));
        services.AddSingleton<IAppScaleProvider, AppScaleProvider>();
        services.AddSingleton<IThemingService, ThemingService>();
    }

    private static IServiceCollection AddDialogs(this IServiceCollection services)
    {
        // IDialogViewAssemblyProvider: lets DialogViewResolver pair a Gum.Presentation-hosted
        // DialogViewModel with its View when the View lives in a different assembly (the Gum tool
        // itself, or a dynamically-loaded plugin like ImportFromGumxPlugin). See DialogViewResolver.cs.
        services.AddSingleton<IDialogViewAssemblyProvider, AppDomainDialogViewAssemblyProvider>();
        services.AddSingleton<IDialogViewResolver, DialogViewResolver>();
        services.AddSingleton<IDialogService, DialogService>();
        // IDeleteDialogService: headless seam (ADR-0005 Phase 3) over the standalone
        // DeleteOptionsWindow so DeleteLogic no longer references WPF or the concrete plugin
        // host for the delete-confirmation dialog. The WPF-coupled DeleteDialogService impl
        // owns the window plus the ShowDeleteDialog/DeleteConfirmed plugin calls.
        services.AddSingleton<IDeleteDialogService, DeleteDialogService>();

        return services;
    }
}
