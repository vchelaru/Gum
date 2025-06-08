using Gum.Commands;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using Microsoft.Extensions.DependencyInjection;
using RenderingLibrary.Content;

namespace Gum.Services;

public static class IoC
{
    public static IServiceCollection AddGum(this IServiceCollection services)
    {
        return services
            .AddViews()
            .AddViewModels()
            .AddServices()
            .AddManagers()
            .AddAndUnravelGumCommands()
            .AddSharedServices();
    }

    private static IServiceCollection AddServices(this IServiceCollection services)
    {
        services.AddSingleton<NameVerifier>();
        
        services.AddSingleton<CopyPasteLogic>();
        services.AddSingleton<SetVariableLogic>();
        services.AddSingleton<RenameLogic>();
        
        services.AddSingleton<ProjectState>();
        services.AddSingleton<SelectedState>();
        services.AddSingleton<ISelectedState>(isp => isp.GetRequiredService<SelectedState>());
        
        services.AddSingleton<IEditVariableService, EditVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();

        return services;
    }

    private static IServiceCollection AddViews(this IServiceCollection services)
    {
        services.AddSingleton<MainWindow>();

        return services;
    }

    private static IServiceCollection AddViewModels(this IServiceCollection services)
    {
        // singleton
        services.AddSingleton<MainWindowViewModel>();
        
        // transient
        services.AddTransient<AddVariableViewModel>();

        return services;
    }

    private static IServiceCollection AddAndUnravelGumCommands(this IServiceCollection services)
    {
        // Gum Commands
        services.AddSingleton<GumCommands>();
        services.AddSingleton<GuiCommands>();
        services.AddSingleton<FileCommands>();
        services.AddSingleton<EditCommands>();
        services.AddSingleton<WireframeCommands>();
        services.AddSingleton<ProjectCommands>();

        // ...others
        services.AddSingleton<ElementCommands>();
        return services;
    }

    private static IServiceCollection AddManagers(this IServiceCollection services)
    {
        services.AddSingleton<PluginManager>();
        services.AddSingleton<ProjectManager>();
        services.AddSingleton<HotkeyManager>();
        services.AddSingleton<FileWatchManager>();
        services.AddSingleton<WireframeObjectManager>();
        services.AddSingleton<PropertyGridManager>();
        services.AddSingleton<ElementTreeViewManager>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<FontManager>();
        services.AddSingleton<DragDropManager>();
        services.AddSingleton<StandardElementsManagerGumTool>();
        services.AddSingleton<TypeManager>();
        services.AddSingleton<UndoManager>();
        services.AddSingleton<CircularReferenceManager>();
        
        return services;
    }
    
    /// <summary>
    /// These services are shared between tools (e.g. also used by FRB in isolation).
    /// As a result, their lazy instantiation patterns must remain intact.
    /// Instead of letting the container construct them, we register the instance directly.
    /// We do these here for clarity to make it apparent dependencies can't be injected.
    /// </summary>
    private static IServiceCollection AddSharedServices(this IServiceCollection services)
    {
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);
        services.AddSingleton(StandardElementsManager.Self);
        return services;
    }
}