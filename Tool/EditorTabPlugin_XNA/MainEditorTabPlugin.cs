using CommonFormsAndControls;
using CommunityToolkit.Mvvm.Messaging;
using EditorTabPlugin_XNA.Services;
using EditorTabPlugin_XNA.ViewModels;
using EditorTabPlugin_XNA.Views;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Input;
using Gum.Localization;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Plugins.ScrollBarPlugin;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
using Gum.Services.Fonts;
using Gum.Settings;
using Gum.Themes;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Extensions.Options;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Linq;
using System.Numerics;
using System.Runtime;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.Windows.Markup;
using ToolsUtilities;
using DialogResult = System.Windows.Forms.DialogResult;

namespace Gum.Plugins.InternalPlugins.EditorTab;

[Export(typeof(PluginBase))]
#pragma warning disable CA1001 // Types that own disposable fields should be disposable - This is never disposed so suppressing this
internal class MainEditorTabPlugin : PriorityPlugin, IRecipient<UiBaseFontSizeChangedMessage>, IRecipient<ThemeChangedMessage>
#pragma warning restore CA1001 // Types that own disposable fields should be disposable
{
    #region Fields/Properties

    #region PropertiesSupportingIncrementalChange

    HashSet<string> PropertiesSupportingIncrementalChange = new HashSet<string>
        {
            "Animate",
            "Alpha",
            "AutoGridHorizontalCells",
            "AutoGridVerticalCells",
            "Blue",
            "CurrentChainName",
            "ChildrenLayout",
            "FlipHorizontal",
            "Font",
            "FontSize",
            "Green",
            "Height",
            "HeightUnits",
            "HorizontalAlignment",
            nameof(GraphicalUiElement.IgnoredByParentSize),
            "IsBold",
            "IsRenderTarget",
            "MaxLettersToShow",
            nameof(GraphicalUiElement.MaxHeight),
            nameof(Text.MaxNumberOfLines),
            nameof(GraphicalUiElement.MaxWidth),
            nameof(GraphicalUiElement.MinHeight),
            nameof(GraphicalUiElement.MinWidth),
            "Red",
            "Rotation",
            "SourceFile",
            "StackSpacing",
            "Text",
            "TextureAddress",
            "TextOverflowVerticalMode",
            "UseCustomFont",
            "UseFontSmoothing",
            "VerticalAlignment",
            "Visible",
            "Width",
            "WidthUnits",
            "X",
            "XOrigin",
            "XUnits",
            "Y",
            "YOrigin",
            "YUnits",
        };
    #endregion


    readonly ScrollbarService _scrollbarService;
    private readonly IGuiCommands _guiCommands;
    private readonly IOutputManager _outputManager;
    private readonly LocalizationService _localizationService;
    private readonly ScreenshotService _screenshotService;
    private readonly SelectionManager _selectionManager;
    private readonly IElementCommands _elementCommands;
    private readonly SinglePixelTextureService _singlePixelTextureService;
    private BackgroundManager _backgroundManager;
    private readonly ISelectedState _selectedState;
    private readonly WireframeCommands _wireframeCommands;
    private readonly IFileCommands _fileCommands;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly IUiSettingsService _uiSettingsService;
    private readonly IProjectManager _projectManager;
    private EditorViewModel _editorViewModel;
    private readonly FileLocations _fileLocations;
    private readonly IThemingService _themingService;
    private IDragDropManager _dragDropManager;
    WireframeControl _wireframeControl;

    private EditorControls _editorControls;

    System.Windows.Forms.Panel gumEditorPanel;
    private LayerService _layerService;
    private System.Windows.Controls.ContextMenu _wireframeContextMenu;
    private EditingManager _editingManager;
    private readonly IDialogService _dialogService;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly ICircularReferenceManager _circularReferenceManager;
    private readonly IFavoriteComponentManager _favoriteComponentManager;
    private readonly IToolFontService _toolFontService;
    private readonly IToolLayerService _toolLayerService;
    private readonly IPluginManager _pluginManager;
    private IWireframeEditorFactory _wireframeEditorFactory;

    // Suppresses the redundant second wireframe rebuild when selecting an element forces its
    // default state (state event rebuilds) and then fires the element event for the same element.
    private readonly WireframeRefreshCoordinator _wireframeRefreshCoordinator = new();


    // This is used to punch through the selected and go back up to the top. More info here:
    // https://github.com/vchelaru/Gum/issues/1810
    public bool IsComponentNoInstanceSelected => _selectedState.SelectedInstance == null && _selectedState.SelectedComponent != null;

    #endregion

    [ImportingConstructor]
    public MainEditorTabPlugin(
        ISelectedState selectedState,
        IProjectManager projectManager,
        IGuiCommands guiCommands,
        IOutputManager outputManager,
        LocalizationService localizationService,
        IReorderLogic reorderLogic,
        INameVerifier nameVerifier,
        IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic,
        IWireframeObjectManager wireframeObjectManager,
        FileLocations fileLocations,
        IUndoManager undoManager,
        IDialogService dialogService,
        IHotkeyManager hotkeyManager,
        IElementCommands elementCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        IUiSettingsService uiSettingsService,
        WireframeCommands wireframeCommands,
        IMessenger messenger,
        IThemingService themingService,
        IDragDropManager dragDropManager,
        ICircularReferenceManager circularReferenceManager,
        IFavoriteComponentManager favoriteComponentManager,
        IPluginManager pluginManager)
    {
        _selectedState = selectedState;
        _projectManager = projectManager;
        _guiCommands = guiCommands;
        _outputManager = outputManager;
        _localizationService = localizationService;
        _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
        _wireframeObjectManager = wireframeObjectManager;
        _fileLocations = fileLocations;
        _dialogService = dialogService;
        _hotkeyManager = hotkeyManager;
        _elementCommands = elementCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _uiSettingsService = uiSettingsService;
        _wireframeCommands = wireframeCommands;
        _themingService = themingService;
        _dragDropManager = dragDropManager;
        _circularReferenceManager = circularReferenceManager;
        _favoriteComponentManager = favoriteComponentManager;
        _pluginManager = pluginManager;

        _scrollbarService = new ScrollbarService(_selectedState, _wireframeObjectManager, _projectManager);
        _editingManager = new EditingManager(
            _wireframeObjectManager,
            reorderLogic,
            _elementCommands,
            nameVerifier,
            _setVariableLogic,
            _selectedState,
            _circularReferenceManager,
            _favoriteComponentManager
            );

        _layerService = new Services.LayerService();

        // Plugin-scoped services (not registered in Builder.cs): the editor tab owns the single
        // instance of each and threads it down to the XNA objects it constructs. See issue #3294.
        _toolFontService = new ToolFontService();
        _toolLayerService = new ToolLayerService();

        _wireframeEditorFactory = new WireframeEditorFactory(
            _hotkeyManager,
            _selectedState,
            _elementCommands,
            _guiCommands,
            _fileCommands,
            _setVariableLogic,
            undoManager,
            _variableInCategoryPropagationLogic,
            _wireframeObjectManager,
            _uiSettingsService,
            _toolFontService,
            _pluginManager,
            _projectManager);

        _selectionManager = new SelectionManager(
            _selectedState,
            undoManager,
            _editingManager,
            _dialogService,
            _hotkeyManager,
            _wireframeObjectManager,
            _guiCommands,
            _wireframeEditorFactory,
            new NineSliceCoordinateRefresher(),
            new PreciseHitTester());

        _screenshotService = new ScreenshotService(_selectionManager, _wireframeCommands, _guiCommands);
        _singlePixelTextureService = new SinglePixelTextureService();
        _backgroundManager = new BackgroundManager(_wireframeCommands, messenger, _themingService);

        _editorViewModel = new EditorViewModel(
            _pluginManager,
            _fileCommands,
            _wireframeObjectManager);

        messenger.RegisterAll(this);
    }

    public override void StartUp()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
        GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;
        GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
        GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;
        CustomSetPropertyOnRenderable.FontService = Locator.GetRequiredService<IFontManager>();
        // Gum core ships no shader loader, so the tool registers a resolver that compiles a
        // render-target Container's SourceShaderFile (.fx) into an Effect at runtime (ShadowDusk),
        // letting the WYSIWYG preview render shaded containers. Mirrors FontService above.
        // Wrapped so a successful compile is reported to the Output window — the resolver only
        // surfaces failures (via PropertyAssignmentError), so without this a working shader is
        // silent and users can't tell it actually compiled.
        CustomSetPropertyOnRenderable.RenderTargetEffectResolver = ResolveAndReportRenderTargetShader;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += HandlePropertyAssignmentError;

        AssignEvents();

        var menuItem = AddMenuItem("File", "Export", "Export as Image");
        _screenshotService.InitializeMenuItem(menuItem);
        BeforeRender += _screenshotService.HandleBeforeRender;
        AfterRender += _screenshotService.HandleAfterRender;

        HandleWireframeInitialized();

    }

    private void HandlePropertyAssignmentError(string obj)
    {
        _guiCommands.PrintOutput(obj);
    }

    // Compiles the .fx via the tool-side resolver and, on success, reports it to the Output window.
    // The resolver only ever logs failures (it throws, which AssignSourceShaderFileOnContainer turns
    // into a PropertyAssignmentError), so a working shader would otherwise compile silently. A
    // successful return is logged here; failures keep propagating to the existing error path. Note
    // the underlying effect is cached by path in LoaderManager, so this fires once per actual
    // compile (a cache hit skips the resolver), which is exactly the "it compiled" signal we want.
    private object? ResolveAndReportRenderTargetShader(string absolutePath)
    {
        object? effect = EditorTabPlugin_XNA.Services.RenderTargetShaderResolver.Resolve(absolutePath);
        if (effect != null)
        {
            _guiCommands.PrintOutput($"Compiled render-target shader for the preview:\n{absolutePath}");
        }
        return effect;
    }

    private void AssignEvents()
    {
        this.CreateGraphicalUiElement += HandleCreateGraphicalUiElement;

        this.ReactToStateSaveSelected += HandleStateSelected;

        this.InstanceSelected += HandleInstanceSelected;
        this.InstanceReordered += HandleInstanceReordered;
        this.InstanceDelete += HandleInstanceDelete;

        this.ElementSelected += HandleElementSelected;
        this.ElementSelected += _scrollbarService.HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;

        this.BehaviorSelected += HandleBehaviorSelected;

        this.VariableSet += HandleVariableSet;
        this.VariableSetLate += HandleVariableSetLate;

        this.CategoryDelete += HandleCategoryDelete;
        this.TryHandleDelete += HandleDelete;

        this.StateDelete += HandleStateDelete;

        this.CameraChanged += _scrollbarService.HandleCameraChanged;

        this.XnaInitialized += HandleXnaInitialized;
        
        this.WireframeResized += _scrollbarService.HandleWireframeResized;
        this.WireframeRefreshed += HandleWireframeRefreshed;
        this.WireframePropertyChanged += HandleWireframePropertyChanged;

        this.GetWorldCursorPosition += HandleGetWorldCursorPosition;

        this.IpsoSelected += HandleIpsoSelected;
        this.SetHighlightedIpso += HandleSetHighlightedElement;
        _selectionManager.HighlightedIpsoChanged += HandleHighlightedIpsoChanged;

        this.ProjectLoad += HandleProjectLoad;
        this.ProjectPropertySet += HandleProjectPropertySet;

        this.CreateRenderableForType += HandleCreateRenderableForType;
        this.GetSelectedIpsos += HandleGetSelectedIpsos;

        this.AfterUndo += HandleAfterUndo;

    }

    private void HandleBehaviorSelected(BehaviorSave? save)
    {
        _wireframeObjectManager.RefreshAll(false);
    }

    private Vector2? HandleGetWorldCursorPosition(IGumCursorState cursor)
    {
        Renderer.Self.Camera.ScreenToWorld(cursor.X, cursor.Y,
                                   out float worldX, out float worldY);

        return new Vector2(worldX, worldY);
    }

    List<IPositionedSizedObject> ipsosToReturn = new List<IPositionedSizedObject>();
    private IEnumerable<IPositionedSizedObject>? HandleGetSelectedIpsos()
    {
        ipsosToReturn.Clear();

        if (_selectedState.SelectedInstance != null)
        {
            foreach (var instance in _selectedState.SelectedInstances)
            {
                var representation = _wireframeObjectManager.GetRepresentation(instance);
                if (representation != null)
                {
                    ipsosToReturn.Add(representation);
                }
            }
        }
        else if (_selectedState.SelectedElement != null)
        {
            var representation = _wireframeObjectManager.GetRepresentation(_selectedState.SelectedElement);
            if (representation != null)
            {
                ipsosToReturn.Add(representation);
            }
        }

        return ipsosToReturn;

    }

    private IRenderableIpso? HandleCreateRenderableForType(string type)
    {
        return FallbackRenderableFactory.TryHandleAsBaseType(type, SystemManagers.Default) as IRenderableIpso;
    }

    private GraphicalUiElement? HandleCreateGraphicalUiElement(ElementSave elementSave)
    {
        var toReturn = elementSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        toReturn.AddToManagers(SystemManagers.Default, _layerService.MainEditorLayer);
        UpdateTextOutlines(toReturn);
        return toReturn;
    }


    private void UpdateTextOutlines(GraphicalUiElement rootGue)
    {
        if (rootGue.Component is Text text)
        {
            text.RenderBoundary = _projectManager.ShowTextOutlines;
        }
        if (rootGue.Children != null)
        {
            foreach (var child in rootGue.Children)
            {
                if (child is GraphicalUiElement gue)
                {
                    UpdateTextOutlines(gue);
                }
            }
        }
        else
        {
            foreach (var child in rootGue.ContainedElements)
            {
                UpdateTextOutlines(child);
            }
        }
    }

    private bool HandleDelete()
    {
        return _selectionManager.TryHandleDelete();
    }

    private void HandleWireframeRefreshed()
    {
        _editorViewModel.RefreshCanvasSize();

        _wireframeControl.UpdateCanvasBoundsToProject();

        _selectionManager.Refresh();
    }

    private void HandleAfterUndo()
    {
        _selectionManager.Refresh();

        // reset everything. This is slow, but is easy
        _wireframeObjectManager.RefreshAll(true);
    }

    private void HandleIpsoSelected(IPositionedSizedObject ipso)
    {
        _selectionManager.SelectedGue = ipso as GraphicalUiElement;
    }

    private void HandleVariableSet(ElementSave save1, InstanceSave save2, string arg3, object arg4)
    {
        _selectionManager.Refresh();

    }

    private void HandleSetHighlightedElement(GraphicalUiElement? whatToHighlight)
    {
        _selectionManager.HighlightedIpso = whatToHighlight;
    }

    private void HandleHighlightedIpsoChanged(IPositionedSizedObject? ipso)
    {
        _pluginManager.HighlightTreeNode(ipso);
    }

    private void HandleStateDelete(StateSave save)
    {
        _selectionManager.Refresh();

    }

    private void HandleCategoryDelete(StateSaveCategory category)
    {
        _selectionManager.Refresh();
    }

    private void HandleInstanceDelete(ElementSave save1, InstanceSave save2)
    {
        _selectionManager.Refresh();
    }

    private void HandleInstanceReordered(InstanceSave save)
    {
        _selectionManager.Refresh();

        _wireframeObjectManager.RefreshAll(true);
    }

    private void HandleWireframePropertyChanged(string name)
    {
        if(name == nameof(WireframeCommands.AreHighlightsVisible))
        {
            _selectionManager.AreHighlightsVisible = 
                _wireframeCommands.AreHighlightsVisible;
        }
        else if(name == nameof(WireframeCommands.AreRulersVisible))
        {
            _wireframeControl.RulersVisible =
                _wireframeCommands.AreRulersVisible;
        }
        else if(name == nameof(WireframeCommands.AreCanvasBoundsVisible))
        {
            _wireframeControl.CanvasBoundsVisible =
                _wireframeCommands.AreCanvasBoundsVisible;
        }
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        _editorViewModel.HandleProjectLoad(save);

        _wireframeControl.UpdateCanvasBoundsToProject();

        _selectionManager.RestrictToUnitValues =
            save.RestrictToUnitValues;

        _wireframeCommands.IsBackgroundGridVisible =
            save.ShowCheckerBackground;

        _wireframeObjectManager.RefreshAll(true);

        AdjustTextureFilter();
    }

    private void HandleProjectPropertySet(string propertyName)
    {
        if(propertyName == nameof(GumProjectSave.TextureFilter))
        {
            AdjustTextureFilter();
        }
        if (propertyName == nameof(GumProjectSave.RestrictToUnitValues))
        {
            _selectionManager.RestrictToUnitValues =
                _projectManager.GumProjectSave.RestrictToUnitValues;
        }
        else if (propertyName == nameof(GumProjectSave.ShowCheckerBackground))
        {
            _wireframeCommands.IsBackgroundGridVisible =
                _projectManager.GumProjectSave.ShowCheckerBackground;
        }
        else if (propertyName == nameof(GumProjectSave.SinglePixelTextureFile) ||
            propertyName == nameof(GumProjectSave.SinglePixelTextureTop) ||
            propertyName == nameof(GumProjectSave.SinglePixelTextureLeft) ||
            propertyName == nameof(GumProjectSave.SinglePixelTextureRight) ||
            propertyName == nameof(GumProjectSave.SinglePixelTextureBottom))
        {
            _singlePixelTextureService.RefreshSinglePixelTexture();

            _wireframeObjectManager.RefreshAll(forceLayout: true, forceReloadTextures: true);
        }
    }

    private void AdjustTextureFilter()
    {
        var project = ObjectFinder.Self.GumProjectSave;

        if (project != null)
        {
            switch(project.TextureFilter)
            {
                case nameof(TextureFilter.Linear):
                    _layerService.MainEditorLayer.IsLinearFilteringEnabled = true;
                    break;
                case nameof(TextureFilter.Point):
                default:
                    _layerService.MainEditorLayer.IsLinearFilteringEnabled = false;

                    break;
            }
        }
    }

    private void HandleElementDeleted(ElementSave save)
    {
        _wireframeObjectManager.RefreshAll(true);
    }

    void IRecipient<UiBaseFontSizeChangedMessage>.Receive(UiBaseFontSizeChangedMessage message)
    {
        _editorControls?.UpdateButtonSizes(message.Size);
    }

    private void HandleVariableSetLate(ElementSave? element, InstanceSave instance, string unqualifiedName, object oldValue)
    {
        /////////////////////////////Early Out//////////////////////////
        if(element == null)
        {
            // This could be a variable on a behavior or instance in a behavior. If so, we don't show anything in the editor
            return;
        }
        ////////////////////////////End Early Out///////////////////////

        var qualifiedName = unqualifiedName;
        if (instance != null)
        {
            qualifiedName = instance.Name + "." + qualifiedName;
        }

        var state = _selectedState.SelectedStateSave ?? element.DefaultState;

        // This method could be called...
        // 1. Directly on an element or instance when the user edits a value
        // 2. Indirectly, as a result of a variable reference
        // If it's (2), then that means the element that is being
        // edited may not be the current element, and in that case
        // we shouldn't use _selectedState.SelectedStateSave.
        if(_selectedState.SelectedElements.Contains(element) == false)
        {
            state = element.DefaultState;
        }

        var value = state.GetValue(qualifiedName);

        var areSame = value == null && oldValue == null;
        if (!areSame && value != null)
        {
            areSame = value.Equals(oldValue);
        }

        var unqualifiedMember = qualifiedName;
        if(qualifiedName.Contains("."))
        {
            unqualifiedMember = qualifiedName.Substring(qualifiedName.LastIndexOf('.') + 1);
        }

        // Inefficient but let's do this for now - we can make it more efficient later
        // November 19, 2019
        // While this is inefficient
        // at runtime, it is *really*
        // inefficient for debugging. If
        // a set value fails, we have to trace
        // the entire variable assignment and that
        // can take forever. Therefore, we're going to
        // migrate towards setting the individual values
        // here. This can expand over time to just exclude
        // the RefreshAll call completely....but I don't know
        // if that will cause problems now, so instead I'm going
        // to do it one by one:
        var handledByDirectSet = false;

        var supportsIncrementalChange = PropertiesSupportingIncrementalChange.Contains(unqualifiedMember);

        // A Forms-promoted alias (e.g. Spacing, Orientation) is not itself a visual property, but a
        // behavior ToolOnlyVariableReference drives one or more underlying visual variables from it
        // (Spacing -> StackSpacing, Orientation -> ChildrenLayout). Those underlying variables were
        // just materialized into the state by BehaviorToolOnlyReferencesApplier and are themselves
        // incrementally updatable, so resolve them and take the fast in-place path instead of a full
        // RefreshAll on every scrub tick (issue #3191).
        var aliasDrivenMembers = BehaviorToolOnlyReferencesApplier
            .GetUnderlyingMembersDrivenBy(element, instance, unqualifiedMember)
            .Where(PropertiesSupportingIncrementalChange.Contains)
            .ToList();

        var canIncrementallyUpdate = supportsIncrementalChange || aliasDrivenMembers.Count > 0;

        // If the values are the same they may have been set to be the same by a plugin that
        // didn't allow the assignment, so don't go through the work of saving and refreshing.
        // Update January 19, 2025 - actually for incrmeental changes just use it, it will be fast
        if (!areSame || canIncrementallyUpdate)
        {

            // if a deep reference is set, then this is more complicated than a single variable assignment, so we should
            // force everything. This makes debugging a little more difficult, but it keeps the wireframe accurate without having to track individual assignments.
            if (canIncrementallyUpdate &&
            // June 19, 2024 - if the value is null (from default assignment), we
            // can't set this single value - it requires a recursive variable finder.
            // for simplicity (for now?) we will just refresh all:
                value != null &&

                (instance != null || _selectedState.SelectedComponent != null || _selectedState.SelectedStandardElement != null))
            {
                // this assumes that the object having its variable set is the selected instance. If we're setting
                // an exposed variable, this is not the case - the object having its variable set is actually the instance.
                //GraphicalUiElement gue = _wireframeObjectManager.GetSelectedRepresentation();
                GraphicalUiElement? gue = null;
                if (instance != null)
                {
                    gue = _wireframeObjectManager.GetRepresentation(instance);
                }
                else
                {
                    gue = _wireframeObjectManager.GetSelectedRepresentation();
                }

                // If we dispose a file, we should re-create the screen for sure!
                var disposedFile = false;

                if (gue != null)
                {
                    VariableSave? variable = null;
                    if(element != null)
                    {
                        variable = ObjectFinder.Self.GetRootVariable(qualifiedName, element);
                    }

                    if(variable?.IsFile == true && value is string asString)
                    {
                        try
                        {
                            var standardized =  ToolsUtilities.FileManager.Standardize(asString, preserveCase:true, makeAbsolute:true);
                            standardized = ToolsUtilities.FileManager.RemoveDotDotSlash(standardized);
                            // invalidate files...
                            var loaderManager = global::RenderingLibrary.Content.LoaderManager.Self;

                            var existing = loaderManager.GetDisposable(standardized);

                            disposedFile = existing != null;

                            loaderManager.Dispose(standardized);

                        }

                        catch
                        {
                            // this could be an invalid file name, so tolerate crashes
                        }
                    }

                    // The directly-changed member maps to a visual property only when it is itself
                    // incrementally supported; a Forms alias (e.g. Spacing) has no matching GUE property,
                    // so pushing it would be a no-op - skip it and push the underlying member(s) instead.
                    var didPush = false;
                    if (supportsIncrementalChange)
                    {
                        gue.SetProperty(unqualifiedMember, value);
                        didPush = true;
                    }

                    // Push each underlying visual variable the alias drives, using the value the
                    // behavior applier already materialized into the state. If none resolved (e.g. the
                    // edit was on a non-default state, where the applier doesn't materialize), didPush
                    // stays false so we fall back to RefreshAll rather than silently skipping the update.
                    foreach (var aliasMember in aliasDrivenMembers)
                    {
                        var aliasQualifiedName = instance != null ? instance.Name + "." + aliasMember : aliasMember;
                        var aliasValue = state.GetValue(aliasQualifiedName);
                        if (aliasValue != null)
                        {
                            gue.SetProperty(aliasMember, aliasValue);
                            didPush = true;
                        }
                    }

                    _wireframeObjectManager.RootGue?.ApplyVariableReferences(state);
                    //gue.ApplyVariableReferences(_selectedState.SelectedStateSave);

                    handledByDirectSet = didPush && !disposedFile;
                }
                if (gue != null && value is string valueAsString && unqualifiedMember == "Text" && _localizationService.HasDatabase)
                {
                    _wireframeObjectManager.ApplyLocalization(gue, valueAsString);
                }
            }

            if (!handledByDirectSet)
            {
                _wireframeObjectManager.RefreshAll(true, forceReloadTextures: false);
            }


            _selectionManager.Refresh();
        }
    }

    // When a new element is selected, its default state is selected too, so both
    // HandleStateSelected and HandleElementSelected fire in one cascade. The redundant second
    // rebuild is now suppressed via _wireframeRefreshCoordinator (issue #3212).
    private void HandleElementSelected(ElementSave save)
    {
        // Selecting an element forces its default state first, so HandleStateSelected has already
        // rebuilt the wireframe for this element earlier in the same synchronous cascade. Skip the
        // redundant rebuild here, but still refresh the selection visuals.
        if (_wireframeRefreshCoordinator.ShouldRebuildOnElementSelected(save))
        {
            _wireframeObjectManager.RefreshAll(forceLayout: true);
        }

        _selectionManager.Refresh();

    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        _wireframeObjectManager.RefreshAll(forceLayout: false);
        _editingManager.RefreshContextMenu();
        _selectionManager.WireframeEditor?.UpdateAspectRatioForGrabbedIpso();
        _selectionManager.Refresh();
    }

    private void HandleXnaInitialized()
    {
        _scrollbarService.HandleWireframeInitialized(_wireframeControl, gumEditorPanel);


        _wireframeControl.Initialize(
            gumEditorPanel,
            _hotkeyManager,
            _selectionManager,
            _dragDropManager,
            _editorViewModel,
            _projectManager,
            _toolFontService,
            _toolLayerService);
        var systemManagers = _wireframeControl.SystemManagers;


        // _layerService must be created after _wireframeControl so that the SystemManagers.Default are assigned
        _layerService.Initialize();
        _selectionManager.Initialize(
            _layerService.OverlayLayer,
            Renderer.Self.Camera,
            InputLibrary.Cursor.Self,
            new Gum.Wireframe.Editors.Visuals.SelectionRectangleVisual(_layerService.OverlayLayer),
            new GraphicalOutline(_layerService.OverlayLayer),
            new HighlightManager(_layerService.OverlayLayer));

        _wireframeControl.ShareLayerReferences(_layerService);

        // This must be initialized *after* ShareLayerReferences since that
        // creates the rulers
        _editorViewModel.InitializeXnaView(systemManagers,
            _wireframeControl.TopRuler,
            _wireframeControl.LeftRuler);

        _editingManager.Initialize(_wireframeContextMenu);

        _backgroundManager.Initialize(_wireframeControl.SystemManagers);

        _scrollbarService.HandleXnaInitialized();


        this._wireframeControl.Parent.Resize += (_, _) =>
        {
            UpdateWireframeControlSizes();
            _pluginManager.HandleWireframeResized();
        };

        //this._wireframeControl.MouseClick += wireframeControl1_MouseClick;
        this._wireframeControl.MouseDown += wireframeControl1_MouseDown;


        this._wireframeControl.DragDrop += OnWireframeDrop;
        this._wireframeControl.DragEnter += OnWireframeDragEnter;
        this._wireframeControl.DragOver += (sender, e) =>
        {
            // Intentionally does NOT set e.Effect. The effect is chosen once in
            // OnWireframeDragEnter, and WinForms carries it forward into each DragOver,
            // so the drop stays valid for the whole drag without re-asserting it here.
            // This is fine because the entire canvas accepts a drop the same way; if
            // drop validity ever becomes position-dependent, set the effect per-move
            // here the way the tree view's DragOver does.
            //this.DoDragDrop(e.Data, DragDropEffects.Move | DragDropEffects.Copy);
            //DragDropManager.Self.HandleDragOver(sender, e);

        };

        // December 29, 2024
        // AppCenter is dead - do we want to replace this?
        //_wireframeControl.ErrorOccurred += (exception) => Crashes.TrackError(exception);

        this._wireframeControl.QueryContinueDrag += (sender, args) =>
        {
            args.Action = System.Windows.Forms.DragAction.Continue;
        };
        _wireframeControl.CameraChanged += () =>
        {
            _pluginManager.CameraChanged();
        };

        this._wireframeControl.KeyDown += (o, args) =>
        {
            if (args.KeyCode == Keys.Tab)
            {
                _guiCommands.ToggleToolVisibility();
            }
        };

        // Apply FrameRate, but keep it within sane limits
        float frameRate = Math.Max(Math.Min(_projectManager.FrameRate, 60), 10);
        _wireframeControl.DesiredFramesPerSecond = frameRate;

        UpdateWireframeControlSizes();

        IEffectiveThemeSettings themeSettings = _themingService.EffectiveSettings;
        ApplyThemeSettings(themeSettings);
    }

    internal void OnWireframeDragEnter(object? sender, System.Windows.Forms.DragEventArgs e)
    {
        // WinForms glue only: inspect the drag payload, ask the neutral manager
        // whether to accept it, then apply the decision. The accept/reject logic
        // itself lives in DragDropManager.DecideWireframeDragEffect.
        bool hasFileDrop = e.Data.GetDataPresent(System.Windows.Forms.DataFormats.FileDrop);
        bool hasNodes = MultiSelectTreeView.ExtractDraggedNodes(e.Data).Length > 0;
        bool hasStandardChip = e.Data.GetDataPresent(DragDropManager.StandardElementNameDataFormat);

        DragAcceptDecision decision = _dragDropManager.DecideWireframeDragEffect(hasFileDrop, hasNodes);

        // A Standards-palette chip is accepted like a dragged Standard node — the drop creates an
        // instance of that type on the open Screen/Component at the cursor position.
        if (decision.Accept || hasStandardChip)
        {
            e.Effect = System.Windows.Forms.DragDropEffects.Copy;
        }
        else if (decision.BlockedReason != null)
        {
            _guiCommands.PrintOutput($"File drag rejected: {decision.BlockedReason}.");
        }
    }

    internal void OnWireframeDrop(object? sender, System.Windows.Forms.DragEventArgs e)
    {
        // Handle Standards-palette chip drops: create + position an instance of the chip's type,
        // reusing the same path as a dragged Standard node.
        if (e.Data.GetData(DragDropManager.StandardElementNameDataFormat) is string standardTypeName)
        {
            if (ObjectFinder.Self.GetStandardElement(standardTypeName) is { } standardElement)
            {
                _dragDropManager.OnNodeObjectDroppedInWireframe(standardElement);
            }
            return;
        }

        // Handle node drops
        TreeNode[] droppedNodes = MultiSelectTreeView.ExtractDraggedNodes(e.Data);

        if (droppedNodes.Length > 0)
        {
            foreach (var draggedObject in droppedNodes.Select(x => x.Tag))
            {
                _dragDropManager.OnNodeObjectDroppedInWireframe(draggedObject);
            }

            return;
        }

        // Handle file drops.
        // Wrapped in try/catch because exceptions thrown inside a WinForms/OLE
        // DragDrop handler are swallowed by the drag loop — a failed drop then
        // looks like drag+drop silently "stopped working". Surfacing it in the
        // output window makes the failure diagnosable (#3128).
        try
        {
            HandleWireframeFileDrop(e);
        }
        catch (Exception ex)
        {
            _outputManager.AddError($"Drag+drop failed while handling the dropped file(s): {ex}");
        }
    }

    private void HandleWireframeFileDrop(System.Windows.Forms.DragEventArgs e)
    {
        if (!CanDrop())
        {
            return;
        }

        float worldX, worldY;
        Renderer.Self.Camera.ScreenToWorld(InputLibrary.Cursor.Self.X, InputLibrary.Cursor.Self.Y, out worldX, out worldY);
        string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

        if (files == null)
        {
            _guiCommands.PrintOutput("File drop ignored: the drag payload contained no file data.");
            return;
        }

        var handled = false;
        bool shouldUpdate = false;

        // If only one file was dropped, see if we're over an instance that can take a file
        if (files.Length == 1)
        {
            if (!_dragDropManager.IsValidExtensionForFileDrop(files[0]))
            {
                _guiCommands.PrintOutput($"File drop ignored: '{System.IO.Path.GetFileName(files[0])}' is not a supported texture or font file.");
                handled = true;
            }
        }

        if (!handled)
        {
            TryHandleFileDropOnInstance(worldX, worldY, files, ref handled, ref shouldUpdate);
        }

        if (!handled)
        {
            TryHandleFileDropOnComponent(worldX, worldY, files, ref handled, ref shouldUpdate);
        }


        if (!handled)
        {
            string[] validFiles = files
                .Where(f => _dragDropManager.IsValidExtensionForFileDrop(f))
                .ToArray();

            if (validFiles.Length == 0)
            {
                _guiCommands.PrintOutput("File drop ignored: none of the dropped files are supported texture or font files.");
            }

            List<string> outsideProjectFiles = validFiles
                .Where(f => !FileManager.IsRelativeTo(f, _fileLocations.ProjectFolder))
                .ToList();

            bool cancelDrop = false;

            if (outsideProjectFiles.Count >= 2)
            {
                cancelDrop = !HandleBatchFilesOutsideProject(outsideProjectFiles, ref validFiles);
            }

            if (!cancelDrop)
            {
                foreach (string file in validFiles)
                {
                    string fileName = FileManager.MakeRelative(file, _fileLocations.ProjectFolder);
                    AddNewInstanceForDrop(fileName, worldX, worldY);
                    shouldUpdate = true;
                }

                _setVariableLogic.SetBatchFileCopyDecision(shouldCopy: null);
            }
        }
        if (shouldUpdate)
        {
            _fileCommands.TryAutoSaveCurrentElement();
            _guiCommands.RefreshVariables();

            _wireframeObjectManager.RefreshAll(true);
        }
    }

    /// <summary>
    /// Shows a single consolidated dialog for all files that are outside the project folder,
    /// then either pre-copies them into the project folder (updating <paramref name="validFiles"/>)
    /// or sets the batch copy decision so SetVariableLogic suppresses per-file dialogs.
    /// </summary>
    /// <returns>false if the user cancelled and the drop should be aborted; true otherwise.</returns>
    private bool HandleBatchFilesOutsideProject(List<string> outsideProjectFiles, ref string[] validFiles)
    {
        string fileList = string.Join("\n", outsideProjectFiles.Select(System.IO.Path.GetFileName));
        string message = $"The following {outsideProjectFiles.Count} files are outside the project folder:\n\n{fileList}\n\nWhat would you like to do?";

        DialogChoices<string> choices = new()
        {
            ["reference-current"] = "Reference all files in their current locations",
            ["copy-relative"] = "Copy all files to the Gum project folder and reference the copies"
        };

        string? result = _dialogService.ShowChoices(message, choices, canCancel: true);

        if (result == null)
        {
            return false;
        }

        if (result == "copy-relative")
        {
            List<string> updatedFiles = validFiles.ToList();
            foreach (string file in outsideProjectFiles)
            {
                string destPath = System.IO.Path.Combine(_fileLocations.ProjectFolder, System.IO.Path.GetFileName(file));
                try
                {
                    System.IO.File.Copy(file, destPath, overwrite: true);
                    int idx = updatedFiles.IndexOf(file);
                    if (idx >= 0)
                    {
                        updatedFiles[idx] = destPath;
                    }
                }
                catch (Exception ex)
                {
                    _dialogService.ShowMessage($"Error copying {System.IO.Path.GetFileName(file)}:\n{ex.Message}");
                }
            }
            validFiles = updatedFiles.ToArray();
        }
        else
        {
            // "reference-current": tell SetVariableLogic not to prompt per file
            _setVariableLogic.SetBatchFileCopyDecision(shouldCopy: false);
        }

        return true;
    }

    private string? GetBaseTypeForExtension(string fileName)
    {
        string extension = FileManager.GetExtension(fileName);

        if (extension == "svg")
        {
            if (ObjectFinder.Self.GetStandardElement("Svg") != null)
            {
                return "Svg";
            }
            return null;
        }

        if (extension == "ttf")
        {
            return "Text";
        }

        return "Sprite";
    }

    private void AddNewInstanceForDrop(string fileName, float worldX, float worldY)
    {
        string? baseType = GetBaseTypeForExtension(fileName);
        if (baseType == null)
        {
            _dialogService.ShowMessage("The Svg standard element is not available in this project. Cannot create an Svg instance.");
            return;
        }

        string nameToAdd = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

        var element = _selectedState.SelectedElement;

        IEnumerable<string> existingNames = element.Instances.Select(i => i.Name);
        nameToAdd = StringFunctions.MakeStringUnique(nameToAdd, existingNames);

        InstanceSave instance =
            _elementCommands.AddInstance(element, nameToAdd, baseType);

        _dragDropManager.SetInstanceToPosition(worldX, worldY, instance);

        if (baseType == "Text")
        {
            var variableName = instance.Name + ".Font";
            var oldValue = _selectedState.SelectedStateSave.GetValueOrDefault<string>(variableName);
            _selectedState.SelectedStateSave.SetValue(variableName, fileName, instance);
            _setVariableLogic.ReactToPropertyValueChanged("Font", oldValue, element, instance, _selectedState.SelectedStateSave, refresh: false);
        }
        else
        {
            var variableName = instance.Name + ".SourceFile";
            var oldValue = _selectedState.SelectedStateSave.GetValueOrDefault<string>(variableName);
            _selectedState.SelectedStateSave.SetValue(variableName, fileName, instance);
            _setVariableLogic.ReactToPropertyValueChanged("SourceFile", oldValue, element, instance, _selectedState.SelectedStateSave, refresh: false);
        }
    }

    private void TryHandleFileDropOnComponent(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(_selectedState.SelectedElement) { StateName = _selectedState.SelectedStateSave.Name });

        // see if it's over the component:
        IPositionedSizedObject ipsoOver = _selectionManager.GetRepresentationAt(worldX, worldY, IsComponentNoInstanceSelected, elementStack);

        string extension = FileManager.GetExtension(files[0]);
        bool isFontFile = extension == "ttf";

        if (ipsoOver?.Tag is ComponentSave component)
        {
            bool isTextureTarget = component.BaseType == "Sprite" || component.BaseType == "NineSlice" || component.BaseType == "Svg";
            bool isFontTarget = component.BaseType == "Text";

            if ((isFontFile && isFontTarget) || (!isFontFile && isTextureTarget))
            {
                string fileName = FileManager.MakeRelative(files[0], _fileLocations.ProjectFolder, preserveCase: true);

                string? baseType = GetBaseTypeForExtension(fileName);
                string addNewLabel = "Add new " + (baseType ?? "Sprite");

                string message = "What do you want to do with the file " + fileName;

                if (isFontFile)
                {
                    DialogChoices<string> choices = new()
                    {
                        ["set-font"] = "Set font on " + component.Name,
                        ["_"] = addNewLabel
                    };

                    string? result = _dialogService.ShowChoices(message, choices, canCancel: true);

                    if (result == "set-font")
                    {
                        var oldValue = _selectedState.SelectedStateSave
                            .GetValueOrDefault<string>("Font");

                        _selectedState.SelectedStateSave.SetValue("Font", fileName, "string");
                        _selectedState.SelectedInstance = null;
                        _setVariableLogic.PropertyValueChanged(
                            "Font",
                            oldValue,
                            _selectedState.SelectedInstance,
                            _selectedState.SelectedStateSave);

                        shouldUpdate = true;
                        handled = true;
                    }
                    else if (result == null)
                    {
                        handled = true;
                    }
                }
                else
                {
                    DialogChoices<string> choices = new()
                    {
                        ["set-source"] = "Set source file on " + component.Name,
                        ["_"] = addNewLabel
                    };

                    string? result = _dialogService.ShowChoices(message, choices, canCancel: true);

                    if (result == "set-source")
                    {
                        var oldValue = _selectedState.SelectedStateSave
                            .GetValueOrDefault<string>("SourceFile");

                        _selectedState.SelectedStateSave.SetValue("SourceFile", fileName, "string");
                        _selectedState.SelectedInstance = null;
                        _setVariableLogic.PropertyValueChanged(
                            "SourceFile",
                            oldValue,
                            _selectedState.SelectedInstance,
                            _selectedState.SelectedStateSave);

                        shouldUpdate = true;
                        handled = true;
                    }
                    else if (result == null)
                    {
                        handled = true;
                    }
                }
            }
        }
    }

    private bool CanDrop()
    {
        string? blockedReason = _dragDropManager.GetFileDropBlockedReason();
        if (blockedReason != null)
        {
            _guiCommands.PrintOutput($"File drop ignored: {blockedReason}.");
            return false;
        }
        return true;
    }

    private void TryHandleFileDropOnInstance(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        string extension = FileManager.GetExtension(files[0]);
        bool isFontFile = extension == "ttf";

        InstanceSave instance = isFontFile
            ? FindInstanceWithFontProperty(worldX, worldY)
            : FindInstanceWithSourceFile(worldX, worldY);

        if (instance != null)
        {
            string fileName = FileManager.MakeRelative(files[0], _fileLocations.ProjectFolder, preserveCase: true);

            string? baseType = GetBaseTypeForExtension(fileName);
            string addNewLabel = "Add new " + (baseType ?? "Sprite");

            string message = "What do you want to do with the file " + fileName;

            if (isFontFile)
            {
                DialogChoices<string> choices = new()
                {
                    ["set-font"] = "Set font on " + instance.Name,
                    ["_"] = addNewLabel
                };

                string? result = _dialogService.ShowChoices(message, choices, canCancel: true);

                if (result == "set-font")
                {
                    var oldValue = _selectedState.SelectedStateSave
                        .GetValueOrDefault<string>(instance.Name + ".Font");

                    _selectedState.SelectedStateSave.SetValue(instance.Name + ".Font", fileName, instance);
                    _selectedState.SelectedInstance = instance;

                    _setVariableLogic.PropertyValueChanged(
                        "Font",
                        oldValue, instance,
                        _selectedState.SelectedStateSave);

                    shouldUpdate = true;
                    handled = true;
                }
                else if (result == null)
                {
                    handled = true;
                }
            }
            else
            {
                DialogChoices<string> choices = new()
                {
                    ["set-source"] = "Set source file on " + instance.Name,
                    ["_"] = addNewLabel
                };

                string? result = _dialogService.ShowChoices(message, choices, canCancel: true);

                if (result == "set-source")
                {
                    var oldValue = _selectedState.SelectedStateSave
                        .GetValueOrDefault<string>(instance.Name + ".SourceFile");

                    _selectedState.SelectedStateSave.SetValue(instance.Name + ".SourceFile", fileName, instance);
                    _selectedState.SelectedInstance = instance;

                    _setVariableLogic.PropertyValueChanged(
                        "SourceFile",
                        oldValue, instance,
                        _selectedState.SelectedStateSave);

                    shouldUpdate = true;
                    handled = true;
                }
                else if (result == null)
                {
                    handled = true;
                }
                // continue for Add new Sprite
            }
        }
    }


    private InstanceSave FindInstanceWithSourceFile(float worldX, float worldY)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(_selectedState.SelectedElement) { StateName = _selectedState.SelectedStateSave.Name });

        IPositionedSizedObject ipsoOver = _selectionManager.GetRepresentationAt(worldX, worldY, IsComponentNoInstanceSelected, elementStack);

        if (ipsoOver != null && ipsoOver.Tag is InstanceSave)
        {
            var baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(ipsoOver.Tag as InstanceSave);

            if (baseStandardElement.DefaultState.Variables.Any(v => v.Name == "SourceFile"))
            {
                return ipsoOver.Tag as InstanceSave;
            }
        }

        return null;
    }

    private InstanceSave FindInstanceWithFontProperty(float worldX, float worldY)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(_selectedState.SelectedElement) { StateName = _selectedState.SelectedStateSave.Name });

        IPositionedSizedObject ipsoOver = _selectionManager.GetRepresentationAt(worldX, worldY, IsComponentNoInstanceSelected, elementStack);

        if (ipsoOver != null && ipsoOver.Tag is InstanceSave)
        {
            var baseStandardElement = ObjectFinder.Self.GetRootStandardElementSave(ipsoOver.Tag as InstanceSave);

            if (baseStandardElement?.Name == "Text")
            {
                return ipsoOver.Tag as InstanceSave;
            }
        }

        return null;
    }



    void HandleWireframeInitialized()
    {
        _wireframeContextMenu = new System.Windows.Controls.ContextMenu();

        gumEditorPanel = new ();
        gumEditorPanel.Dock = DockStyle.Fill;

        // 2025-01-02 UI Scale update
        // WireFrameControl needs to be added to the gumEditorPanel first
        // Otherwise, the combobox will be drawn ontop of the top yellow ruler
        CreateWireframeControl();

        System.Windows.Controls.Grid wpfGrid = new();
        wpfGrid.RowDefinitions.Add(new () { Height = GridLength.Auto});
        wpfGrid.RowDefinitions.Add(new () { Height = new (1, GridUnitType.Star) });

        _editorControls = new EditorControls();
        wpfGrid.Children.Add(_editorControls);
        Grid.SetRow(_editorControls, 0);

        WindowsFormsHost host = new WindowsFormsHost();
        host.Child = gumEditorPanel;

        // This is kind of a hack to deal with the airspace issue blocking mouse
        // interaction with the grid splitter and window resize handle: WindowsFormsHost
        // hosts a real HWND, which always paints on top of WPF siblings regardless of
        // z-order, so without this margin the grid splitter can't receive mouse input
        // right at the edge of this tab.
        host.Margin = new Thickness(4, 0, 4, 0);

        wpfGrid.Children.Add(host);
        Grid.SetRow(host, 1);

        _tabManager.AddControl(wpfGrid, "Editor", TabLocation.RightTop);

        wpfGrid.DataContext = _editorViewModel;

        _wireframeControl.XnaUpdate += () =>
        {
            _backgroundManager.Activity();
            _wireframeObjectManager.Activity();
            _toolLayerService.Activity();
        };

    }

    private void CreateWireframeControl()
    {
        this._wireframeControl = new WireframeControl(_dialogService, _outputManager, _pluginManager);
        this._wireframeControl.AllowDrop = true;
        this._wireframeControl.Dock = DockStyle.Fill;
        this._wireframeControl.Cursor = System.Windows.Forms.Cursors.Default;
        this._wireframeControl.DesiredFramesPerSecond = 30F;
        this._wireframeControl.Name = "wireframeControl1";
        this._wireframeControl.TabIndex = 0;
        this._wireframeControl.Text = "wireframeControl1";
        gumEditorPanel.Controls.Add(this._wireframeControl);
    }

    /// <summary>
    /// Refreshes the wifreframe control size - for some reason this is necessary if windows has a non-100% scale (for higher resolution displays)
    /// </summary>
    private void UpdateWireframeControlSizes()
    {
        // I don't think we need this for docking:
        //WireframeEditControl.Width = WireframeEditControl.Parent.Width / 2;

        //_toolbarPanel.Width = _toolbarPanel.Parent.Width;

        _wireframeControl.Width = _wireframeControl.Parent.Width;
    }

    private void HandleStateSelected(StateSave? save)
    {
        _wireframeObjectManager.RefreshAll(forceLayout: true);
        // Record that this rebuild happened for the current element so the ElementSelected event
        // that follows in the same cascade (when an element is selected) can skip its redundant
        // rebuild. See WireframeRefreshCoordinator.
        _wireframeRefreshCoordinator.OnStateRebuild(_selectedState.SelectedElement);
    }

    private void wireframeControl1_MouseDown(object? sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            _editingManager.OnRightClick();

            if (_wireframeContextMenu.Items.Count > 0)
            {
                _wireframeContextMenu.Placement = PlacementMode.MousePoint;
                _wireframeContextMenu.IsOpen = true;
            }
        }
    }

    private void ApplyThemeSettings(IEffectiveThemeSettings settings)
    {
        this._wireframeControl.BackgroundColor = ToXna(settings.CheckerA);
        this._wireframeControl.SetGuideColors(settings.GuideLine, settings.GuideText);
        static Microsoft.Xna.Framework.Color ToXna(Color color) => new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
    }

    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        ApplyThemeSettings(message.settings);
    }
}