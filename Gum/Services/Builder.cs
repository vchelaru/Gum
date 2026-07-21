using Gum.CommandLine;
using Gum.Commands;
using Gum.Controls;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.SelectionHistory;
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
using Gum.Dialogs;
using Gum.Mvvm;
using Gum.Services.Dialogs;
using Gum.Plugins;
using Gum.ViewModels;
using Expression = System.Linq.Expressions.Expression;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Wireframe;
using Microsoft.Extensions.Configuration;
using System.IO;
using Gum.Settings;
using ToolsUtilities;
using Gum.Logic.FileWatch;
using Gum.Reflection;
using Gum.ProjectServices.FontGeneration;
using Gum.Services.Fonts;
using Gum.Localization;

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
    public static void AddGum(this IServiceCollection services)
    {
        // transients
        services.ForEachConcreteTypeAssignableTo<ViewModel>(
            typeof(GumBuilder).Assembly,
            static (isp, type) => isp.AddTransient(type)
        );
        // ADR-0005: VMs relocated into the headless Gum.Presentation assembly (e.g. HotkeyViewModel,
        // the GetUserStringDialogBaseViewModel dialog family) live outside typeof(GumBuilder).Assembly,
        // so they need their own scan or they're silently unregistered.
        services.ForEachConcreteTypeAssignableTo<ViewModel>(
            typeof(DialogViewModel).Assembly,
            static (isp, type) => isp.AddTransient(type)
        );
        services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
        services.AddTransient<PeriodicUiTimer>();

        // static singletons
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);
        // PluginManager: drained from a static Self singleton (#3291). DI-constructed (empty ctor); Initialize()
        // (Program.cs) does the heavy two-stage setup and registers the global instance for the static plugin machinery.
        services.AddSingleton<PluginManager>();
        services.AddSingleton<IPluginManager>(provider => provider.GetRequiredService<PluginManager>());
        // IUndoPluginNotifier: narrow headless port (ADR-0005 Phase 3) so UndoManager no longer depends on the
        // concrete PluginManager. Resolves to the same PluginManager singleton, so plugin calls fire as before.
        services.AddSingleton<IUndoPluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        // IDeletePluginNotifier: narrow headless port (ADR-0005 Phase 3) so DeleteLogic no longer depends on the
        // concrete PluginManager for delete notifications. Resolves to the same PluginManager singleton, so plugin
        // calls fire as before. The two WPF-coupled delete calls (ShowDeleteDialog/DeleteConfirmed) now live behind
        // IDeleteDialogService (see AddDialogs), removing DeleteLogic's last dependency on IPluginManager.
        services.AddSingleton<IDeletePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        // ICopyPastePluginNotifier: same narrow-port pattern as IDeletePluginNotifier/IUndoPluginNotifier, for
        // CopyPasteLogic's InstanceAdd/ElementDuplicate plugin notifications. Resolves to the same PluginManager
        // singleton.
        services.AddSingleton<ICopyPastePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        // IRenamePluginNotifier: same narrow-port pattern, for RenameLogic's StateRename/CategoryRename/
        // ElementRename/InstanceRename plugin notifications. Resolves to the same PluginManager singleton.
        services.AddSingleton<IRenamePluginNotifier>(provider => provider.GetRequiredService<PluginManager>());
        services.AddSingleton<TypeManager>();
        services.AddSingleton<ITypeManager>(provider => provider.GetRequiredService<TypeManager>());
        services.AddSingleton<ProjectManager>();
        services.AddSingleton<StandardElementsManagerGumTool>();
        services.AddSingleton<IStandardElementsManagerGumTool>(provider => provider.GetRequiredService<StandardElementsManagerGumTool>());
        services.AddSingleton<IProjectManager>(provider => provider.GetRequiredService<ProjectManager>());
        // IDeleteProjectProvider: narrow headless port (ADR-0005 Phase 3) so DeleteLogic reads the loaded
        // project through a single-property port instead of the wider IProjectManager. Resolves to the same
        // ProjectManager singleton, so it returns the live GumProjectSave exactly as before.
        services.AddSingleton<IDeleteProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        // ICopyPasteProjectProvider: same narrow-port pattern as IDeleteProjectProvider, for CopyPasteLogic's
        // element-name-uniqueness check. Resolves to the same ProjectManager singleton.
        services.AddSingleton<ICopyPasteProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        // IReferenceFinderProjectProvider: same narrow-port pattern, for ReferenceFinder's project reads.
        // Resolves to the same ProjectManager singleton.
        services.AddSingleton<IReferenceFinderProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        // IRenameProjectProvider: same narrow-port pattern, for RenameLogic's project reads in
        // RenameAllReferencesTo. Resolves to the same ProjectManager singleton.
        services.AddSingleton<IRenameProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        services.AddSingleton<ICommandLineManager, CommandLineManager>();
        services.AddSingleton<IProjectState, ProjectState>();
        // ElementTreeViewManager: drained from a static Self singleton (#3286). Concrete is needed for the
        // Initialize() call in Program.cs (two-stage initialization); no interface is extracted because it is a
        // bloated WinForms-coupled UI class whose only consumer couples to internal/UI-typed members.
        services.AddSingleton<ElementTreeViewManager>();
        // PropertyGridManager: drained from a static Self singleton (#3288), same pattern as ElementTreeViewManager.
        // Concrete (no interface) because it is a bloated WinForms/WPF-coupled UI class. GuiCommands is both a
        // consumer AND a dependency of it, so it breaks the construction cycle by injecting Lazy<PropertyGridManager>.
        services.AddSingleton<PropertyGridManager>();
        // IBehaviorVariablePropertyGridSink: narrow headless port (issue #3875) so the relocated SelectedState
        // (Gum.Presentation) can push the selected behavior variable into the property grid without depending on
        // the concrete PropertyGridManager. Resolves to the same PropertyGridManager singleton.
        services.AddSingleton<IBehaviorVariablePropertyGridSink>(provider => provider.GetRequiredService<PropertyGridManager>());

        // singletons
        services.AddSingleton<ICircularReferenceManager, CircularReferenceManager>();
        services.AddSingleton<IFavoriteComponentManager, FavoriteComponentManager>();
        services.AddSingleton<ICopyPasteLogic, CopyPasteLogic>();
        services.AddSingleton<IDeleteLogic, DeleteLogic>();
        services.AddSingleton<IGumProjectRepairLogic, GumProjectRepairLogic>();
        services.AddSingleton<ISkiaShapeStandardsLogic, SkiaShapeStandardsLogic>();
        services.AddSingleton<FileLocations>();
        // IFileLocations: narrow headless port (ADR-0005 Phase 3) so dialog VMs that only need
        // folder paths can move into Gum.Presentation. Resolves to the same FileLocations singleton.
        services.AddSingleton<IFileLocations>(provider => provider.GetRequiredService<FileLocations>());
        services.AddSingleton<FileWatchLogic>();
        services.AddSingleton<IFontGenerationCallbacks, ToolFontGenerationCallbacks>();
        services.AddSingleton<IFontFileGenerator>(provider =>
        {
            IFontGenerationCallbacks callbacks = provider.GetRequiredService<IFontGenerationCallbacks>();
            BmFontExeFileGenerator bmFont = new BmFontExeFileGenerator(callbacks);
            KernSmithFileGenerator kernSmith = new KernSmithFileGenerator(callbacks);
            IProjectState projectState = provider.GetRequiredService<IProjectState>();
            return new FontFileGeneratorSelector(bmFont, kernSmith,
                () => projectState.GumProjectSave?.FontGenerator ?? DataTypes.FontGeneratorType.BmFont);
        });
        services.AddSingleton<IHeadlessFontGenerationService>(provider =>
            new HeadlessFontGenerationService(
                provider.GetRequiredService<IFontFileGenerator>(),
                provider.GetRequiredService<IFontGenerationCallbacks>()));
        services.AddSingleton<IFontManager, FontManager>();
        services.AddSingleton<IHotkeyManager, HotkeyManager>();
        services.AddSingleton<IRetryService, RetryService>();
        services.AddSingleton<LocalizationService>();
        services.AddSingleton<ILocalizationService>(provider => provider.GetRequiredService<LocalizationService>());
        services.AddSingleton<ISelectedState, SelectedState>();
        services.AddSingleton<INameVerifier, NameVerifier>();
        services.AddSingleton<IUndoManager, UndoManager>();
        services.AddSingleton<ISelectionHistory, SelectionHistoryService>();
        // Late-bound seam for folding animation edits into the element undo snapshot (#3406). The relay
        // is what UndoManager/ElementUndoStrategy receive at construction; the animation plugin registers
        // itself as the real provider in its StartUp (via IAnimationUndoProviderRegistrar, bridged into
        // the plugin MEF container). Both interfaces resolve to the one relay singleton.
        services.AddSingleton<AnimationUndoProviderRelay>();
        services.AddSingleton<IAnimationUndoProvider>(provider => provider.GetRequiredService<AnimationUndoProviderRelay>());
        services.AddSingleton<IAnimationUndoProviderRegistrar>(provider => provider.GetRequiredService<AnimationUndoProviderRelay>());
        services.AddSingleton<EditVariableService>();
        services.AddSingleton<IEditVariableService>(provider => provider.GetRequiredService<EditVariableService>());
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        services.AddSingleton<IDragDropManager, DragDropManager>();
        services.AddSingleton<MenuStripManager>();
        services.AddSingleton<ImportLogic>();
        services.AddSingleton<IImportLogic>(provider => provider.GetRequiredService<ImportLogic>());
        services.AddSingleton<MainOutputViewModel>();

        // WireframeObjectManager concrete class is needed for Initialize() call in Program.cs (two-stage initialization)
        services.AddSingleton<IWireframeObjectManager, WireframeObjectManager>();
        services.AddSingleton<IOutputManager>(provider => provider.GetRequiredService<MainOutputViewModel>());
        services.AddSingleton<FileWatchIgnoreList>();
        services.AddSingleton<IFileWatchIgnoreList>(provider => provider.GetRequiredService<FileWatchIgnoreList>());
        services.AddSingleton<IRecycleBinService, RecycleBinService>();
        services.AddSingleton<ICsvLocalizationLoader, CsvLocalizationLoader>();
        services.AddSingleton<FileWatchManager>();
        services.AddSingleton<IFileWatchManager>(provider => provider.GetRequiredService<FileWatchManager>());
        services.AddSingleton<ReorderLogic>();
        services.AddSingleton<IReorderLogic>(provider => provider.GetRequiredService<ReorderLogic>());
        services.AddSingleton<InheritanceLogic>();

        services.AddSingleton<IUserProjectSettingsManager, UserProjectSettingsManager>();
        services.AddSingleton<ProjectServices.ITypeResolver>(provider =>
            new TypeManagerTypeResolverAdapter(provider.GetRequiredService<Reflection.ITypeManager>()));
        services.AddSingleton<ProjectServices.IElementAnimationsProvider, ProjectServices.FileElementAnimationsProvider>();
        services.AddSingleton<ProjectServices.IAdditionalErrorSource, ProjectServices.AnimationKeyframeErrorSource>();
        services.AddSingleton<ProjectServices.IHeadlessErrorChecker>(provider =>
            new ProjectServices.HeadlessErrorChecker(
                provider.GetRequiredService<ProjectServices.ITypeResolver>(),
                provider.GetServices<ProjectServices.IAdditionalErrorSource>()));
        services.AddSingleton<ProjectServices.IErrorDocsRegistry, ProjectServices.ErrorDocsRegistry>();
        services.AddSingleton<ErrorChecker>();
        services.AddSingleton<IErrorChecker>(provider => provider.GetRequiredService<ErrorChecker>());
        services.AddSingleton<IVariableSaveLogic, VariableSaveLogic>();
        services.AddSingleton<IVariableReferenceLogic, VariableReferenceLogic>();
        services.AddSingleton<IReferenceFinder, ReferenceFinder>();
        services.AddSingleton<RenameLogic>();
        services.AddSingleton<IRenameLogic>(provider => provider.GetRequiredService<RenameLogic>());
        // IUndoRenameLogic: narrow headless rename port (ADR-0005 Phase 3) so UndoManager no longer depends on the
        // full IRenameLogic. Resolves to the same RenameLogic singleton, so rename calls fire as before.
        services.AddSingleton<IUndoRenameLogic>(provider => provider.GetRequiredService<RenameLogic>());
        services.AddSingleton<ISetVariableLogic, SetVariableLogic>();
        services.AddSingleton<CommonControlLogic>();

        services.AddSingleton<WireframeCommands>();
        services.AddSingleton<IWireframeCommands>(provider => provider.GetRequiredService<WireframeCommands>());
        services.AddSingleton<IGuiCommands, GuiCommands>();
        services.AddSingleton<IEditCommands, EditCommands>();
        services.AddSingleton<IVariableInCategoryPropagationLogic, VariableInCategoryPropagationLogic>();
        services.AddSingleton<ICompositeMemberRegistry, CompositeMemberRegistry>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IElementCommands, ElementCommands>();
        services.AddSingleton<IFileCommands, FileCommands>();
        services.AddSingleton<FileChangeReactionLogic>();
        services.AddSingleton<ProjectCommands>();
        // ICopyPasteProjectCommands: narrow headless port (ADR-0005 Phase 3) so CopyPasteLogic depends on only
        // the three project-mutation calls it needs instead of the wider ProjectCommands. Resolves to the same
        // ProjectCommands singleton.
        services.AddSingleton<ICopyPasteProjectCommands>(provider => provider.GetRequiredService<ProjectCommands>());

        services.AddSingleton<IMessenger>(_ => WeakReferenceMessenger.Default);

        services.AddSingleton<MainPanelViewModel>();
        services.AddSingleton<ITabManager>(provider => provider.GetRequiredService<MainPanelViewModel>());
        services.AddSingleton<MainWindow>();
        services.AddSingleton<MainWindowViewModel>();

        // other
        services.AddDialogs();
        services.AddViewModelFuncFactories(typeof(ServiceCollectionExtensions).Assembly);
        services.AddSingleton<IDispatcher>(_ => new AppDispatcher(() => Application.Current.Dispatcher));
        services.AddSingleton<IAppScaleProvider, AppScaleProvider>();
        services.AddSingleton<IUiSettingsService, UiSettingsService>();
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

    private class Lazier<T> : Lazy<T> where T : notnull
    {
        public Lazier(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<T>) { }
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