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
            .AddAndUnravelGumCommands();
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
        
        services.AddSingleton<IObjectFinder, ObjectFinder>();

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

        // ...others?
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
        services.AddSingleton<StandardElementsManager>();
        services.AddSingleton<CircularReferenceManager>();
        
        return services;
    }
}