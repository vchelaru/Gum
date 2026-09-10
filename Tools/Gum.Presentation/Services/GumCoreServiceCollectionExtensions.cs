using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using CommunityToolkit.Mvvm.Messaging;
using Gum.CommandLine;
using Gum.Commands;
using Gum.Dialogs;
using Gum.Localization;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Plugins.AlignmentButtons;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.ProjectServices;
using Gum.ProjectServices.FontGeneration;
using Gum.PropertyGridHelpers;
using Gum.Reflection;
using Gum.SelectionHistory;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumFormsPlugin;
using GumFormsPlugin.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Gum.Services;

/// <summary>
/// Registers the tool's headless service graph: every service whose implementation lives in this
/// assembly, <c>Gum.ProjectServices</c>, or <c>GumCommon</c>. A UI head (WPF today, Avalonia next)
/// calls <see cref="AddGumCore"/> and then registers its own implementations of the seam
/// interfaces listed in <see cref="HeadProvidedContracts"/>. Nothing here references a UI framework.
/// </summary>
public static class GumCoreServiceCollectionExtensions
{
    /// <summary>
    /// The contracts <see cref="AddGumCore"/> consumes but does not register, because their
    /// implementations are framework-specific or still live in the WPF project. Every head must
    /// register each of these, and the headless composition test stubs them.
    /// </summary>
    public static readonly IReadOnlyList<Type> HeadProvidedContracts = new[]
    {
        typeof(IDispatcher),
        typeof(IDialogService),
        typeof(IDeleteDialogService),
        typeof(IClipboardService),
        typeof(ISpinnerFactory),
        typeof(IAppScaleProvider),
        typeof(IThemingService),
        typeof(ITabManager),
        typeof(IModifierKeyState),
        typeof(IRecycleBinService),
        typeof(IFilePickingFolderProvider),
        typeof(IVariableTypeConverterProvider),
        typeof(ICompositeMemberRegistry),
        typeof(IGuiCommands),
        typeof(IPluginManager),
        typeof(IUndoPluginNotifier),
        typeof(IDeletePluginNotifier),
        typeof(ICopyPastePluginNotifier),
        typeof(IRenamePluginNotifier),
        typeof(IStandardElementsManagerGumTool),
        typeof(IBehaviorVariablePropertyGridSink),
    };

