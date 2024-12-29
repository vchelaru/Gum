using FlatRedBall.AnimationEditorForms.Controls;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Plugins.ScrollBarPlugin;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls.Primitives;
using System.Windows.Forms;

namespace Gum.Plugins.InternalPlugins.EditorTab;

[Export(typeof(PluginBase))]
internal class MainEditorTabPlugin : InternalPlugin
{

    public static MainEditorTabPlugin Self
    {
        get;
        private set;
    }

    readonly ScrollbarService _scrollbarService;
    WireframeControl _wireframeControl;
    private FlowLayoutPanel _toolbarPanel;

    public MainEditorTabPlugin()
    {
        _scrollbarService = new ScrollbarService();
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

        this.CameraChanged += _scrollbarService.HandleCameraChanged;
        this.XnaInitialized += HandleXnaInitialized;
        this.WireframeResized += _scrollbarService.HandleWireframeResized;
        this.ElementSelected += _scrollbarService.HandleElementSelected;
    }

    private void HandleXnaInitialized()
    {
        _scrollbarService.HandleXnaInitialized();


        this._wireframeControl.Parent.Resize += (not, used) =>
        {
            UpdateWireframeControlSizes();
            PluginManager.Self.HandleWireframeResized();
        };

        this._wireframeControl.MouseClick += wireframeControl1_MouseClick;

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

    public void HandleWireframeInitialized(WireframeControl wireframeControl1, WireframeEditControl wireframeEditControl, 
        System.Windows.Forms.Cursor addCursor, System.Windows.Forms.Panel gumEditorPanel, FlowLayoutPanel toolbarPanel)
    {
        GumCommands.Self.GuiCommands.AddControl(gumEditorPanel, "Editor", TabLocation.RightTop);


        _wireframeControl = wireframeControl1;
        _toolbarPanel = toolbarPanel;
        wireframeControl1.XnaUpdate += () =>
        {
            Wireframe.WireframeObjectManager.Self.Activity();
            ToolLayerService.Self.Activity();
        };

        _scrollbarService.HandleWireframeInitialized(wireframeControl1, gumEditorPanel);

        ToolCommands.GuiCommands.Self.Initialize(wireframeControl1);

        Wireframe.WireframeObjectManager.Self.Initialize(wireframeEditControl, wireframeControl1, addCursor);
        wireframeControl1.Initialize(wireframeEditControl, gumEditorPanel, HotkeyManager.Self);

    }

    /// <summary>
    /// Refreshes the wifreframe control size - for some reason this is necessary if windows has a non-100% scale (for higher resolution displays)
    /// </summary>
    private void UpdateWireframeControlSizes()
    {
        // I don't think we need this for docking:
        //WireframeEditControl.Width = WireframeEditControl.Parent.Width / 2;

        _toolbarPanel.Width = _toolbarPanel.Parent.Width;

        _wireframeControl.Width = _wireframeControl.Parent.Width;

        // Add location.Y to account for the shortcut bar at the top.
        _wireframeControl.Height = _wireframeControl.Parent.Height - _wireframeControl.Location.Y;
    }

    private void HandleInstanceSelected(ElementSave element, InstanceSave instance)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: false);
    }

    private void HandleStateSelected(StateSave save)
    {
        WireframeObjectManager.Self.RefreshAll(forceLayout: true);
    }

    private void wireframeControl1_MouseClick(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Right)
        {
            EditingManager.Self.OnRightClick();
        }
    }
}
