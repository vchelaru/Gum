using FlatRedBall.SpecializedXnaControls;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using InputLibrary;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Forms;
using TextureCoordinateSelectionPlugin.ViewModels;
using TextureCoordinateSelectionPlugin.Views;
using Color = System.Drawing.Color;

namespace TextureCoordinateSelectionPlugin.Logic;

#region Enums

public enum RefreshType
{
    Force,
    OnlyIfGrabbed
}

#endregion

public class ControlLogic : IDisposable
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly ITabManager _tabManager;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly ScrollBarLogicWpf _scrollBarLogic;
    private readonly BackgroundManager _backgroundManager;
    private readonly LineGridManager _lineGridManager;
    private readonly NineSliceGuideManager _nineSliceGuideManager;
    private readonly TextureOutlineManager _textureOutlineManager;

    MainControlViewModel ViewModel;

    object oldTextureLeftValue;
    object oldTextureTopValue;
    object oldTextureWidthValue;
    object oldTextureHeightValue;

    /// <summary>
    /// This can be set to false to prevent the
    /// view from refreshing, which we want to do when
    /// the view itself is what set the values
    /// </summary>
    bool shouldRefreshAccordingToVariableSets = true;
    MainControl mainControl;

    SystemManagers SystemManagers => mainControl.InnerControl.SystemManagers;

    Texture2D CurrentTexture
    {
        get => mainControl.InnerControl.CurrentTexture;
        set => mainControl.InnerControl.CurrentTexture = value;
    }

    public ControlLogic(ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        ITabManager tabManager,
        IHotkeyManager hotkeyManager,
        ScrollBarLogicWpf scrollBarLogic,
        MainControlViewModel mainControlViewModel)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _tabManager = tabManager;
        _hotkeyManager = hotkeyManager;
        _scrollBarLogic = scrollBarLogic;

        ViewModel = mainControlViewModel;

        _backgroundManager = new BackgroundManager();
        _lineGridManager = new LineGridManager();
        _nineSliceGuideManager = new NineSliceGuideManager();
        _textureOutlineManager = new TextureOutlineManager();
    }

    public PluginTab CreateControl()
    {
        mainControl = new MainControl();
        //var control = new ImageRegionSelectionControl();
        var innerControl = mainControl.InnerControl;

        innerControl.AvailableZoomLevels = new int[]
        {
            3200,
            1600,
            1200,
            800,
            500,
            300,
            200,
            150,
            100,
            75,
            50,
            33,
            25,
            10,
        };
        innerControl.StartRegionChanged += HandleStartRegionChanged;
        innerControl.RegionChanged += HandleRegionChanged;
        innerControl.EndRegionChanged += HandleEndRegionChanged;
        innerControl.KeyDown += HandleKeyDown;

        //_guiCommands.AddWinformsControl(control, "Texture Coordinates", TabLocation.Right);

        var pluginTab = _tabManager.AddControl(mainControl, "Texture Coordinates", TabLocation.RightBottom);
        innerControl.DoubleClick += (not, used) =>
            HandleRegionDoubleClicked(innerControl);

        ViewModel.AvailableZoomLevels = innerControl.AvailableZoomLevels;
        mainControl.DataContext = ViewModel;

        ViewModel.PropertyChanged += HandleViewModelPropertyChanged;

        _backgroundManager.Initialize(SystemManagers);
        _lineGridManager.Initialize(SystemManagers);
        _nineSliceGuideManager.Initialize(SystemManagers);
        _textureOutlineManager.Initialize(SystemManagers);

        RefreshLineGrid();

        InitializeScrollBarLogic();

        return pluginTab;
    }

    private void InitializeScrollBarLogic()
    {
        _scrollBarLogic.Initialize(mainControl.VerticalScrollBar, mainControl.HorizontalScrollBar, SystemManagers.Renderer.Camera);

        mainControl.InnerControl.SizeChanged += (_, _) =>
        {
            UpdateScrollBarsToTexture();
        };

        mainControl.InnerControl.MouseWheelZoom += (_, _) =>
        {
            UpdateScrollBarsToTexture();
            ViewModel.SelectedZoomLevel = mainControl.InnerControl.ZoomValue;
        };

        mainControl.InnerControl.Panning += () =>
        {
            UpdateScrollBarsToTexture();
        };
    }

    private void HandleKeyDown(object? sender, KeyEventArgs e)
    {
        var camera = mainControl.InnerControl.SystemManagers.Renderer.Camera;
        if (_hotkeyManager.MoveCameraRight.IsPressed(e))
        {
            camera.X += 10;
        }
        if (_hotkeyManager.MoveCameraLeft.IsPressed(e))
        {
            camera.X -= 10;
        }
        if (_hotkeyManager.MoveCameraUp.IsPressed(e))
        {
            camera.Y -= 10;
        }
        if (_hotkeyManager.MoveCameraDown.IsPressed(e))
        {
            camera.Y += 10;
        }
        if (_hotkeyManager.ZoomCameraIn.IsPressed(e) || _hotkeyManager.ZoomCameraInAlternative.IsPressed(e))
        {
            mainControl.InnerControl.HandleZoom(ZoomDirection.ZoomIn, considerCursor: false);
        }
        if (_hotkeyManager.ZoomCameraOut.IsPressed(e) || _hotkeyManager.ZoomCameraOutAlternative.IsPressed(e))
        {
            mainControl.InnerControl.HandleZoom(ZoomDirection.ZoomOut, considerCursor: false);
        }

        UpdateScrollBarsToTexture();

    }

    private void UpdateScrollBarsToTexture()
    {
        var texture = mainControl.InnerControl.CurrentTexture;
        var width = texture?.Width ?? 1024;
        var height = texture?.Height ?? 1024;
        _scrollBarLogic.UpdateScrollBarsToCamera(width, height);
    }

    private void HandleViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        void RefreshSnappingGridSize()
        {
            if (!ViewModel.IsSnapToGridChecked)
            {
                mainControl.InnerControl.SnappingGridSize = null;
            }
            else
            {
                mainControl.InnerControl.SnappingGridSize = ViewModel.SelectedSnapToGridValue;
            }
        }

        switch (e.PropertyName)
        {
            case nameof(MainControlViewModel.IsSnapToGridChecked):
                RefreshSnappingGridSize();
                RefreshLineGrid();

                break;
            case nameof(MainControlViewModel.SelectedSnapToGridValue):
                RefreshSnappingGridSize();
                RefreshLineGrid();
                break;
            case nameof(MainControlViewModel.SelectedZoomLevel):
                mainControl.InnerControl.ZoomValue = ViewModel.SelectedZoomLevel;
                UpdateScrollBarsToTexture();
                break;
        }
    }

    internal void Refresh(Texture2D? textureToAssign, bool showNineSliceGuides, float? customFrameTextureCoordinateWidth)
    {
        mainControl.InnerControl.CurrentTexture = textureToAssign;

        RefreshSelector(Logic.RefreshType.OnlyIfGrabbed);

        _textureOutlineManager.CurrentTexture = textureToAssign;
        _textureOutlineManager.Refresh();

        RefreshLineGrid();

        _nineSliceGuideManager.ShowGuides = showNineSliceGuides;
        _nineSliceGuideManager.CurrentTexture = textureToAssign;
        _nineSliceGuideManager.Selector = mainControl.InnerControl.RectangleSelector;
        _nineSliceGuideManager.CustomFrameWidth = customFrameTextureCoordinateWidth;
        _nineSliceGuideManager.Refresh();
    }

    private void RefreshLineGrid()
    {
        _lineGridManager.IsVisible = ViewModel.IsSnapToGridChecked;
        _lineGridManager.GridSize = ViewModel.SelectedSnapToGridValue;
        _lineGridManager.CurrentTexture = CurrentTexture;
        _lineGridManager.Refresh();
    }

    public void HandleRegionDoubleClicked(ImageRegionSelectionControl control)
    {
        using var undoLock = _undoManager.RequestLock();

        var state = _selectedState.SelectedStateSave;
        var instancePrefix = _selectedState.SelectedInstance?.Name;
        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;

        if (!string.IsNullOrEmpty(instancePrefix))
        {
            instancePrefix += ".";
        }

        if (state != null && graphicalUiElement != null)
        {
            graphicalUiElement.TextureAddress = TextureAddress.Custom;

            var cursorX = (int)control.XnaCursor.GetWorldX(control.SystemManagers);
            var cursorY = (int)control.XnaCursor.GetWorldY(control.SystemManagers);


            var selectionRect = GetOptimalSelectionRectangle(cursorX, cursorY, graphicalUiElement.TextureWidth, graphicalUiElement.TextureHeight);

            graphicalUiElement.TextureLeft = selectionRect.X;
            graphicalUiElement.TextureTop = selectionRect.Y;
            graphicalUiElement.TextureWidth = selectionRect.Width;
            graphicalUiElement.TextureHeight = selectionRect.Height;

            state.SetValue($"{instancePrefix}TextureLeft", selectionRect.X, "int");
            state.SetValue($"{instancePrefix}TextureTop", selectionRect.Y, "int");
            state.SetValue($"{instancePrefix}TextureWidth", selectionRect.Width, "int");
            state.SetValue($"{instancePrefix}TextureHeight", selectionRect.Height, "int");
            state.SetValue($"{instancePrefix}TextureAddress",
                Gum.Managers.TextureAddress.Custom, nameof(TextureAddress));

            _textureOutlineManager.CurrentTexture = control.CurrentTexture;
            _textureOutlineManager.Refresh();

            RefreshSelector(RefreshType.Force);

            UpdateScrollBarsToTexture();

            // We should refresh the entire grid because we could be
            // changing this from Entire Texture to Custom, resulting in
            // new variables being shown
            //_guiCommands.RefreshVariableValues();
            _guiCommands.RefreshVariables();

        }
    }

    private Rectangle GetOptimalSelectionRectangle(int cursorX, int cursorY, int textureWidth, int textureHeight)
    {
        // Default to 64x64 size, centered on the cursor position
        int left = Math.Max(0, cursorX - 32);
        int top = Math.Max(0, cursorY - 32);

        // If they are using the grid size, snap to it instead!
        if (ViewModel.IsSnapToGridChecked)
        {
            var gridSize = ViewModel.SelectedSnapToGridValue;

            // find the top left using division and floor
            left = (cursorX / gridSize) * gridSize;
            top = (cursorY / gridSize) * gridSize;

            // send back the rectangle selection
            return new Rectangle(left, top, ViewModel.SelectedSnapToGridValue, ViewModel.SelectedSnapToGridValue);
        }

        return new Rectangle(left, top, 64, 64);

    }

    private void HandleStartRegionChanged(object? sender, EventArgs e)
    {
        _undoManager.RecordUndo();

        var state = _selectedState.SelectedStateSave;

        var instancePrefix = _selectedState.SelectedInstance?.Name;

        if (!string.IsNullOrEmpty(instancePrefix))
        {
            instancePrefix += ".";
        }

        oldTextureLeftValue = state.GetValue($"{instancePrefix}TextureLeft");
        oldTextureTopValue = state.GetValue($"{instancePrefix}TextureTop");
        oldTextureWidthValue = state.GetValue($"{instancePrefix}TextureWidth");
        oldTextureHeightValue = state.GetValue($"{instancePrefix}TextureHeight");
    }

    private void HandleRegionChanged(object? sender, EventArgs e)
    {
        var control = sender as ImageRegionSelectionControl;

        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;

        if (graphicalUiElement != null)
        {
            var selector = control.RectangleSelector;

            graphicalUiElement.TextureLeft = MathFunctions.RoundToInt(selector.Left);
            graphicalUiElement.TextureTop = MathFunctions.RoundToInt(selector.Top);

            graphicalUiElement.TextureWidth = MathFunctions.RoundToInt(selector.Width);
            graphicalUiElement.TextureHeight = MathFunctions.RoundToInt(selector.Height);

            var state = _selectedState.SelectedStateSave;
            var instancePrefix = _selectedState.SelectedInstance?.Name;

            if (!string.IsNullOrEmpty(instancePrefix))
            {
                instancePrefix += ".";
            }



            state.SetValue($"{instancePrefix}TextureLeft", graphicalUiElement.TextureLeft, "int");
            state.SetValue($"{instancePrefix}TextureTop", graphicalUiElement.TextureTop, "int");
            state.SetValue($"{instancePrefix}TextureWidth", graphicalUiElement.TextureWidth, "int");
            state.SetValue($"{instancePrefix}TextureHeight", graphicalUiElement.TextureHeight, "int");


            _guiCommands.RefreshVariableValues();
        }

        _nineSliceGuideManager.Selector = mainControl.InnerControl.RectangleSelector;
        _nineSliceGuideManager.Refresh();
    }

    private void HandleEndRegionChanged(object? sender, EventArgs e)
    {
        var element = _selectedState.SelectedElement;
        var instance = _selectedState.SelectedInstance;
        var state = _selectedState.SelectedStateSave;

        shouldRefreshAccordingToVariableSets = false;
        {
            // This could be really heavy if we notify everyone of the changes. We should only do it when the editing stops...
            _setVariableLogic.ReactToPropertyValueChanged("TextureLeft", oldTextureLeftValue,
                element, instance, state, refresh: false);
            _setVariableLogic.ReactToPropertyValueChanged("TextureTop", oldTextureTopValue,
                element, instance, state, refresh: false);
            _setVariableLogic.ReactToPropertyValueChanged("TextureWidth", oldTextureWidthValue,
                element, instance, state, refresh: false);
            _setVariableLogic.ReactToPropertyValueChanged("TextureHeight", oldTextureHeightValue,
                element, instance, state, refresh: false);
        }
        shouldRefreshAccordingToVariableSets = true;

        _undoManager.RecordUndo();

        _fileCommands.TryAutoSaveCurrentElement();
    }

    public void RefreshSelector(RefreshType refreshType)
    {
        if (mainControl.InnerControl.CurrentTexture == null)
        {
            return;
        }

        var control = mainControl.InnerControl;

        // early out
        if (refreshType == RefreshType.OnlyIfGrabbed &&
            control.RectangleSelector != null &&
            control.RectangleSelector.SideGrabbed != FlatRedBall.SpecializedXnaControls.RegionSelection.ResizeSide.None)
        {
            return;
        }

        if (shouldRefreshAccordingToVariableSets == false)
        {
            return;
        }

        if (_selectedState.SelectedElement == null)
        {
            // in case a behavior is selected:
            return;
        }

        //////////////end early out///////////////////////////////

        var shouldClearOut = true;
        if (_selectedState.SelectedStateSave != null)
        {

            var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;
            var rfv = new RecursiveVariableFinder(_selectedState.SelectedStateSave);
            var instancePrefix = _selectedState.SelectedInstance?.Name;

            if (!string.IsNullOrEmpty(instancePrefix))
            {
                instancePrefix += ".";
            }

            var textureAddress = rfv.GetValue<Gum.Managers.TextureAddress>($"{instancePrefix}TextureAddress");
            if (textureAddress == Gum.Managers.TextureAddress.Custom)
            {
                shouldClearOut = false;
                control.DesiredSelectorCount = 1;

                var selector = control.RectangleSelector;


                selector.Left = rfv.GetValue<int>($"{instancePrefix}TextureLeft");
                selector.Width = rfv.GetValue<int>($"{instancePrefix}TextureWidth");

                selector.Top = rfv.GetValue<int>($"{instancePrefix}TextureTop");
                selector.Height = rfv.GetValue<int>($"{instancePrefix}TextureHeight");

                selector.Visible = true;
                selector.ShowHandles = true;
                selector.ShowMoveCursorWhenOver = true;

                this.CenterCameraOnSelection();

            }
            else if (textureAddress == TextureAddress.DimensionsBased)
            {
                shouldClearOut = false;
                control.DesiredSelectorCount = 1;
                var selector = control.RectangleSelector;

                selector.Left = rfv.GetValue<int>($"{instancePrefix}TextureLeft");
                selector.Top = rfv.GetValue<int>($"{instancePrefix}TextureTop");

                var widthScale = rfv.GetValue<float>($"{instancePrefix}TextureWidthScale");
                var heightScale = rfv.GetValue<float>($"{instancePrefix}TextureHeightScale");

                var absoluteWidth = graphicalUiElement.GetAbsoluteWidth();
                var absoluteHeight = graphicalUiElement.GetAbsoluteHeight();

                selector.Width = absoluteWidth / widthScale;
                selector.Height = absoluteHeight / heightScale;

                selector.Visible = true;
                selector.ShowHandles = false;
                selector.AllowMoveWithoutHandles = true;
                selector.ShowMoveCursorWhenOver = true;
            }
        }

        if (shouldClearOut)
        {
            control.DesiredSelectorCount = 0;
        }
    }

    public async void CenterCameraOnSelection()
    {
        var camera = SystemManagers.Renderer.Camera;

        // For Vic K:
        // I could not figure
        // out how to get the control
        // to be fully laid out here. I
        // had to put a delay of 100 ms and
        // all works, but this feels dirty. I've
        // tried looking at the control's ActualWidth 
        // and ActualHeight, but that still returned 0
        // uless I had the delay.
        await Task.Delay(100);
        mainControl.UpdateLayout();
        var selector = mainControl.InnerControl.RectangleSelector;
        if(selector != null)
        {
            camera.X = selector.Left + selector.Width / 2.0f - camera.ClientWidth/(2 * camera.Zoom);
            camera.Y = selector.Top + selector.Height / 2.0f - camera.ClientHeight/(2 * camera.Zoom);
            UpdateScrollBarsToTexture();
        }

    }

    public void Dispose()
    {
        _backgroundManager?.Dispose();
    }
}