    /// <summary>Adds the headless service graph. See the class summary for what the head must add.</summary>
    public static IServiceCollection AddGumCore(this IServiceCollection services)
    {
        // transients
        services.ForEachConcreteTypeAssignableTo<ViewModel>(
            typeof(GumCoreServiceCollectionExtensions).Assembly,
            static (isp, type) => isp.AddTransient(type));
        services.AddTransient(typeof(Lazy<>), typeof(Lazier<>));
        services.AddTransient<PeriodicUiTimer>();

        // static singletons
        services.AddSingleton<IObjectFinder>(ObjectFinder.Self);
        // PluginEnablementStore: the user-disabled-plugins persistence PluginManager used to own via a
        // static field (#3880) - plain string plugin ids, no MEF/WinForms types.
        services.AddSingleton<IPluginEnablementStore, PluginEnablementStore>();
        services.AddSingleton<TypeManager>();
        services.AddSingleton<ITypeManager>(provider => provider.GetRequiredService<TypeManager>());
        services.AddSingleton<ProjectManager>();
        services.AddSingleton<IProjectManager>(provider => provider.GetRequiredService<ProjectManager>());
        // Narrow ports over ProjectManager (ADR-0005 Phase 3): each resolves to the same singleton.
        services.AddSingleton<IDeleteProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        services.AddSingleton<ICopyPasteProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        services.AddSingleton<IReferenceFinderProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        services.AddSingleton<IRenameProjectProvider>(provider => provider.GetRequiredService<ProjectManager>());
        services.AddSingleton<ICommandLineManager, CommandLineManager>();
        services.AddSingleton<IProjectState, ProjectState>();

        // singletons
        services.AddSingleton<ICircularReferenceManager, CircularReferenceManager>();
        services.AddSingleton<IFavoriteComponentManager, FavoriteComponentManager>();
        services.AddSingleton<ICopyPasteLogic, CopyPasteLogic>();
        services.AddSingleton<IDeleteLogic, DeleteLogic>();
        services.AddSingleton<IGumProjectRepairLogic, GumProjectRepairLogic>();
        services.AddSingleton<ISkiaShapeStandardsLogic, SkiaShapeStandardsLogic>();
        // Forms theme import is app-wide rather than GumFormsPlugin-owned: new-project creation
        // imports the default theme without going through the plugin's Add Forms dialog.
        services.AddSingleton<IFormsFileService, FormsFileService>();
        services.AddSingleton<IFormsThemeImporter, FormsThemeImporter>();
        services.AddSingleton<GumFormsLogic>();
        services.AddSingleton<INewProjectLogic, NewProjectLogic>();
        services.AddSingleton<FileLocations>();
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
        // itself as the real provider in its StartUp. Both interfaces resolve to the one relay singleton.
        services.AddSingleton<AnimationUndoProviderRelay>();
        services.AddSingleton<IAnimationUndoProvider>(provider => provider.GetRequiredService<AnimationUndoProviderRelay>());
        services.AddSingleton<IAnimationUndoProviderRegistrar>(provider => provider.GetRequiredService<AnimationUndoProviderRelay>());
        services.AddSingleton<EditVariableService>();
        services.AddSingleton<IEditVariableService>(provider => provider.GetRequiredService<EditVariableService>());
        services.AddSingleton<IDeleteVariableService, DeleteVariableService>();
        services.AddSingleton<IExposeVariableService, ExposeVariableService>();
        services.AddSingleton<IStateEditingIndicatorService, StateEditingIndicatorService>();
        services.AddSingleton<IDragDropManager, DragDropManager>();
        services.AddSingleton<IProjectFileDropLogic, ProjectFileDropLogic>();
        services.AddSingleton<IScreenImportService, ScreenImportService>();
        services.AddSingleton<ImportLogic>();
        services.AddSingleton<IImportLogic>(provider => provider.GetRequiredService<ImportLogic>());
        services.AddSingleton<MainOutputViewModel>();

        // WireframeObjectManager's concrete type is needed for the Initialize() call in the startup sequence.
        services.AddSingleton<IWireframeObjectManager, WireframeObjectManager>();
        services.AddSingleton<IOutputManager>(provider => provider.GetRequiredService<MainOutputViewModel>());
        services.AddSingleton<FileWatchIgnoreList>();
        services.AddSingleton<IFileWatchIgnoreList>(provider => provider.GetRequiredService<FileWatchIgnoreList>());
        services.AddSingleton<ICsvLocalizationLoader, CsvLocalizationLoader>();
        services.AddSingleton<FileWatchManager>();
        services.AddSingleton<IFileWatchManager>(provider => provider.GetRequiredService<FileWatchManager>());
        services.AddSingleton<ReorderLogic>();
        services.AddSingleton<IReorderLogic>(provider => provider.GetRequiredService<ReorderLogic>());
        services.AddSingleton<InheritanceLogic>();

        services.AddSingleton<IUserProjectSettingsManager, UserProjectSettingsManager>();
        services.AddSingleton<ProjectServices.ITypeResolver>(provider =>
            new TypeManagerTypeResolverAdapter(provider.GetRequiredService<ITypeManager>()));
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
        // IUndoRenameLogic: narrow headless rename port (ADR-0005 Phase 3). Resolves to the same RenameLogic singleton.
        services.AddSingleton<IUndoRenameLogic>(provider => provider.GetRequiredService<RenameLogic>());
        services.AddSingleton<ISetVariableLogic, SetVariableLogic>();
        services.AddSingleton<CommonControlLogic>();

        services.AddSingleton<WireframeCommands>();
        services.AddSingleton<IWireframeCommands>(provider => provider.GetRequiredService<WireframeCommands>());
        services.AddSingleton<IEditCommands, EditCommands>();
        services.AddSingleton<IVariableInCategoryPropagationLogic, VariableInCategoryPropagationLogic>();
        services.AddSingleton<IFileSystemRevealService, FileSystemRevealService>();
        services.AddSingleton<IElementCommands, ElementCommands>();
        services.AddSingleton<IFileCommands, FileCommands>();
        services.AddSingleton<FileChangeReactionLogic>();
        services.AddSingleton<ProjectCommands>();
        // ICopyPasteProjectCommands: narrow headless port (ADR-0005 Phase 3). Resolves to the same ProjectCommands singleton.
        services.AddSingleton<ICopyPasteProjectCommands>(provider => provider.GetRequiredService<ProjectCommands>());

        services.AddSingleton<IMessenger>(_ => WeakReferenceMessenger.Default);

        // A service that takes a Func<SomeViewModel> needs the factory registered from the assembly
        // that declares the ctor, or the container never learns how to build it.
        services.AddViewModelFuncFactories(typeof(GumCoreServiceCollectionExtensions).Assembly);
        services.AddSingleton<IUiSettingsService, UiSettingsService>();

        return services;
    }

