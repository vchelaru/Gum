using CommonFormsAndControls.Forms;
using CommunityToolkit.Mvvm.Messaging;
using EditorTabPlugin_XNA.Services;
using EditorTabPlugin_XNA.ViewModels;
using EditorTabPlugin_XNA.Views;
using FlatRedBall.Glue.Themes;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Behaviors;
using Gum.DataTypes.Variables;
using Gum.Dialogs;
using Gum.Extensions;
using Gum.Logic;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Plugins.ScrollBarPlugin;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.Services.Dialogs;
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
internal class MainEditorTabPlugin : InternalPlugin, IRecipient<UiBaseFontSizeChangedMessage>, IRecipient<ThemeChangedMessage>
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
    private readonly LocalizationManager _localizationManager;
    private readonly ScreenshotService _screenshotService;
    private readonly SelectionManager _selectionManager;
    private readonly IElementCommands _elementCommands;
    private readonly SinglePixelTextureService _singlePixelTextureService;
    private BackgroundSpriteService _backgroundSpriteService;
    private readonly ISelectedState _selectedState;
    private readonly WireframeCommands _wireframeCommands;
    private readonly IFileCommands _fileCommands;
    private readonly HotkeyManager _hotkeyManager;
    private readonly SetVariableLogic _setVariableLogic;
    private EditorViewModel _editorViewModel;
    private readonly IOptionsMonitor<ThemeSettings> _themeSettings;
    private readonly FileLocations _fileLocations;
    private DragDropManager _dragDropManager;
    WireframeControl _wireframeControl;

    private int _defaultWireframeEditControlHeight;

    System.Windows.Forms.Panel gumEditorPanel;
    private LayerService _layerService;
    private ContextMenuStrip _wireframeContextMenuStrip;
    private EditingManager _editingManager;
    private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;
    private readonly WireframeObjectManager _wireframeObjectManager;


    // This is used to punch through the selected and go back up to the top. More info here:
    // https://github.com/vchelaru/Gum/issues/1810
    public bool IsComponentNoInstanceSelected => _selectedState.SelectedInstance == null && _selectedState.SelectedComponent != null;

    #endregion

    public MainEditorTabPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        
        _scrollbarService = new ScrollbarService();
        _guiCommands = Locator.GetRequiredService<IGuiCommands>();
        _localizationManager = Locator.GetRequiredService<LocalizationManager>();
        _editingManager = new EditingManager(
            Locator.GetRequiredService<WireframeObjectManager>(),
            Locator.GetRequiredService<ReorderLogic>());
        _variableInCategoryPropagationLogic = Locator.GetRequiredService<IVariableInCategoryPropagationLogic>();
        _wireframeObjectManager = Locator.GetRequiredService<WireframeObjectManager>();
        _fileLocations = Locator.GetRequiredService<FileLocations>();

        IUndoManager undoManager = Locator.GetRequiredService<IUndoManager>();
        IDialogService dialogService = Locator.GetRequiredService<IDialogService>();
        HotkeyManager hotkeyManager = Locator.GetRequiredService<HotkeyManager>();
        _selectionManager = new SelectionManager(
            _selectedState, 
            undoManager, 
            _editingManager, 
            dialogService, 
            hotkeyManager,
            _variableInCategoryPropagationLogic,
            _wireframeObjectManager);

        _screenshotService = new ScreenshotService(_selectionManager);
        _elementCommands = Locator.GetRequiredService<IElementCommands>();
        _singlePixelTextureService = new SinglePixelTextureService();
        _backgroundSpriteService = new BackgroundSpriteService();
        _dragDropManager = Locator.GetRequiredService<DragDropManager>();
        _wireframeCommands = Locator.GetRequiredService<WireframeCommands>();
        _fileCommands = Locator.GetRequiredService<IFileCommands>();
        _hotkeyManager = hotkeyManager;
        _setVariableLogic = Locator.GetRequiredService<SetVariableLogic>();
        PluginManager pluginManager = Locator.GetRequiredService<PluginManager>();

        _editorViewModel = new EditorViewModel(
            pluginManager, 
            _fileCommands, 
            _wireframeObjectManager);

        Locator.GetRequiredService<IMessenger>().RegisterAll(this);
    }

    public override void StartUp()
    {
        GraphicalUiElement.SetPropertyOnRenderable = CustomSetPropertyOnRenderable.SetPropertyOnRenderable;
        GraphicalUiElement.UpdateFontFromProperties = CustomSetPropertyOnRenderable.UpdateToFontValues;
        GraphicalUiElement.ThrowExceptionsForMissingFiles = CustomSetPropertyOnRenderable.ThrowExceptionsForMissingFiles;
        GraphicalUiElement.AddRenderableToManagers = CustomSetPropertyOnRenderable.AddRenderableToManagers;
        GraphicalUiElement.RemoveRenderableFromManagers = CustomSetPropertyOnRenderable.RemoveRenderableFromManagers;
        CustomSetPropertyOnRenderable.PropertyAssignmentError += HandlePropertyAssignmentError;

        AssignEvents();

        var menuItem = AddMenuItem("File", "Export as Image");
        _screenshotService.InitializeMenuItem(menuItem);
        BeforeRender += _screenshotService.HandleBeforeRender;
        AfterRender += _screenshotService.HandleAfterRender;

        HandleWireframeInitialized();

    }

    private void HandlePropertyAssignmentError(string obj)
    {
        _guiCommands.PrintOutput(obj);
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

    private Vector2? HandleGetWorldCursorPosition(InputLibrary.Cursor cursor)
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
        return RuntimeObjectCreator.TryHandleAsBaseType(type, SystemManagers.Default) as IRenderableIpso;
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
            text.RenderBoundary = ProjectManager.Self.GeneralSettingsFile.ShowTextOutlines;
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

    private void HandleSetHighlightedElement(IPositionedSizedObject whatToHighlight)
    {
        _selectionManager.HighlightedIpso = whatToHighlight;
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
                ProjectManager.Self.GumProjectSave.RestrictToUnitValues;
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
        _wireframeContextMenuStrip.Renderer = FrbMenuStripRenderer.GetCurrentThemeRenderer(out var fontSize);
        _wireframeContextMenuStrip.Font = new Font("Segoe UI", fontSize);
    }

    private void HandleVariableSetLate(ElementSave element, InstanceSave instance, string qualifiedName, object oldValue)
    {
        /////////////////////////////Early Out//////////////////////////
        if(element == null)
        {
            // This could be a variable on a behavior or instance in a behavior. If so, we don't show anything in the editor
            return;
        }
        ////////////////////////////End Early Out///////////////////////

        if(instance != null)
        {
            qualifiedName = instance.Name + "." + qualifiedName;
        }

        var state = _selectedState.SelectedStateSave ?? element?.DefaultState;
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

        // If the values are the same they may have been set to be the same by a plugin that
        // didn't allow the assignment, so don't go through the work of saving and refreshing.
        // Update January 19, 2025 - actually for incrmeental changes just use it, it will be fast
        if (!areSame || supportsIncrementalChange)
        {

            // if a deep reference is set, then this is more complicated than a single variable assignment, so we should
            // force everything. This makes debugging a little more difficult, but it keeps the wireframe accurate without having to track individual assignments.
            if (PropertiesSupportingIncrementalChange.Contains(unqualifiedMember) &&
            // June 19, 2024 - if the value is null (from default assignment), we
            // can't set this single value - it requires a recursive variable finder.
            // for simplicity (for now?) we will just refresh all:
                value != null &&

                (instance != null || _selectedState.SelectedComponent != null || _selectedState.SelectedStandardElement != null))
            {
                // this assumes that the object having its variable set is the selected instance. If we're setting
                // an exposed variable, this is not the case - the object having its variable set is actually the instance.
                //GraphicalUiElement gue = _wireframeObjectManager.GetSelectedRepresentation();
                GraphicalUiElement gue = null;
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
                    VariableSave variable = null;
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

                    gue.SetProperty(unqualifiedMember, value);

                    _wireframeObjectManager.RootGue?.ApplyVariableReferences(state);
                    //gue.ApplyVariableReferences(_selectedState.SelectedStateSave);

                    handledByDirectSet = !disposedFile;
                }
                if (unqualifiedMember == "Text" && _localizationManager.HasDatabase)
                {
                    _wireframeObjectManager.ApplyLocalization(gue, value as string);
                }
            }

            if (!handledByDirectSet)
            {
                _wireframeObjectManager.RefreshAll(true, forceReloadTextures: false);
            }


            _selectionManager.Refresh();
        }
    }

    // todo - When a new element is selected, a new state is selected too
    // need to only handle this 1 time. Currently there is a double-refresh
    private void HandleElementSelected(ElementSave save)
    {
        _wireframeObjectManager.RefreshAll(forceLayout: true);

        _selectionManager.Refresh();

    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        _wireframeObjectManager.RefreshAll(forceLayout: false);
        _editingManager.RefreshContextMenuStrip();
        _selectionManager.WireframeEditor?.UpdateAspectRatioForGrabbedIpso();
        _selectionManager.Refresh();
    }

    private void HandleXnaInitialized()
    {
        _scrollbarService.HandleWireframeInitialized(_wireframeControl, gumEditorPanel);

        _layerService = new Services.LayerService();

        _wireframeControl.Initialize(
            gumEditorPanel, 
            _hotkeyManager, 
            _selectionManager, 
            _dragDropManager,
            _editorViewModel);
        var systemManagers = _wireframeControl.SystemManagers;


        // _layerService must be created after _wireframeControl so that the SystemManagers.Default are assigned
        _layerService.Initialize();
        _selectionManager.Initialize(_layerService);

        _wireframeControl.ShareLayerReferences(_layerService);

        // This must be initialized *after* ShareLayerReferences since that
        // creates the rulers
        _editorViewModel.InitializeXnaView(systemManagers,
            _wireframeControl.TopRuler,
            _wireframeControl.LeftRuler);

        _editingManager.Initialize(_wireframeContextMenuStrip);

        _backgroundSpriteService.Initialize(_wireframeControl.SystemManagers);

        _scrollbarService.HandleXnaInitialized();


        this._wireframeControl.Parent.Resize += (not, used) =>
        {
            UpdateWireframeControlSizes();
            PluginManager.Self.HandleWireframeResized();
        };

        //this._wireframeControl.MouseClick += wireframeControl1_MouseClick;
        this._wireframeControl.MouseDown += wireframeControl1_MouseDown;


        this._wireframeControl.DragDrop += OnWireframeDrop;
        this._wireframeControl.DragEnter += _dragDropManager.OnWireframeDragEnter;
        this._wireframeControl.DragOver += (sender, e) =>
        {
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
            PluginManager.Self.CameraChanged();
        };

        this._wireframeControl.KeyDown += (o, args) =>
        {
            if (args.KeyCode == Keys.Tab)
            {
                _guiCommands.ToggleToolVisibility();
            }
        };

        // Apply FrameRate, but keep it within sane limits
        float frameRate = Math.Max(Math.Min(ProjectManager.Self.GeneralSettingsFile.FrameRate, 60), 10);
        _wireframeControl.DesiredFramesPerSecond = frameRate;

        UpdateWireframeControlSizes();

        IEffectiveThemeSettings themeSettings = Locator.GetRequiredService<IThemingService>().EffectiveSettings;
        ApplyThemeSettings(themeSettings);
    }

    internal void OnWireframeDrop(object sender, System.Windows.Forms.DragEventArgs e)
    {
        // Handle node drops
        List<TreeNode>? droppedNodes = e switch
        {
            _ when e.GetData<List<TreeNode>>() is { } nodes => nodes,
            _ when e.GetData<TreeNode>() is { } singleNode => [singleNode],
            _ => null,
        };

        if (droppedNodes is not null)
        {
            foreach (var draggedObject in droppedNodes.Select(x => x.Tag))
            {
                _dragDropManager.OnNodeObjectDroppedInWireframe(draggedObject);
            }

            return;
        }

        // Handle file drops

        if (!CanDrop())
            return;

        float worldX, worldY;
        Renderer.Self.Camera.ScreenToWorld(InputLibrary.Cursor.Self.X, InputLibrary.Cursor.Self.Y, out worldX, out worldY);
        string[] files = (string[])e.Data.GetData(System.Windows.Forms.DataFormats.FileDrop);

        if (files == null)
        {
            return;
        }

        var handled = false;
        bool shouldUpdate = false;

        // If only one file was dropped, see if we're over an instance that can take a file
        if (files.Length == 1)
        {
            if (!_dragDropManager.IsValidExtensionForFileDrop(files[0]))
            {
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
            foreach (string file in files)
            {
                if (!_dragDropManager.IsValidExtensionForFileDrop(file))
                    continue;

                string fileName = FileManager.MakeRelative(file, _fileLocations.ProjectFolder);
                AddNewInstanceForDrop(fileName, worldX, worldY);
                shouldUpdate = true;
            }

        }
        if (shouldUpdate)
        {
            _fileCommands.TryAutoSaveCurrentElement();
            _guiCommands.RefreshVariables();

            _wireframeObjectManager.RefreshAll(true);
        }
    }

    private void AddNewInstanceForDrop(string fileName, float worldX, float worldY)
    {
        string nameToAdd = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

        var element = _selectedState.SelectedElement;

        IEnumerable<string> existingNames = element.Instances.Select(i => i.Name);
        nameToAdd = StringFunctions.MakeStringUnique(nameToAdd, existingNames);

        InstanceSave instance =
            _elementCommands.AddInstance(element, nameToAdd);
        instance.BaseType = "Sprite";

        _dragDropManager.SetInstanceToPosition(worldX, worldY, instance);

        var variableName = instance.Name + ".SourceFile";

        var oldValue = _selectedState.SelectedStateSave.GetValueOrDefault<string>(variableName);

        _selectedState.SelectedStateSave.SetValue(variableName, fileName, instance);

        _setVariableLogic.ReactToPropertyValueChanged("SourceFile", oldValue, element, instance, _selectedState.SelectedStateSave, refresh: false);

    }

    private void TryHandleFileDropOnComponent(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(_selectedState.SelectedElement) { StateName = _selectedState.SelectedStateSave.Name });

        // see if it's over the component:
        IPositionedSizedObject ipsoOver = _selectionManager.GetRepresentationAt(worldX, worldY, IsComponentNoInstanceSelected, elementStack);
        if (ipsoOver?.Tag is ComponentSave component && (component.BaseType == "Sprite" || component.BaseType == "NineSlice"))
        {
            string fileName = FileManager.MakeRelative(files[0], _fileLocations.ProjectFolder, preserveCase:true);

            string message = "What do you want to do with the file " + fileName;
            DialogChoices<string> choices = new()
            {
                ["set-source"] = "Set source file on " + component.Name,
                ["_"] = "Add new Sprite"
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

    private bool CanDrop()
    {
        return _selectedState.SelectedStandardElement == null &&    // Don't allow dropping on standard elements
               _selectedState.SelectedElement != null &&            // An element must be selected
               _selectedState.SelectedStateSave != null;            // A state must be selected
    }

    private void TryHandleFileDropOnInstance(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        // This only supports drag+drop on an instance, but what if dropping on a component
        // which inherits from Sprite, or perhaps an instance that has an exposed file variable?
        // Not super high priority, but it's worth noting that this currently doesn't work...
        InstanceSave instance = FindInstanceWithSourceFile(worldX, worldY);
        if (instance != null)
        {
            string fileName = FileManager.MakeRelative(files[0], _fileLocations.ProjectFolder, preserveCase:true);

            string message = "What do you want to do with the file " + fileName;
            DialogChoices<string> choices = new()
            {
                ["set-source"] = "Set source file on " + instance.Name,
                ["_"] = "Add new Sprite"
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



    void HandleWireframeInitialized()
    {
        ContextMenuStrip wireframeContextMenuStrip;

        wireframeContextMenuStrip = new System.Windows.Forms.ContextMenuStrip();
        wireframeContextMenuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
        wireframeContextMenuStrip.Name = "WireframeContextMenuStrip";
        wireframeContextMenuStrip.Size = new System.Drawing.Size(61, 4);
        wireframeContextMenuStrip.Renderer = FrbMenuStripRenderer.GetCurrentThemeRenderer(out _, "Frb.Colors.Background");

        gumEditorPanel = new ();
        gumEditorPanel.Dock = DockStyle.Fill;

        // 2025-01-02 UI Scale update
        // WireFrameControl needs to be added to the gumEditorPanel first
        // Otherwise, the combobox will be drawn ontop of the top yellow ruler
        CreateWireframeControl(wireframeContextMenuStrip);
        _wireframeContextMenuStrip = wireframeContextMenuStrip;

        System.Windows.Controls.Grid wpfGrid = new();
        wpfGrid.RowDefinitions.Add(new () { Height = GridLength.Auto});
        wpfGrid.RowDefinitions.Add(new () { Height = new (1, GridUnitType.Star) });

        EditorControls editorControls = new ();
        wpfGrid.Children.Add(editorControls);
        Grid.SetRow(editorControls, 0);

        WindowsFormsHost host = new WindowsFormsHost();
        host.Child = gumEditorPanel;

        wpfGrid.Children.Add(host);
        Grid.SetRow(host, 1);

        _tabManager.AddControl(wpfGrid, "Editor", TabLocation.RightTop);

        wpfGrid.DataContext = _editorViewModel;

        _wireframeControl.XnaUpdate += () =>
        {
            _backgroundSpriteService.Activity();
            _wireframeObjectManager.Activity();
            ToolLayerService.Self.Activity();
        };

    }

    private void CreateWireframeControl(System.Windows.Forms.ContextMenuStrip WireframeContextMenuStrip)
    {
        this._wireframeControl = new WireframeControl();
        this._wireframeControl.AllowDrop = true;
        this._wireframeControl.Dock = DockStyle.Fill;
        this._wireframeControl.ContextMenuStrip = WireframeContextMenuStrip;
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
    }

    private void wireframeControl1_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            _editingManager.OnRightClick();
        }
    }

    private void ApplyThemeSettings(IEffectiveThemeSettings settings)
    {
        this._wireframeControl.BackgroundColor = ToXna(settings.CheckerA);
        this._wireframeControl.SetGuideColors(settings.GuideLine, settings.GuideText);
        _wireframeContextMenuStrip.Renderer = FrbMenuStripRenderer.GetCurrentThemeRenderer(out _);
        static Microsoft.Xna.Framework.Color ToXna(Color color) => new Microsoft.Xna.Framework.Color(color.R, color.G, color.B, color.A);
    }

    void IRecipient<ThemeChangedMessage>.Receive(ThemeChangedMessage message)
    {
        ApplyThemeSettings(message.settings);
    }
}