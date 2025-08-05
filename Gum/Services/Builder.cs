using Gum.Commands;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.InternalPlugins.VariableGrid;
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
    public static IHostBuilder CreateHostBuilder(string[]? args = null)
    {
        return Host.CreateDefaultBuilder(args)
            .ConfigureServices(services =>
            {
                services.AddGum();
            });
    }
}

file static class ServiceCollectionExtensions
{
    public static void AddGum(this IServiceCollection services)
    {
        // transients
        services.ForEachConcreteTypeAssignableTo<ViewModel>(
            typeof(GumBuilder).Assembly,
            static (isp, type) => isp.AddTransient(type)
        );
        services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
        
        // static singletons
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);
        
        // singletons
        services.AddSingleton<ISelectedState, SelectedState>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<NameVerifier>();
        services.AddSingleton<UndoManager>();
        services.AddSingleton<FontManager>();
        services.AddSingleton<HotkeyManager>();
        services.AddSingleton<IEditVariableService, EditVariableService>();
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        services.AddSingleton<CircularReferenceManager>();
        services.AddSingleton<DragDropManager>();
        
        services.AddSingleton<VariableReferenceLogic>();
        services.AddSingleton<RenameLogic>();
        services.AddSingleton<SetVariableLogic>();
        
        services.AddSingleton<WireframeCommands>();
        services.AddSingleton<GuiCommands>();
        services.AddSingleton<EditCommands>();
        services.AddSingleton<ElementCommands>();
        services.AddSingleton<FileCommands>();
        services.AddSingleton<ProjectCommands>();
        
        // other
        services.AddDialogs();
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