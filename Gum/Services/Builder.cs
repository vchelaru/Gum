using Gum.Commands;
using Gum.Controls;
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
using CommunityToolkit.Mvvm.Messaging;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Threading;
using Gum.Mvvm;
using Gum.Services.Dialogs;
using Gum.Plugins;
using Gum.ViewModels;
using Expression = System.Linq.Expressions.Expression;
using Gum.Plugins.ImportPlugin.Manager;

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
        services.AddTransient<PeriodicUiTimer>();
        
        // static singletons
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);

        // singletons
        services.AddSingleton<ISelectedState, SelectedState>();
        services.AddSingleton<LocalizationManager>();
        services.AddSingleton<INameVerifier, NameVerifier>();
        services.AddSingleton<IUndoManager, UndoManager>();
        services.AddSingleton<CopyPasteLogic>();
        services.AddSingleton<FontManager>();
        services.AddSingleton<HotkeyManager>();
        services.AddSingleton<IEditVariableService, EditVariableService>();
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        services.AddSingleton<CircularReferenceManager>();
        services.AddSingleton<DragDropManager>();
        services.AddSingleton<MenuStripManager>();
        services.AddSingleton<ImportLogic>();

        services.AddSingleton<VariableReferenceLogic>();
        services.AddSingleton<IRenameLogic, RenameLogic>();
        services.AddSingleton<SetVariableLogic>();
        
        services.AddSingleton<WireframeCommands>();
        services.AddSingleton<IGuiCommands, GuiCommands>();
        services.AddSingleton<EditCommands>();
        services.AddSingleton<VariableInCategoryPropagationLogic>();
        services.AddSingleton<IElementCommands, ElementCommands>();
        services.AddSingleton<IFileCommands, FileCommands>();
        services.AddSingleton<ProjectCommands>();

        services.AddSingleton<IMessenger, WeakReferenceMessenger>();
        
        services.AddSingleton<MainPanelViewModel>();
        services.AddSingleton<ITabManager>(provider => provider.GetRequiredService<MainPanelViewModel>());
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();
        
        // other
        services.AddDialogs();
        services.AddViewModelFuncFactories(typeof(ServiceCollectionExtensions).Assembly);
        services.AddSingleton<IDispatcher>(_ => new AppDispatcher(() => Application.Current.Dispatcher));
        services.AddSingleton<IUiSettingsService, UiSettingsService>();

    }
    
    private static IServiceCollection AddDialogs(this IServiceCollection services)
    {
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

file static class ViewModelFuncFactoryRegistration
{
    public static IServiceCollection AddViewModelFuncFactories(this IServiceCollection services, Assembly targetAssembly)
    {
        Type[] allTypes = targetAssembly.GetTypes();

        foreach (Type type in allTypes)
        {
            if (!type.IsClass || type.IsAbstract)
                continue;

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (ParameterInfo param in ctor.GetParameters())
                {
                    if (!IsFuncType(param.ParameterType))
                        continue;

                    Type[] funcArgs = param.ParameterType.GetGenericArguments();
                    Type resultType = funcArgs.Last();

                    if (!typeof(ViewModel).IsAssignableFrom(resultType))
                        continue;

                    RegisterFuncFactory(services, param.ParameterType, funcArgs);
                }
            }
        }

        return services;
    }

    private static bool IsFuncType(Type t)
    {
        if (!t.IsGenericType) return false;
        Type? def = t.GetGenericTypeDefinition();
        return def.FullName!.StartsWith("System.Func");
    }

    private static void RegisterFuncFactory(IServiceCollection services, Type funcType, Type[] typeArgs)
    {
        if (services.Any(sd => sd.ServiceType == funcType))
            return;

        Type resultType = typeArgs.Last();
        Type[] paramTypes = typeArgs.Take(typeArgs.Length - 1).ToArray();

        ObjectFactory factory = ActivatorUtilities.CreateFactory(resultType, paramTypes);

        var factoryDelegate = BuildFactoryLambda(funcType, factory, paramTypes, resultType);
        Delegate factoryFunc = (Delegate)factoryDelegate;
        services.AddTransient(funcType, sp => factoryFunc.DynamicInvoke(sp)!);
    }

    private static object BuildFactoryLambda(Type funcType, ObjectFactory factory, Type[] paramTypes, Type resultType)
    {
        ParameterExpression spParam = Expression.Parameter(typeof(IServiceProvider), "sp");

        ParameterExpression[] delegateParams = paramTypes.Select(Expression.Parameter).ToArray();

        NewArrayExpression argsArray = Expression.NewArrayInit(typeof(object),
            delegateParams.Select(p => Expression.Convert(p, typeof(object)))
        );

        MethodCallExpression factoryCall = Expression.Call(
            Expression.Constant(factory),
            typeof(ObjectFactory).GetMethod("Invoke")!,
            spParam,
            argsArray
        );

        UnaryExpression castResult = Expression.Convert(factoryCall, resultType);

        LambdaExpression innerLambda = Expression.Lambda(funcType, castResult, delegateParams);

        Type outerFuncType = typeof(Func<,>).MakeGenericType(typeof(IServiceProvider), funcType);
        return Expression.Lambda(outerFuncType, innerLambda, spParam).Compile();
    }
}