    /// <summary>
    /// Invokes <paramref name="callback"/> for every concrete, non-generic class in
    /// <paramref name="assembly"/> assignable to <typeparamref name="TBaseType"/> that has a public ctor.
    /// </summary>
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

    /// <summary>
    /// Registers a transient <c>Func&lt;..., TViewModel&gt;</c> for every public ctor parameter of that
    /// shape found in <paramref name="targetAssembly"/>, so services can create ViewModels on demand.
    /// </summary>
    public static IServiceCollection AddViewModelFuncFactories(this IServiceCollection services, Assembly targetAssembly)
    {
        Type[] allTypes = targetAssembly.GetTypes();

        foreach (Type type in allTypes)
        {
            if (!type.IsClass || type.IsAbstract)
            {
                continue;
            }

            foreach (ConstructorInfo ctor in type.GetConstructors(BindingFlags.Public | BindingFlags.Instance))
            {
                foreach (ParameterInfo param in ctor.GetParameters())
                {
                    if (!IsFuncType(param.ParameterType))
                    {
                        continue;
                    }

                    Type[] funcArgs = param.ParameterType.GetGenericArguments();
                    Type resultType = funcArgs.Last();

                    if (!typeof(ViewModel).IsAssignableFrom(resultType))
                    {
                        continue;
                    }

                    RegisterFuncFactory(services, param.ParameterType, funcArgs);
                }
            }
        }

        return services;
    }

    private static bool IsFuncType(Type t)
    {
        if (!t.IsGenericType)
        {
            return false;
        }
        Type? def = t.GetGenericTypeDefinition();
        return def.FullName!.StartsWith("System.Func");
    }

    private static void RegisterFuncFactory(IServiceCollection services, Type funcType, Type[] typeArgs)
    {
        if (services.Any(sd => sd.ServiceType == funcType))
        {
            return;
        }

        Type resultType = typeArgs.Last();
        Type[] paramTypes = typeArgs.Take(typeArgs.Length - 1).ToArray();

        ObjectFactory factory = ActivatorUtilities.CreateFactory(resultType, paramTypes);

        object factoryDelegate = BuildFactoryLambda(funcType, factory, paramTypes, resultType);
        Delegate factoryFunc = (Delegate)factoryDelegate;
        services.AddTransient(funcType, sp => factoryFunc.DynamicInvoke(sp)!);
    }

    private static object BuildFactoryLambda(Type funcType, ObjectFactory factory, Type[] paramTypes, Type resultType)
    {
        ParameterExpression spParam = Expression.Parameter(typeof(IServiceProvider), "sp");

        ParameterExpression[] delegateParams = paramTypes.Select(Expression.Parameter).ToArray();

        NewArrayExpression argsArray = Expression.NewArrayInit(typeof(object),
            delegateParams.Select(p => Expression.Convert(p, typeof(object))));

        MethodCallExpression factoryCall = Expression.Call(
            Expression.Constant(factory),
            typeof(ObjectFactory).GetMethod("Invoke")!,
            spParam,
            argsArray);

        UnaryExpression castResult = Expression.Convert(factoryCall, resultType);

        LambdaExpression innerLambda = Expression.Lambda(funcType, castResult, delegateParams);

        Type outerFuncType = typeof(Func<,>).MakeGenericType(typeof(IServiceProvider), funcType);
        return Expression.Lambda(outerFuncType, innerLambda, spParam).Compile();
    }

    private class Lazier<T> : Lazy<T> where T : notnull
    {
        public Lazier(IServiceProvider serviceProvider) : base(serviceProvider.GetRequiredService<T>) { }
    }
}
