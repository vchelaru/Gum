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
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using RenderingLibrary.Math.Geometry;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using TextureCoordinateSelectionPlugin.Models;
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

public class TextureCoordinateDisplayController : IDisposable
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

    bool _isSnapToGridEnabled;
    int _snapToGridSize;

    internal event Action<int>? ZoomLevelChanged;

    ExposedTextureCoordinateSet? _currentExposedSource;

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

    public TextureCoordinateDisplayController(ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        ITabManager tabManager,
        IHotkeyManager hotkeyManager,
        ScrollBarLogicWpf scrollBarLogic)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _tabManager = tabManager;
        _hotkeyManager = hotkeyManager;
        _scrollBarLogic = scrollBarLogic;

        _backgroundManager = new BackgroundManager();
        _lineGridManager = new LineGridManager();
        _nineSliceGuideManager = new NineSliceGuideManager();
        _textureOutlineManager = new TextureOutlineManager();
    }

    public PluginTab CreateControl(object dataContext, out IList<int> availableZoomLevels)
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

        availableZoomLevels = innerControl.AvailableZoomLevels;
        mainControl.DataContext = dataContext;

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
            ZoomLevelChanged?.Invoke(mainControl.InnerControl.ZoomValue);
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

    internal void SetCurrentExposedSource(ExposedTextureCoordinateSet? source)
    {
        _currentExposedSource = source;
    }

    internal void UpdateZoom(int zoomLevel)
    {
        mainControl.InnerControl.ZoomValue = zoomLevel;
        UpdateScrollBarsToTexture();
    }

    internal void UpdateSnapGrid(bool isEnabled, int gridSize)
    {
        _isSnapToGridEnabled = isEnabled;
        _snapToGridSize = gridSize;

        if (!_isSnapToGridEnabled)
        {
            mainControl.InnerControl.SnappingGridSize = null;
        }
        else
        {
            mainControl.InnerControl.SnappingGridSize = _snapToGridSize;
        }
        RefreshLineGrid();
    }

    private GraphicalUiElement? FindExposedSourceChild(GraphicalUiElement parent)
    {
        if (_currentExposedSource?.SourceObjectName == null) return null;
        return parent.Children
            .FirstOrDefault(c => c.Name == _currentExposedSource.SourceObjectName) as GraphicalUiElement;
    }

    internal void Refresh()
    {
        var textureToAssign = GetTextureToAssign(out bool showNineSliceGuides, out float? customFrameTextureCoordinateWidth);

        if (textureToAssign?.IsDisposed == true)
        {
            textureToAssign = null;
        }

        mainControl.InnerControl.CurrentTexture = textureToAssign;

        if (_currentExposedSource != null)
        {
            mainControl.InnerControl.CanChangeX = _currentExposedSource.ExposedLeftName != null;
            mainControl.InnerControl.CanChangeY = _currentExposedSource.ExposedTopName != null;
            mainControl.InnerControl.CanChangeWidth = _currentExposedSource.ExposedWidthName != null;
            mainControl.InnerControl.CanChangeHeight = _currentExposedSource.ExposedHeightName != null;
        }
        else
        {
            mainControl.InnerControl.CanChangeX = true;
            mainControl.InnerControl.CanChangeY = true;
            mainControl.InnerControl.CanChangeWidth = true;
            mainControl.InnerControl.CanChangeHeight = true;
        }

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

    private Texture2D? GetTextureToAssign(out bool isNineslice, out float? customFrameTextureCoordinateWidth)
    {
        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;
        isNineslice = false;
        customFrameTextureCoordinateWidth = null;
        Texture2D? textureToAssign = null;

        if (graphicalUiElement != null)
        {
            var containedRenderable = graphicalUiElement.RenderableComponent;

            if (containedRenderable is Sprite asSprite)
            {
                textureToAssign = asSprite.Texture;
            }
            else if (containedRenderable is NineSlice nineSlice)
            {
                isNineslice = true;
                customFrameTextureCoordinateWidth = nineSlice.CustomFrameTextureCoordinateWidth;
                var isUsingSameTextures =
                    nineSlice.TopLeftTexture == nineSlice.CenterTexture &&
                    nineSlice.TopTexture == nineSlice.CenterTexture &&
                    nineSlice.TopRightTexture == nineSlice.CenterTexture &&
                    nineSlice.LeftTexture == nineSlice.CenterTexture &&
                    nineSlice.RightTexture == nineSlice.CenterTexture &&
                    nineSlice.BottomLeftTexture == nineSlice.CenterTexture &&
                    nineSlice.BottomTexture == nineSlice.CenterTexture &&
                    nineSlice.BottomRightTexture == nineSlice.CenterTexture;

                if (isUsingSameTextures)
                {
                    textureToAssign = nineSlice.CenterTexture;
                }
            }

            if (textureToAssign == null && _currentExposedSource != null)
            {
                var innerChild = graphicalUiElement.Children
                    .FirstOrDefault(c => c.Name == _currentExposedSource.SourceObjectName);

                if (innerChild is GraphicalUiElement innerGue)
                {
                    var innerRenderable = innerGue.RenderableComponent;
                    if (innerRenderable is Sprite innerSprite)
                    {
                        textureToAssign = innerSprite.Texture;
                    }
                    else if (innerRenderable is NineSlice innerNineSlice)
                    {
                        isNineslice = true;
                        customFrameTextureCoordinateWidth = innerNineSlice.CustomFrameTextureCoordinateWidth;
                        var isUsingSameTextures =
                            innerNineSlice.TopLeftTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.TopTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.TopRightTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.LeftTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.RightTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.BottomLeftTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.BottomTexture == innerNineSlice.CenterTexture &&
                            innerNineSlice.BottomRightTexture == innerNineSlice.CenterTexture;

                        if (isUsingSameTextures)
                        {
                            textureToAssign = innerNineSlice.CenterTexture;
                        }
                    }
                }
            }
        }

        return textureToAssign;
    }

    private void RefreshLineGrid()
    {
        _lineGridManager.IsVisible = _isSnapToGridEnabled;
        _lineGridManager.GridSize = _snapToGridSize;
        _lineGridManager.CurrentTexture = CurrentTexture;
        _lineGridManager.Refresh();
    }

    public void HandleRegionDoubleClicked(ImageRegionSelectionControl control)
    {
        if (_currentExposedSource != null) return;

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
        if (_isSnapToGridEnabled)
        {
            // find the top left using division and floor
            left = (cursorX / _snapToGridSize) * _snapToGridSize;
            top = (cursorY / _snapToGridSize) * _snapToGridSize;

            // send back the rectangle selection
            return new Rectangle(left, top, _snapToGridSize, _snapToGridSize);
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

        if (_currentExposedSource != null)
        {
            oldTextureLeftValue = _currentExposedSource.ExposedLeftName != null ? state.GetValue($"{instancePrefix}{_currentExposedSource.ExposedLeftName}") : null;
            oldTextureTopValue = _currentExposedSource.ExposedTopName != null ? state.GetValue($"{instancePrefix}{_currentExposedSource.ExposedTopName}") : null;
            oldTextureWidthValue = _currentExposedSource.ExposedWidthName != null ? state.GetValue($"{instancePrefix}{_currentExposedSource.ExposedWidthName}") : null;
            oldTextureHeightValue = _currentExposedSource.ExposedHeightName != null ? state.GetValue($"{instancePrefix}{_currentExposedSource.ExposedHeightName}") : null;
        }
        else
        {
            oldTextureLeftValue = state.GetValue($"{instancePrefix}TextureLeft");
            oldTextureTopValue = state.GetValue($"{instancePrefix}TextureTop");
            oldTextureWidthValue = state.GetValue($"{instancePrefix}TextureWidth");
            oldTextureHeightValue = state.GetValue($"{instancePrefix}TextureHeight");
        }
    }

    private void HandleRegionChanged(object? sender, EventArgs e)
    {
        var control = sender as ImageRegionSelectionControl;

        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;

        if (graphicalUiElement != null)
        {
            var selector = control.RectangleSelector;

            var state = _selectedState.SelectedStateSave;
            var instancePrefix = _selectedState.SelectedInstance?.Name;

            if (!string.IsNullOrEmpty(instancePrefix))
            {
                instancePrefix += ".";
            }

            if (_currentExposedSource != null)
            {
                var innerChild = FindExposedSourceChild(graphicalUiElement);
                if (innerChild != null)
                {
                    innerChild.TextureLeft = MathFunctions.RoundToInt(selector.Left);
                    innerChild.TextureTop = MathFunctions.RoundToInt(selector.Top);
                    innerChild.TextureWidth = MathFunctions.RoundToInt(selector.Width);
                    innerChild.TextureHeight = MathFunctions.RoundToInt(selector.Height);

                    if (_currentExposedSource.ExposedLeftName != null)
                        state.SetValue($"{instancePrefix}{_currentExposedSource.ExposedLeftName}", innerChild.TextureLeft, "int");
                    if (_currentExposedSource.ExposedTopName != null)
                        state.SetValue($"{instancePrefix}{_currentExposedSource.ExposedTopName}", innerChild.TextureTop, "int");
                    if (_currentExposedSource.ExposedWidthName != null)
                        state.SetValue($"{instancePrefix}{_currentExposedSource.ExposedWidthName}", innerChild.TextureWidth, "int");
                    if (_currentExposedSource.ExposedHeightName != null)
                        state.SetValue($"{instancePrefix}{_currentExposedSource.ExposedHeightName}", innerChild.TextureHeight, "int");
                }
            }
            else
            {
                graphicalUiElement.TextureLeft = MathFunctions.RoundToInt(selector.Left);
                graphicalUiElement.TextureTop = MathFunctions.RoundToInt(selector.Top);
                graphicalUiElement.TextureWidth = MathFunctions.RoundToInt(selector.Width);
                graphicalUiElement.TextureHeight = MathFunctions.RoundToInt(selector.Height);

                state.SetValue($"{instancePrefix}TextureLeft", graphicalUiElement.TextureLeft, "int");
                state.SetValue($"{instancePrefix}TextureTop", graphicalUiElement.TextureTop, "int");
                state.SetValue($"{instancePrefix}TextureWidth", graphicalUiElement.TextureWidth, "int");
                state.SetValue($"{instancePrefix}TextureHeight", graphicalUiElement.TextureHeight, "int");
            }

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
            if (_currentExposedSource != null)
            {
                if (_currentExposedSource.ExposedLeftName != null)
                    _setVariableLogic.ReactToPropertyValueChanged(_currentExposedSource.ExposedLeftName, oldTextureLeftValue,
                        element, instance, state, refresh: false);
                if (_currentExposedSource.ExposedTopName != null)
                    _setVariableLogic.ReactToPropertyValueChanged(_currentExposedSource.ExposedTopName, oldTextureTopValue,
                        element, instance, state, refresh: false);
                if (_currentExposedSource.ExposedWidthName != null)
                    _setVariableLogic.ReactToPropertyValueChanged(_currentExposedSource.ExposedWidthName, oldTextureWidthValue,
                        element, instance, state, refresh: false);
                if (_currentExposedSource.ExposedHeightName != null)
                    _setVariableLogic.ReactToPropertyValueChanged(_currentExposedSource.ExposedHeightName, oldTextureHeightValue,
                        element, instance, state, refresh: false);
            }
            else
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

            if (_currentExposedSource != null && graphicalUiElement != null)
            {
                var innerChild = FindExposedSourceChild(graphicalUiElement);
                if (innerChild != null &&
                    (Gum.Managers.TextureAddress)innerChild.TextureAddress == Gum.Managers.TextureAddress.Custom)
                {
                    shouldClearOut = false;
                    control.DesiredSelectorCount = 1;
                    var selector = control.RectangleSelector;

                    selector.Left = innerChild.TextureLeft;
                    selector.Top = innerChild.TextureTop;
                    selector.Width = innerChild.TextureWidth;
                    selector.Height = innerChild.TextureHeight;

                    selector.Visible = true;
                    selector.ShowHandles = true;
                    selector.ShowMoveCursorWhenOver = _currentExposedSource.ExposedLeftName != null || _currentExposedSource.ExposedTopName != null;

                    this.CenterCameraOnSelection();
                }
            }
            else
            {
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

    internal void UpdateButtonSizes(double baseFontSize)
    {
        mainControl?.UpdateButtonSizes(baseFontSize);
    }

    public void Dispose()
    {
        _backgroundManager?.Dispose();
    }
}
