using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.InternalPlugins.VariableGrid.ViewModels;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using GumCommon;

namespace Gum.Services;

internal static class GumBuilder
{
    public static IHost BuildGum(string[]? args = null)
    {
        // Build Host
        IHost host = Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddCleanServices();
            })
            .Build();
        
        Locator.Register(host.Services);

        // Register legacy services
        ServiceCollection legacyServices = new();
        legacyServices.AddLegacyServices();
        Locator.Register(legacyServices.BuildServiceProvider());
        
        // This is needed until we unroll all the static singletons...
        CircularReferenceManager circularReferenceManager = Locator.GetRequiredService<CircularReferenceManager>();
        FileCommands fileCommands = Locator.GetRequiredService<FileCommands>();
        SetVariableLogic.Self.Initialize(circularReferenceManager, fileCommands);
        
        return host;
    }
}

file static class ServiceCollectionExtensions
{
    // Register services that have no dependencies on legacy services.
    // These must not use the Locator for resolving dependencies.
    public static void AddCleanServices(this IServiceCollection services)
    {
        services.AddSingleton<ISelectedState, SelectedState>();
        services.AddSingleton<RenameLogic>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<NameVerifier>();
    }
    
    // Register legacy services that may use Locator or have unresolved dependencies.
    // These may depend on services within this container, but should avoid doing so
    // to ease migration. Once all their dependencies are in the clean container,
    // they can be moved to AddCleanServices.
    public static void AddLegacyServices(this IServiceCollection services)
    {
        services.AddSingleton(ElementCommands.Self);
        services.AddSingleton(GumCommands.Self.GuiCommands);
        services.AddSingleton(GumCommands.Self.FileCommands);
        services.AddSingleton(UndoManager.Self);
        services.AddSingleton(SetVariableLogic.Self);
        services.AddSingleton(HotkeyManager.Self);
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);
        
        services.AddSingleton<CircularReferenceManager>();
        services.AddSingleton<FontManager>();
        services.AddSingleton<DragDropManager>();
        services.AddSingleton<IEditVariableService, EditVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();
        
        services.AddTransient<AddVariableViewModel>();
    }
}