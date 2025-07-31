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
using System.Reflection;
using Gum.Mvvm;
using Gum.Services.Dialogs;

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
        SetVariableLogic.Self.Initialize(circularReferenceManager);
        
        return host;
    }
}

file static class ServiceCollectionExtensions
{
    // Register services that have no dependencies on legacy services.
    // These must not use the Locator for resolving dependencies.
    public static void AddCleanServices(this IServiceCollection services)
    {
        //transients
        services.ForEachConcreteTypeAssignableTo<ViewModel>(
            typeof(GumBuilder).Assembly,
            static (isp, type) => isp.AddTransient(type)
        );
        services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
        
        services.AddSingleton<ISelectedState, SelectedState>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<NameVerifier>();
        services.AddSingleton<UndoManager>();
        services.AddSingleton<FontManager>();
        services.AddSingleton<HotkeyManager>();
        services.AddSingleton<IEditVariableService, EditVariableService>();
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        
        //logic
        services.AddSingleton<VariableReferenceLogic>();
        services.AddSingleton<RenameLogic>();
        
        //commands
        services.AddSingleton<WireframeCommands>();
        services.AddSingleton<GuiCommands>();
        services.AddSingleton<EditCommands>();
        services.AddSingleton<ElementCommands>();
        services.AddSingleton<FileCommands>();
        services.AddSingleton<ProjectCommands>();
        
        services.AddDialogs();
    }
    
    // Register legacy services that may use Locator or have unresolved dependencies.
    // These may depend on services within this container, but should avoid doing so
    // to ease migration. Once all their dependencies are in the clean container,
    // they can be moved to AddCleanServices.
    public static void AddLegacyServices(this IServiceCollection services)
    {
        services.AddSingleton(SetVariableLogic.Self);
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);
        
        services.AddSingleton<CircularReferenceManager>();
        services.AddSingleton<DragDropManager>();
    }

    private static IServiceCollection AddDialogs(this IServiceCollection services)
    {
        services.AddSingleton<IMainWindowHandleProvider, MainFormWindowHandleProvider>();
        services.AddSingleton<IDialogViewResolver, DialogViewResolver>();
        services.AddSingleton<IDialogService, DialogService>();

        return services;
    }
    
    private class Lazier<T> : Lazy<T> where T : notnull
    {
        public Lazier(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<T>){}
    }
}

file static class ServiceCollectionHelpers
{
    public static IServiceCollection ForEachConcreteTypeAssignableTo<TBaseType>(
        this IServiceCollection services,
        Assembly assembly,
        Action<IServiceCollection, Type> callback)
    {
        Type baseType = typeof(TBaseType);

        IEnumerable<Type> closedTypes = assembly.DefinedTypes
            .Where(t =>
                t.IsClass &&
                !t.IsAbstract &&
                !t.IsGenericTypeDefinition &&
                baseType.IsAssignableFrom(t) &&
                t.DeclaredConstructors.Any(c => c.IsPublic && !c.IsStatic))
            .Select(t => t.AsType());

        foreach (Type type in closedTypes)
        {
            callback(services, type);
        }

        return services;
    }
}