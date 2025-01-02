using FlatRedBall.AnimationEditorForms.Controls;
using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ScrollBarPlugin;
using Gum.ToolStates;
using Gum.Wireframe;
using GumRuntime;
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
    static HashSet<string> PropertiesSupportingIncrementalChange = new HashSet<string>
        {
            "Animate",
            "Alpha",
            "AutoGridHorizontalCells",
            "AutoGridVerticalCells",
            "Blue",
            "CurrentChainName",
            "Children Layout",
            "FlipHorizontal",
            "FontSize",
            "Green",
            "Height",
            "Height Units",
            "HorizontalAlignment",
            nameof(GraphicalUiElement.IgnoredByParentSize),
            "IsBold",
            "MaxLettersToShow",
            nameof(Text.MaxNumberOfLines),
            "Red",
            "Rotation",
            "StackSpacing",
            "Text",
            "Texture Address",
            "TextOverflowVerticalMode",
            "UseCustomFont",
            "UseFontSmoothing",
            "VerticalAlignment",
            "Visible",
            "Width",
            "Width Units",
            "X",
            "X Origin",
            "X Units",
            "Y",
            "Y Origin",
            "Y Units",
        };

    public static MainEditorTabPlugin Self
    {
        get;
        private set;
    }

    readonly ScrollbarService _scrollbarService;
    private readonly GuiCommands _guiCommands;
    WireframeControl _wireframeControl;

    private FlatRedBall.AnimationEditorForms.Controls.WireframeEditControl _wireframeEditControl;
    private int _defaultWireframeEditControlHeight;

    Panel gumEditorPanel;



    public MainEditorTabPlugin()
    {
        _scrollbarService = new ScrollbarService();
        _guiCommands = GumCommands.Self.GuiCommands;
        Self = this;
    }

    public override void StartUp()
    {
        AssignEvents();


    }

    private void AssignEvents()
    {
        this.ReactToStateSaveSelected += HandleStateSelected;
        this.InstanceSelected += HandleInstanceSelected;
        this.ElementSelected += HandleElementSelected;
        this.VariableSetLate += HandleVariableSetLate;

        this.CameraChanged += _scrollbarService.HandleCameraChanged;
        this.XnaInitialized += HandleXnaInitialized;
        this.WireframeResized += _scrollbarService.HandleWireframeResized;
        this.ElementSelected += _scrollbarService.HandleElementSelected;
        this.UiZoomValueChanged += HandleUiZoomValueChanged;
    }

    private void HandleUiZoomValueChanged()
    {
        // Uncommenting this makes the area for teh combo box properly grow, but it
        // kills the wireframe view. Not sure why....
        //_wireframeEditControl.Height = _defaultWireframeEditControlHeight * _guiCommands.UiZoomValue;
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

        // If the values are the same they may have been set to be the same by a plugin that
        // didn't allow the assignment, so don't go through the work of saving and refreshing
        if (!areSame)
        {
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
                    gue.SetProperty(unqualifiedMember, value);

                    WireframeObjectManager.Self.RootGue?.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);
                    //gue.ApplyVariableReferences(SelectedState.Self.SelectedStateSave);

                    handledByDirectSet = true;
                }
                if (unqualifiedMember == "Text" && LocalizationManager.HasDatabase)
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

    public void HandleWireframeInitialized(
        System.Windows.Forms.ContextMenuStrip wireframeContextMenuStrip,
        System.Windows.Forms.Cursor addCursor)
    {
        gumEditorPanel = new Panel();

        CreateWireframeEditControl(gumEditorPanel);

        CreateWireframeControl(wireframeContextMenuStrip);

        GumCommands.Self.GuiCommands.AddControl(gumEditorPanel, "Editor", TabLocation.RightTop);


        _wireframeControl.XnaUpdate += () =>
        {
            Wireframe.WireframeObjectManager.Self.Activity();
            ToolLayerService.Self.Activity();
        };

        _scrollbarService.HandleWireframeInitialized(_wireframeControl, gumEditorPanel);

        ToolCommands.GuiCommands.Self.Initialize(_wireframeControl);

        Wireframe.WireframeObjectManager.Self.Initialize(_wireframeEditControl, _wireframeControl, addCursor);
        _wireframeControl.Initialize(_wireframeEditControl, gumEditorPanel, HotkeyManager.Self);

        EditingManager.Self.Initialize(wireframeContextMenuStrip);
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
