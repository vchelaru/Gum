using CommonFormsAndControls.Forms;
using FlatRedBall.AnimationEditorForms.Controls;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Plugins.ScrollBarPlugin;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolCommands;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json.Linq;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Management.Instrumentation;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;
using ToolsUtilities;

namespace Gum.Plugins.InternalPlugins.EditorTab;

[Export(typeof(PluginBase))]
internal class MainEditorTabPlugin : InternalPlugin
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
    private readonly GuiCommands _guiCommands;
    private readonly LocalizationManager _localizationManager;
    private readonly ScreenshotService _screenshotService;
    private readonly SelectionManager _selectionManager;
    private readonly ElementCommands _elementCommands;
    WireframeControl _wireframeControl;

    private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl _wireframeEditControl;
    private int _defaultWireframeEditControlHeight;

    Panel gumEditorPanel;
    private LayerService _layerService;
    private ContextMenuStrip _wireframeContextMenuStrip;
    private EditingManager _editingManager;

    #endregion

    public MainEditorTabPlugin()
    {
        _scrollbarService = new ScrollbarService();
        _guiCommands = Builder.Get<GuiCommands>();
        _localizationManager = Builder.Get<LocalizationManager>();
        _screenshotService = new ScreenshotService();
        _editingManager = new EditingManager();
        _selectionManager = new SelectionManager(SelectedState.Self, _editingManager);
        _elementCommands = ElementCommands.Self;

    }

    public override void StartUp()
    {
        AssignEvents();

        var menuItem = AddMenuItem("File", "Export as Image");
        _screenshotService.InitializeMenuItem(menuItem);
        BeforeRender += _screenshotService.HandleBeforeRender;
        AfterRender += _screenshotService.HandleAfterRender;

        HandleWireframeInitialized();

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
        
        this.UiZoomValueChanged += HandleUiZoomValueChanged;

        this.IpsoSelected += HandleIpsoSelected;
        this.SetHighlightedIpso += HandleSetHighlightedElement;

        this.ProjectLoad += HandleProjectLoad;
        this.ProjectPropertySet += HandleProjectPropertySet;


        this.AfterUndo += HandleAfterUndo;
    }

    private GraphicalUiElement? HandleCreateGraphicalUiElement(ElementSave elementSave)
    {
        var toReturn = elementSave.ToGraphicalUiElement(SystemManagers.Default, addToManagers: false);
        toReturn.AddToManagers(SystemManagers.Default, _layerService.MainEditorLayer);
        return toReturn;
    }

    private bool HandleDelete()
    {
        return _selectionManager.TryHandleDelete();
    }

    private void HandleWireframeRefreshed()
    {
        _selectionManager.Refresh();
    }

    private void HandleAfterUndo()
    {
        _selectionManager.Refresh();
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
    }

    private void HandleWireframePropertyChanged(string name)
    {
        if(name == nameof(WireframeCommands.AreHighlightsVisible))
        {
            _selectionManager.AreHighlightsVisible = 
                GumCommands.Self.WireframeCommands.AreHighlightsVisible;
        }
        else if(name == nameof(WireframeCommands.IsBackgroundGridVisible))
        {
            _wireframeControl.BackgroundSprite.Visible = 
                GumCommands.Self.WireframeCommands.IsBackgroundGridVisible;
        }
    }

    private void HandleProjectLoad(GumProjectSave save)
    {
        GraphicalUiElement.CanvasWidth = save.DefaultCanvasWidth;
        GraphicalUiElement.CanvasHeight = save.DefaultCanvasHeight;


        _wireframeControl.UpdateCanvasBoundsToProject();


        _selectionManager.RestrictToUnitValues =
            save.RestrictToUnitValues;

        AdjustTextureFilter();
    }

    private void HandleProjectPropertySet(string propertyName)
    {
        if(propertyName == nameof(GumProjectSave.TextureFilter))
        {
            AdjustTextureFilter();
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
        Wireframe.WireframeObjectManager.Self.RefreshAll(true);
    }

    private void HandleUiZoomValueChanged()
    {
        // Uncommenting this makes the area for teh combo box properly grow, but it
        // kills the wireframe view. Not sure why....
        _wireframeEditControl.Height = _defaultWireframeEditControlHeight * _guiCommands.UiZoomValue / 100;
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

        var state = SelectedState.Self.SelectedStateSave ?? element?.DefaultState;
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

                (instance != null || SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
            {
                // this assumes that the object having its variable set is the selected instance. If we're setting
                // an exposed variable, this is not the case - the object having its variable set is actually the instance.
                //GraphicalUiElement gue = WireframeObjectManager.Self.GetSelectedRepresentation();
                GraphicalUiElement gue = null;
                if (instance != null)
                {
                    gue = WireframeObjectManager.Self.GetRepresentation(instance);
                }
                else
                {
                    gue = WireframeObjectManager.Self.GetSelectedRepresentation();
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

                    WireframeObjectManager.Self.RootGue?.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);
                    //gue.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);

                    handledByDirectSet = !disposedFile;
                }
                if (unqualifiedMember == "Text" && _localizationManager.HasDatabase)
                {
                    WireframeObjectManager.Self.ApplyLocalization(gue, value as string);
                }
            }

            if (!handledByDirectSet)
            {
                WireframeObjectManager.Self.RefreshAll(true, forceReloadTextures: false);
            }


            _selectionManager.Refresh();
        }
    }

    // todo - When a new element is selected, a new state is selected too
    // need to only handle this 1 time. Currently there is a double-refresh
    private void HandleElementSelected(ElementSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
        _selectionManager.Refresh();

    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: false);
        _editingManager.RefreshContextMenuStrip();
        _selectionManager.WireframeEditor?.UpdateAspectRatioForGrabbedIpso();
        _selectionManager.Refresh();
    }

    private void HandleXnaInitialized()
    {
        _scrollbarService.HandleWireframeInitialized(_wireframeControl, gumEditorPanel);

        _layerService = new Services.LayerService();

        var localizationManager = Builder.Get<LocalizationManager>();

        _wireframeEditControl.ZoomChanged += HandleControlZoomChange;

        Wireframe.WireframeObjectManager.Self.Initialize(_wireframeControl, 
            localizationManager);
        _wireframeControl.Initialize(_wireframeEditControl, gumEditorPanel, HotkeyManager.Self, _selectionManager);

        // _layerService must be created after _wireframeControl so that the SystemManagers.Default are assigned
        _layerService.Initialize();
        _selectionManager.Initialize(_layerService);

        _wireframeControl.ShareLayerReferences(_layerService);

        _editingManager.Initialize(_wireframeContextMenuStrip);



        _scrollbarService.HandleXnaInitialized();


        this._wireframeControl.Parent.Resize += (not, used) =>
        {
            UpdateWireframeControlSizes();
            PluginManager.Self.HandleWireframeResized();
        };

        //this._wireframeControl.MouseClick += wireframeControl1_MouseClick;
        this._wireframeControl.MouseDown += wireframeControl1_MouseDown;


        this._wireframeControl.DragDrop += HandleFileDragDrop;
        this._wireframeControl.DragEnter += DragDropManager.Self.HandleFileDragEnter;
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
            args.Action = DragAction.Continue;
        };
        _wireframeControl.CameraChanged += () =>
        {
            PluginManager.Self.CameraChanged();
        };

        this._wireframeControl.KeyDown += (o, args) =>
        {
            if (args.KeyCode == Keys.Tab)
            {
                GumCommands.Self.GuiCommands.ToggleToolVisibility();
            }
        };

        // Apply FrameRate, but keep it within sane limits
        float frameRate = Math.Max(Math.Min(ProjectManager.Self.GeneralSettingsFile.FrameRate, 60), 10);
        _wireframeControl.DesiredFramesPerSecond = frameRate;

        UpdateWireframeControlSizes();
    }

    private void HandleControlZoomChange(object sender, EventArgs e)
    {
        Renderer.Self.Camera.Zoom = _wireframeEditControl.PercentageValue / 100.0f;
    }

    internal void HandleFileDragDrop(object sender, DragEventArgs e)
    {
        if (!CanDrop())
            return;

        float worldX, worldY;
        Renderer.Self.Camera.ScreenToWorld(InputLibrary.Cursor.Self.X, InputLibrary.Cursor.Self.Y, out worldX, out worldY);
        string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

        if (files == null)
        {
            return;
        }

        var handled = false;
        bool shouldUpdate = false;

        // If only one file was dropped, see if we're over an instance that can take a file
        if (files.Length == 1)
        {
            if (!DragDropManager.Self.IsValidExtensionForFileDrop(files[0]))
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
                if (!DragDropManager.Self.IsValidExtensionForFileDrop(file))
                    continue;

                string fileName = FileManager.MakeRelative(file, FileLocations.Self.ProjectFolder);
                AddNewInstanceForDrop(fileName, worldX, worldY);
                shouldUpdate = true;
            }

        }
        if (shouldUpdate)
        {
            GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
            GumCommands.Self.GuiCommands.RefreshVariables();

            WireframeObjectManager.Self.RefreshAll(true);
        }
    }

    private void AddNewInstanceForDrop(string fileName, float worldX, float worldY)
    {
        string nameToAdd = FileManager.RemovePath(FileManager.RemoveExtension(fileName));

        var element = SelectedState.Self.SelectedElement;

        IEnumerable<string> existingNames = element.Instances.Select(i => i.Name);
        nameToAdd = StringFunctions.MakeStringUnique(nameToAdd, existingNames);

        InstanceSave instance =
            _elementCommands.AddInstance(element, nameToAdd);
        instance.BaseType = "Sprite";

        DragDropManager.Self.SetInstanceToPosition(worldX, worldY, instance);

        var variableName = instance.Name + ".SourceFile";

        var oldValue = SelectedState.Self.SelectedStateSave.GetValueOrDefault<string>(variableName);

        SelectedState.Self.SelectedStateSave.SetValue(variableName, fileName, instance);

        SetVariableLogic.Self.ReactToPropertyValueChanged("SourceFile", oldValue, element, instance, SelectedState.Self.SelectedStateSave, refresh: false);

    }

    private void TryHandleFileDropOnComponent(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement) { StateName = SelectedState.Self.SelectedStateSave.Name });

        // see if it's over the component:
        IPositionedSizedObject ipsoOver = _selectionManager.GetRepresentationAt(worldX, worldY, false, elementStack);
        if (ipsoOver?.Tag is ComponentSave component && (component.BaseType == "Sprite" || component.BaseType == "NineSlice"))
        {
            string fileName = FileManager.MakeRelative(files[0], FileLocations.Self.ProjectFolder);

            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.StartPosition = FormStartPosition.Manual;

            mbmb.Location = new System.Drawing.Point(MainWindow.MousePosition.X - mbmb.Width / 2,
                 MainWindow.MousePosition.Y - mbmb.Height / 2);

            mbmb.MessageText = "What do you want to do with the file " + fileName;

            mbmb.AddButton("Set source file on " + component.Name, DialogResult.OK);
            mbmb.AddButton("Add new Sprite", DialogResult.Yes);
            mbmb.AddButton("Nothing", DialogResult.Cancel);


            var result = mbmb.ShowDialog();

            if (result == DialogResult.OK)
            {
                var oldValue = SelectedState.Self.SelectedStateSave
                    .GetValueOrDefault<string>("SourceFile");

                SelectedState.Self.SelectedStateSave.SetValue("SourceFile", fileName);
                ProjectState.Self.Selected.SelectedInstance = null;
                SetVariableLogic.Self.PropertyValueChanged("SourceFile", oldValue, SelectedState.Self.SelectedInstance);

                shouldUpdate = true;
                handled = true;
            }
            else if (result == DialogResult.Cancel)
            {
                handled = true;

            }

        }
    }

    private bool CanDrop()
    {
        return SelectedState.Self.SelectedStandardElement == null &&    // Don't allow dropping on standard elements
               SelectedState.Self.SelectedElement != null &&            // An element must be selected
               SelectedState.Self.SelectedStateSave != null;            // A state must be selected
    }

    private void TryHandleFileDropOnInstance(float worldX, float worldY, string[] files, ref bool handled, ref bool shouldUpdate)
    {
        // This only supports drag+drop on an instance, but what if dropping on a component
        // which inherits from Sprite, or perhaps an instance that has an exposed file variable?
        // Not super high priority, but it's worth noting that this currently doesn't work...
        InstanceSave instance = FindInstanceWithSourceFile(worldX, worldY);
        if (instance != null)
        {
            string fileName = FileManager.MakeRelative(files[0], FileLocations.Self.ProjectFolder);

            MultiButtonMessageBox mbmb = new MultiButtonMessageBox();
            mbmb.StartPosition = FormStartPosition.Manual;

            mbmb.Location = new System.Drawing.Point(MainWindow.MousePosition.X - mbmb.Width / 2,
                 MainWindow.MousePosition.Y - mbmb.Height / 2);

            mbmb.MessageText = "What do you want to do with the file " + fileName;

            mbmb.AddButton("Set source file on " + instance.Name, DialogResult.OK);
            mbmb.AddButton("Add new Sprite", DialogResult.Yes);
            mbmb.AddButton("Nothing", DialogResult.Cancel);

            var result = mbmb.ShowDialog();

            if (result == DialogResult.OK)
            {
                var oldValue = SelectedState.Self.SelectedStateSave
                    .GetValueOrDefault<string>(instance.Name + ".SourceFile");

                SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".SourceFile", fileName, instance);
                ProjectState.Self.Selected.SelectedInstance = instance;
                SetVariableLogic.Self.PropertyValueChanged("SourceFile", oldValue, instance);

                shouldUpdate = true;
                handled = true;
            }
            else if (result == DialogResult.Cancel)
            {
                handled = true;

            }
            // continue for DialogResult.Yes
        }
    }


    private InstanceSave FindInstanceWithSourceFile(float worldX, float worldY)
    {
        List<ElementWithState> elementStack = new List<ElementWithState>();
        elementStack.Add(new ElementWithState(SelectedState.Self.SelectedElement) { StateName = SelectedState.Self.SelectedStateSave.Name });

        IPositionedSizedObject ipsoOver = _selectionManager.GetRepresentationAt(worldX, worldY, false, elementStack);

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

        gumEditorPanel = new Panel();

        // 2025-01-02 UI Scale update
        // WireFrameControl needs to be added to the gumEditorPanel first
        // Otherwise, the combobox will be drawn ontop of the top yellow ruler
        CreateWireframeControl(wireframeContextMenuStrip);
        _wireframeContextMenuStrip = wireframeContextMenuStrip;

        // The WireframeEditControl (Where the combobox lives) must
        // be added to the gumEditorPanel 2nd, no idea why
        CreateWireframeEditControl(gumEditorPanel);

        GumCommands.Self.GuiCommands.AddControl(gumEditorPanel, "Editor", TabLocation.RightTop);

        _wireframeControl.XnaUpdate += () =>
        {
            Wireframe.WireframeObjectManager.Self.Activity();
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

        // Add location.Y to account for the shortcut bar at the top.
        _wireframeControl.Height = _wireframeControl.Parent.Height - _wireframeControl.Location.Y;
    }

    private void HandleStateSelected(StateSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }

    private void wireframeControl1_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            _editingManager.OnRightClick();
        }
    }


    private void CreateWireframeEditControl(Panel gumEditorPanel)
    {
        _wireframeEditControl = new FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl();
        gumEditorPanel.Controls.Add(_wireframeEditControl);
        // 
        // WireframeEditControl
        // 
        //this.WireframeEditControl.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
        //| System.Windows.Forms.AnchorStyles.Right)));
        _wireframeEditControl.Dock = DockStyle.Top;
        _wireframeEditControl.Location = new System.Drawing.Point(0, 0);
        _wireframeEditControl.Margin = new System.Windows.Forms.Padding(4);
        _wireframeEditControl.Name = "WireframeEditControl";
        _wireframeEditControl.PercentageValue = 100;
        _wireframeEditControl.TabIndex = 1;
        _defaultWireframeEditControlHeight = _wireframeEditControl.Height;

    }
}
