// Usings mirror Gum.Plugins.PluginManager so the bridged-service typeofs below resolve against the same
// symbols PluginManager.LoadPlugins uses.
using System;
using Gum.Wireframe;
using Gum.ToolStates;
using Gum.Managers;
using Gum.Services;
using Gum.Reflection;
using Gum.ToolCommands;
using Gum.Commands;
using CommunityToolkit.Mvvm.Messaging;
using Gum.Services.Dialogs;
using Gum.Dialogs;
using Gum.Undo;
using Gum.Localization;
using Gum.Services.Fonts;
using Gum.Plugins.ImportPlugin.Manager;
using Gum.Logic;
using Gum.Logic.FileWatch;
using Gum.Controls;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.InternalPlugins.Hotkey.ViewModels;
using Gum.Plugins.InternalPlugins.TreeView;
using Gum.PropertyGridHelpers;
using Gum.ViewModels;
using Gum.Plugins;

namespace GumToolUnitTests.Plugins;

/// <summary>
/// The full set of services <see cref="Gum.Plugins.PluginManager"/>.LoadPlugins bridges into the plugin
/// <c>CompositionContainer</c>, in source order. Single source of truth shared by
/// <see cref="AllPluginsCompositionTests"/> (which supplies stubs for them to MEF) and
/// <see cref="ServiceProviderCompositionSpikeTests"/> (which resolves them from the real container).
///
/// This hand-duplicates the list inside <c>LoadPlugins</c>. The intended end state is to extract an internal
/// <c>ComposePlugins(...)</c> from <c>LoadPlugins</c> so both the production code and these tests share one
/// list and this array disappears — tracked as a FOLLOW-UP PR. Until then, a drain that bridges a new
/// service in <c>LoadPlugins</c> must add it here too, or the composition test goes red.
/// </summary>
internal static class PluginBridgedServiceTypes
{
    internal static readonly Type[] All =
    {
        typeof(ISelectedState),
        typeof(IElementCommands),
        typeof(IUndoManager),
        typeof(IProjectManager),

        typeof(IGuiCommands),
        typeof(IFileCommands),
        typeof(ITabManager),
        typeof(MenuStripManager),
        typeof(IDialogService),

        typeof(IWireframeCommands),
        typeof(IFontManager),
        typeof(IProjectState),
        typeof(IImportLogic),
        typeof(IFileWatchManager),

        typeof(InheritanceLogic),
        typeof(IFavoriteComponentManager),

        typeof(MainPanelViewModel),
        typeof(PropertyGridManager),
        typeof(IVariableReferenceLogic),
        typeof(IErrorChecker),
        typeof(IMessenger),
        typeof(FileWatchLogic),
        typeof(PeriodicUiTimer),

        typeof(IDeleteLogic),
        typeof(HotkeyViewModel),
        typeof(MainOutputViewModel),

        typeof(IDispatcher),
        typeof(IWireframeObjectManager),

        typeof(ISetVariableLogic),
        typeof(IHotkeyManager),
        typeof(IEditCommands),
        typeof(ICopyPasteLogic),
        typeof(IVariableInCategoryPropagationLogic),

        typeof(ElementTreeViewManager),
        typeof(IUserProjectSettingsManager),
        typeof(IOutputManager),

        typeof(INameVerifier),
        typeof(ITypeManager),
        typeof(LocalizationService),
        typeof(IRetryService),

        // MainEditorTabPlugin drain (#3331). WireframeCommands is the concrete (BackgroundManager's ctor
        // takes it), distinct from the IWireframeCommands interface bridged above though it resolves to the
        // same singleton.
        typeof(IReorderLogic),
        typeof(FileLocations),
        typeof(IUiSettingsService),
        typeof(IThemingService),
        typeof(IDragDropManager),
        typeof(WireframeCommands),

        // EditingManager drain (#3338): ICircularReferenceManager is bridged for the EditingManager that
        // MainEditorTabPlugin constructs (its other drained dep, IFavoriteComponentManager, is above).
        typeof(ICircularReferenceManager),

        // Animation undo (#3406): MainStateAnimationPlugin injects this to register itself as the live
        // IAnimationUndoProvider with UndoManager/ElementUndoStrategy.
        typeof(IAnimationUndoProviderRegistrar),

        // MainWindowPlugin ctor drain (#3753): updates the main window title on project load/save.
        typeof(MainWindowViewModel),

        // PluginManager self-injection drain (#3753): re-investigated the "cycle smell" assumption and
        // found no real construction cycle (see LoadPlugins for the reasoning). Concrete PluginManager
        // feeds MainEditorTabPlugin/MainBehaviorsPlugin; IPluginManager feeds MainPropertiesWindowPlugin.
        typeof(PluginManager),
        typeof(IPluginManager),
    };
}
