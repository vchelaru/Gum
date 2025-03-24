using FlatRedBall.AnimationEditorForms.Controls;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.ScrollBarPlugin;
using Gum.Services;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
using Microsoft.Xna.Framework.Graphics;
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
    WireframeControl _wireframeControl;

    private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl _wireframeEditControl;
    private int _defaultWireframeEditControlHeight;

    Panel gumEditorPanel;
    private LayerService _layerService;
    private ContextMenuStrip _wireframeContextMenuStrip;

    #endregion

    public MainEditorTabPlugin()
    {
        _scrollbarService = new ScrollbarService();
        _guiCommands = Builder.Get<GuiCommands>();
        _localizationManager = Builder.Get<LocalizationManager>();
    }

    public override void StartUp()
    {
        AssignEvents();

        HandleWireframeInitialized();
    }

    private void AssignEvents()
    {
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.ElementDelete += HandleElementDeleted;
        this.VariableSetLate += HandleVariableSetLate;

        this.CameraChanged += _scrollbarService.HandleCameraChanged;
        this.XnaInitialized += HandleXnaInitialized;
        this.WireframeResized += _scrollbarService.HandleWireframeResized;
        this.ElementSelected += _scrollbarService.HandleElementSelected;
        this.UiZoomValueChanged += HandleUiZoomValueChanged;
        this.ProjectLoad += HandleProjectLoad;
        this.ProjectPropertySet += HandleProjectPropertySet;
    }


    private void HandleProjectLoad(GumProjectSave save)
    {
        GraphicalUiElement.CanvasWidth = save.DefaultCanvasWidth;
        GraphicalUiElement.CanvasHeight = save.DefaultCanvasHeight;

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

                    handledByDirectSet = true;
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


            SelectionManager.Self.Refresh();
        }
    }

    // todo - When a new element is selected, a new state is selected too
    // need to only handle this 1 time. Currently there is a double-refresh
    private void HandleElementSelected(ElementSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: false);
        EditingManager.Self.RefreshContextMenuStrip();
    }

    private void HandleXnaInitialized()
    {
        _scrollbarService.HandleWireframeInitialized(_wireframeControl, gumEditorPanel);

        ToolCommands.GuiCommands_Old.Self.Initialize(_wireframeControl);

        _layerService = new Services.LayerService();

        var localizationManager = Builder.Get<LocalizationManager>();

        Wireframe.WireframeObjectManager.Self.Initialize(_wireframeEditControl, _wireframeControl, localizationManager, _layerService);
        _wireframeControl.Initialize(_wireframeEditControl, gumEditorPanel, HotkeyManager.Self);

        // _layerService must be created after _wireframeControl so that the SystemManagers.Default are assigned
        _layerService.Initialize();
        _wireframeControl.ShareLayerReferences(_layerService);

        EditingManager.Self.Initialize(_wireframeContextMenuStrip);



        _scrollbarService.HandleXnaInitialized();


        this._wireframeControl.Parent.Resize += (not, used) =>
        {
            UpdateWireframeControlSizes();
            PluginManager.Self.HandleWireframeResized();
        };

        //this._wireframeControl.MouseClick += wireframeControl1_MouseClick;
        this._wireframeControl.MouseDown += wireframeControl1_MouseDown;


        this._wireframeControl.DragDrop += DragDropManager.Self.HandleFileDragDrop;
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


    public void HandleWireframeInitialized()
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
        this._wireframeControl = new Gum.Wireframe.WireframeControl();
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
            EditingManager.Self.OnRightClick();
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
