using CommunityToolkit.Mvvm.Messaging;
using FlatRedBall.SpecializedXnaControls;
using TextureCoordinateSelectionPlugin.RegionSelection;
using Gum;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Dialogs;
using Gum.Input;
using Gum.Managers;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.PropertyGridHelpers;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using Gum.Wireframe;
using ToolsUtilities;
using InputLibrary;
using Cursor = InputLibrary.Cursor;
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

/// <summary>The pointer gesture that can apply a plugin-reported region as the texture rectangle.</summary>
public enum RegionGesture
{
    Click,
    DoubleClick
}

#endregion

public class TextureCoordinateDisplayController : ITextureCoordinateDisplayController, IDisposable
{
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IGuiCommands _guiCommands;
    private readonly IFileCommands _fileCommands;
    private readonly ISetVariableLogic _setVariableLogic;
    private readonly ITabManager _tabManager;
    private readonly IHotkeyManager _hotkeyManager;
    private readonly CameraScrollBarBinder _scrollBarLogic;
    private readonly CameraController _cameraController;
    private readonly BackgroundManager _backgroundManager;
    private readonly LineGridManager _lineGridManager;
    private readonly NineSliceGuideManager _nineSliceGuideManager;
    private readonly TextureOutlineManager _textureOutlineManager;
    private readonly TextureRegionOverlayManager _regionOverlayManager;

    /// <summary>The overlay that outlines plugin-reported regions; exposed so tests can see what it draws.</summary>
    public TextureRegionOverlayManager RegionOverlay => _regionOverlayManager;
    private readonly IPluginManager? _pluginManager;
    private readonly Dictionary<string, IReadOnlyList<TextureRegion>> _regionCache = new Dictionary<string, IReadOnlyList<TextureRegion>>();
    private TextureRegionSet _regionSet = TextureRegionSet.Empty;
    private string? _regionSetKey;

    // The one decision point for which gesture applies a plugin region. Double-click is the only choice
    // for now; a user setting can make it Click later without touching the code below.
    private readonly RegionGesture _regionApplyGesture = RegionGesture.DoubleClick;
    private readonly CanvasDisplayScale _displayScale = new CanvasDisplayScale();

    bool _isSnapToGridEnabled;
    int _snapToGridSize;

    public event Action<int>? ZoomLevelChanged;

    ExposedTextureCoordinateSet? _currentExposedSource;

    object? oldTextureLeftValue;
    object? oldTextureTopValue;
    object? oldTextureWidthValue;
    object? oldTextureHeightValue;

    /// <summary>
    /// This can be set to false to prevent the
    /// view from refreshing, which we want to do when
    /// the view itself is what set the values
    /// </summary>
    bool shouldRefreshAccordingToVariableSets = true;
    private ITextureCoordinateView _view = null!;

    SystemManagers SystemManagers => _view.Canvas.SystemManagers;

    Texture2D? CurrentTexture
    {
        get => _view.Canvas.CurrentTexture;
        set => _view.Canvas.CurrentTexture = value;
    }

    public TextureCoordinateDisplayController(ISelectedState selectedState,
        IUndoManager undoManager,
        IGuiCommands guiCommands,
        IFileCommands fileCommands,
        ISetVariableLogic setVariableLogic,
        ITabManager tabManager,
        IHotkeyManager hotkeyManager,
        CameraScrollBarBinder scrollBarLogic,
        IMessenger messenger,
        IThemingService themingService,
        IPluginManager? pluginManager = null)
    {
        _selectedState = selectedState;
        _undoManager = undoManager;
        _guiCommands = guiCommands;
        _fileCommands = fileCommands;
        _setVariableLogic = setVariableLogic;
        _tabManager = tabManager;
        _hotkeyManager = hotkeyManager;
        _scrollBarLogic = scrollBarLogic;
        _cameraController = new CameraController();

        _backgroundManager = new BackgroundManager(messenger, themingService);
        _lineGridManager = new LineGridManager();
        _nineSliceGuideManager = new NineSliceGuideManager(_displayScale);
        _textureOutlineManager = new TextureOutlineManager(_displayScale);
        _regionOverlayManager = new TextureRegionOverlayManager(_displayScale);
        _pluginManager = pluginManager;
    }

    public IPluginTab CreateControl(ITextureCoordinateView view, object dataContext, IList<int> availableZoomLevels)
    {
        _view = view;
        var innerControl = _view.Canvas;

        innerControl.AvailableZoomLevels = availableZoomLevels;
        innerControl.StartRegionChanged += HandleStartRegionChanged;
        innerControl.RegionChanged += HandleRegionChanged;
        innerControl.EndRegionChanged += HandleEndRegionChanged;
        innerControl.DisplayScaleChanged += HandleDisplayScaleChanged;
        _view.KeyDown += HandleKeyDown;
        _view.KeyUp += HandleKeyUp;
        _view.KeyboardFocusLost += HandleKeyboardFocusLost;
        _view.MouseDown += HandleMouseDown;
        _view.MouseMove += HandleMouseMove;
        _view.MouseUp += HandleMouseUp;
        _view.MouseWheel += _cameraController.HandleMouseWheel;

        //_guiCommands.AddWinformsControl(control, "Texture Coordinates", TabLocation.Right);

        var pluginTab = _tabManager.AddControl(_view.View, "Texture Coordinates", TabLocation.RightBottom);
        innerControl.DoubleClick += (_, _) =>
            HandleRegionDoubleClicked(innerControl);

        _view.DataContext = dataContext;

        _backgroundManager.Initialize(SystemManagers);
        _lineGridManager.Initialize(SystemManagers);
        _nineSliceGuideManager.Initialize(SystemManagers);
        _textureOutlineManager.Initialize(SystemManagers);
        _regionOverlayManager.Initialize(SystemManagers);

        RefreshLineGrid();

        _cameraController.Initialize(SystemManagers.Renderer.Camera, new CanvasZoomController(innerControl), _hotkeyManager);
        _cameraController.CameraChanged += HandleCameraChanged;

        InitializeScrollBarLogic();

        return pluginTab;
    }

    private void InitializeScrollBarLogic()
    {
        _scrollBarLogic.Initialize(_view.VerticalScrollBar, _view.HorizontalScrollBar, SystemManagers.Renderer.Camera);

        _view.Canvas.ViewSizeChanged += () =>
        {
            _view.Canvas.ClampCameraToTexture();
            UpdateScrollBarsToTexture();
        };
    }

    private void HandleDisplayScaleChanged()
    {
        _displayScale.DisplayScale = _view.Canvas.DisplayScale;
        _textureOutlineManager.Refresh();
        _nineSliceGuideManager.Refresh();
    }

    private void HandleCameraChanged()
    {
        _view.Canvas.ClampCameraToTexture();
        UpdateScrollBarsToTexture();
        // The hovered region's name keeps a constant size on screen, so it follows the zoom.
        _regionOverlayManager.Refresh();
        ZoomLevelChanged?.Invoke(_view.Canvas.ZoomValue);
    }

    private void HandleMouseDown(GumMouseEventArgs e)
    {
        _cameraController.HandleMouseDown(e);
        SyncCameraPanning();
    }

    private void HandleMouseMove(GumMouseEventArgs e)
    {
        _cameraController.HandleMouseMove(e);
        SyncCameraPanning();
        UpdateHoveredRegion(e.X, e.Y);
    }

    private void UpdateHoveredRegion(int screenX, int screenY)
    {
        if (_regionSet.IsEmpty)
        {
            return;
        }

        SystemManagers.Renderer.Camera.ScreenToWorld(screenX, screenY, out float worldX, out float worldY);
        _regionOverlayManager.Hovered = _cameraController.IsPanning ? null : _regionSet.HitTest(worldX, worldY);
    }

    private void HandleMouseUp(GumMouseEventArgs e)
    {
        _cameraController.HandleMouseUp(e);
        SyncCameraPanning();
    }

    private void HandleKeyUp(GumKeyEventArgs e)
    {
        _cameraController.HandleKeyUp(e);
        SyncCameraPanning();
    }

    private void HandleKeyboardFocusLost()
    {
        _cameraController.HandleFocusLost();
        SyncCameraPanning();
    }

    // The canvas polls its own cursor each frame for the region handles, so it has to be told when
    // a drag belongs to the camera instead.
    private void SyncCameraPanning() => _view.Canvas.IsCameraPanning = _cameraController.IsPanning;

    internal void HandleKeyDown(GumKeyEventArgs keyArgs) =>
        HandleKeyDown(keyArgs, handled => keyArgs.Handled = handled);

    // Split from the WPF handler above so the app-wide/camera key routing can be tested without a
    // real WPF key event (KeyEventArgs needs a live PresentationSource to construct).
    internal void HandleKeyDown(GumKeyEventArgs keyArgs, Action<bool> setHandled)
    {
        // MainWindow's PreviewKeyDown tunnel already offers app-wide hotkeys (Undo/Redo/etc.) to
        // this canvas, so this is a backstop for keys that reach it directly. Entire-app zoom stays
        // disabled because Ctrl+=/Ctrl+- zoom this control's own camera below - CameraZoomScope is
        // what keeps MainWindow from claiming those first.
        if (_hotkeyManager.PreviewKeyDownAppWide(keyArgs, enableEntireAppZoom: false))
        {
            setHandled(keyArgs.Handled);
            return;
        }

        if (IsCanvasNavigationKey(keyArgs))
        {
            setHandled(true);
        }

        _cameraController.HandleKeyPress(keyArgs);
    }

    /// <summary>
    /// Whether a key belongs to the canvas rather than to WPF's focus navigation. The arrow keys pan
    /// the camera here and nudge the selected region (through the polled <c>InputLibrary.Keyboard</c>),
    /// so letting WPF navigate on them would move focus off the canvas and kill both.
    /// </summary>
    internal static bool IsCanvasNavigationKey(GumKeyEventArgs e) =>
        e.Key is GumKey.Left or GumKey.Right or GumKey.Up or GumKey.Down;

    private void UpdateScrollBarsToTexture()
    {
        var texture = _view.Canvas.CurrentTexture;
        var width = texture?.Width ?? 1024;
        var height = texture?.Height ?? 1024;
        _scrollBarLogic.UpdateScrollBarsToCamera(width, height);
    }

    public void SetCurrentExposedSource(ExposedTextureCoordinateSet? source)
    {
        _currentExposedSource = source;
    }

    public void UpdateZoom(int zoomLevel)
    {
        _view.Canvas.ZoomValue = zoomLevel;
        UpdateScrollBarsToTexture();
    }

    public void UpdateSnapGrid(bool isEnabled, int gridSize)
    {
        _isSnapToGridEnabled = isEnabled;
        _snapToGridSize = gridSize;

        if (!_isSnapToGridEnabled)
        {
            _view.Canvas.SnappingGridSize = null;
        }
        else
        {
            _view.Canvas.SnappingGridSize = _snapToGridSize;
        }
        RefreshLineGrid();
    }

    private GraphicalUiElement? FindExposedSourceChild(GraphicalUiElement parent)
    {
        if (_currentExposedSource?.SourceObjectName == null) return null;
        return parent.Children
            .FirstOrDefault(c => c.Name == _currentExposedSource.SourceObjectName) as GraphicalUiElement;
    }

    public void Refresh()
    {
        var textureToAssign = GetTextureToAssign(out bool showNineSliceGuides, out float? customFrameTextureCoordinateWidth);

        if (textureToAssign?.IsDisposed == true)
        {
            textureToAssign = null;
        }

        _view.Canvas.CurrentTexture = textureToAssign;

        if (_currentExposedSource != null)
        {
            _view.Canvas.CanChangeX = _currentExposedSource.ExposedLeftName != null;
            _view.Canvas.CanChangeY = _currentExposedSource.ExposedTopName != null;
            _view.Canvas.CanChangeWidth = _currentExposedSource.ExposedWidthName != null;
            _view.Canvas.CanChangeHeight = _currentExposedSource.ExposedHeightName != null;
        }
        else
        {
            _view.Canvas.CanChangeX = true;
            _view.Canvas.CanChangeY = true;
            _view.Canvas.CanChangeWidth = true;
            _view.Canvas.CanChangeHeight = true;

            var instance = _selectedState.SelectedInstance;
            if (instance != null)
            {
                var instanceElement = ObjectFinder.Self.GetElementSave(instance);
                if (instanceElement != null)
                {
                    if (ObjectFinder.Self.IsVariableHiddenRecursively(instanceElement, "TextureLeft"))
                    {
                        _view.Canvas.CanChangeX = false;
                    }
                    if (ObjectFinder.Self.IsVariableHiddenRecursively(instanceElement, "TextureTop"))
                    {
                        _view.Canvas.CanChangeY = false;
                    }
                    if (ObjectFinder.Self.IsVariableHiddenRecursively(instanceElement, "TextureWidth"))
                    {
                        _view.Canvas.CanChangeWidth = false;
                    }
                    if (ObjectFinder.Self.IsVariableHiddenRecursively(instanceElement, "TextureHeight"))
                    {
                        _view.Canvas.CanChangeHeight = false;
                    }
                }
            }
        }

        RefreshSelector(Logic.RefreshType.OnlyIfGrabbed);

        _textureOutlineManager.CurrentTexture = textureToAssign;
        _textureOutlineManager.Refresh();

        RefreshLineGrid();

        _nineSliceGuideManager.ShowGuides = showNineSliceGuides;
        _nineSliceGuideManager.CurrentTexture = textureToAssign;
        _nineSliceGuideManager.Selector = _view.Canvas.RectangleSelector;
        _nineSliceGuideManager.CustomFrameWidth = customFrameTextureCoordinateWidth;
        _nineSliceGuideManager.Refresh();

        RefreshRegions(textureToAssign);
    }

    /// <summary>
    /// Forgets the regions plugins reported earlier, for when a source file changed on disk or a
    /// project loaded, so the next refresh asks again.
    /// </summary>
    public void ClearRegionCache()
    {
        _regionCache.Clear();
        _regionSetKey = null;
    }

    private void RefreshRegions(Texture2D? texture)
    {
        TextureRegionSet regions = TextureRegionSet.Empty;
        string? sourceFile = _currentExposedSource == null && texture != null ? GetSourceFile() : null;

        if (_pluginManager != null && texture != null && !string.IsNullOrEmpty(sourceFile))
        {
            FilePath file = new FilePath(sourceFile);
            string key = $"{file.FullPath}|{texture.Width}x{texture.Height}";
            if (key == _regionSetKey)
            {
                // Nothing the regions depend on changed; Refresh runs on every variable set.
                UpdateCurrentRegionHighlight();
                return;
            }

            if (!_regionCache.TryGetValue(file.FullPath, out IReadOnlyList<TextureRegion>? reported))
            {
                // PluginManager guards each plugin, so a buggy one cannot break the tab.
                try
                {
                    reported = _pluginManager.GetTextureRegions(file);
                }
                catch (Exception)
                {
                    // Whatever asked the plugins failed; the tab works without regions.
                    reported = new List<TextureRegion>();
                }
                _regionCache[file.FullPath] = reported;
            }

            regions = new TextureRegionSet(reported, texture.Width, texture.Height);
            _regionSetKey = key;
        }
        else
        {
            _regionSetKey = null;
        }

        _regionOverlayManager.Regions = regions;
        _regionSet = regions;
        UpdateCurrentRegionHighlight();
    }

    private void UpdateCurrentRegionHighlight()
    {
        var selector = _view.Canvas.RectangleSelector;
        _regionOverlayManager.Current = selector != null && selector.Visible
            ? (MathFunctions.RoundToInt(selector.Left), MathFunctions.RoundToInt(selector.Top),
               MathFunctions.RoundToInt(selector.Width), MathFunctions.RoundToInt(selector.Height))
            : null;
    }

    private string? GetSourceFile()
    {
        var state = _selectedState.SelectedStateSave;
        if (state == null)
        {
            return null;
        }

        string? instancePrefix = _selectedState.SelectedInstance?.Name;
        if (!string.IsNullOrEmpty(instancePrefix))
        {
            instancePrefix += ".";
        }

        return new RecursiveVariableFinder(state).GetValue<string>($"{instancePrefix}SourceFile");
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

    public void HandleRegionDoubleClicked(ImageRegionSelectionCore control) =>
        HandleRegionGesture(RegionGesture.DoubleClick, control);

    // Every pointer gesture that can set the texture rectangle ends up here. A double-click inside a
    // plugin region applies that region; a double-click anywhere else keeps making the default
    // rectangle. Which gesture applies regions is decided only by _regionApplyGesture.
    private void HandleRegionGesture(RegionGesture gesture, ImageRegionSelectionCore control)
    {
        if (_currentExposedSource != null) return;

        var doubleClickInstance = _selectedState.SelectedInstance;
        if (doubleClickInstance != null)
        {
            var doubleClickElement = ObjectFinder.Self.GetElementSave(doubleClickInstance);
            if (doubleClickElement != null && ObjectFinder.Self.IsVariableHiddenRecursively(doubleClickElement, "TextureAddress"))
            {
                return;
            }
        }

        float worldX = control.XnaCursor.GetWorldX(control.SystemManagers);
        float worldY = control.XnaCursor.GetWorldY(control.SystemManagers);
        TextureRegion? region = gesture == _regionApplyGesture ? _regionSet.HitTest(worldX, worldY) : null;
        if (region == null && gesture != RegionGesture.DoubleClick)
        {
            // The default rectangle stays a double-click gesture whatever applies regions.
            return;
        }

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

            var selectionRect = region is TextureRegion hit
                ? new Rectangle(hit.X, hit.Y, hit.Width, hit.Height)
                : GetOptimalSelectionRectangle((int)worldX, (int)worldY, graphicalUiElement.TextureWidth, graphicalUiElement.TextureHeight);

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
            UpdateCurrentRegionHighlight();

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
        if (state == null)
        {
            // Nothing to record against: a region can only be dragged over a selected element's texture.
            return;
        }

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
        var selector = (sender as ImageRegionSelectionCore)?.RectangleSelector;

        var graphicalUiElement = _selectedState.SelectedIpso as GraphicalUiElement;
        var state = _selectedState.SelectedStateSave;

        if (graphicalUiElement != null && selector != null && state != null)
        {
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

        _nineSliceGuideManager.Selector = _view.Canvas.RectangleSelector;
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
        var control = _view.Canvas;

        // With no texture or no element there is no region to edit. The previous selection's
        // selectors must go too, or dragging where they were writes to this selection (#5101).
        if (control.CurrentTexture == null || _selectedState.SelectedElement == null)
        {
            control.DesiredSelectorCount = 0;
            return;
        }

        // early out
        if (refreshType == RefreshType.OnlyIfGrabbed &&
            control.RectangleSelector != null &&
            control.RectangleSelector.SideGrabbed != TextureCoordinateSelectionPlugin.RegionSelection.ResizeSide.None)
        {
            return;
        }

        if (shouldRefreshAccordingToVariableSets == false)
        {
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
                    var selector = control.RectangleSelectors[0];

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

                    var selector = control.RectangleSelectors[0];


                    selector.Left = rfv.GetValue<int>($"{instancePrefix}TextureLeft");
                    selector.Width = rfv.GetValue<int>($"{instancePrefix}TextureWidth");

                    selector.Top = rfv.GetValue<int>($"{instancePrefix}TextureTop");
                    selector.Height = rfv.GetValue<int>($"{instancePrefix}TextureHeight");

                    selector.Visible = true;
                    selector.ShowHandles = true;
                    selector.ShowMoveCursorWhenOver = _view.Canvas.CanChangeX || _view.Canvas.CanChangeY;

                    this.CenterCameraOnSelection();

                }
                // The width comes from the selected visual's size, so there is nothing to show without one.
                else if (textureAddress == TextureAddress.DimensionsBased && graphicalUiElement != null)
                {
                    shouldClearOut = false;
                    control.DesiredSelectorCount = 1;
                    var selector = control.RectangleSelectors[0];

                    selector.Left = rfv.GetValue<int>($"{instancePrefix}TextureLeft");
                    selector.Top = rfv.GetValue<int>($"{instancePrefix}TextureTop");

                    var widthScale = rfv.GetValue<float>($"{instancePrefix}TextureWidthScale");
                    var heightScale = rfv.GetValue<float>($"{instancePrefix}TextureHeightScale");

                    var absoluteWidth = graphicalUiElement.AbsoluteWidth;
                    var absoluteHeight = graphicalUiElement.AbsoluteHeight;

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

    public void CenterCameraOnSelection()
    {
        var camera = SystemManagers.Renderer.Camera;
        _view.InvokeWhenLoaded(() =>
        {
            var selector = _view.Canvas.RectangleSelector;
            if(selector != null)
            {
                camera.X = selector.Left + selector.Width / 2.0f - camera.ClientWidth/(2 * camera.Zoom);
                camera.Y = selector.Top + selector.Height / 2.0f - camera.ClientHeight/(2 * camera.Zoom);
                UpdateScrollBarsToTexture();
            }
        });
    }

    internal void UpdateButtonSizes(double baseFontSize)
    {
        _view?.UpdateButtonSizes(baseFontSize);
    }

    internal void SetCheckerboardVisible(bool visible)
    {
        _backgroundManager.IsCheckerboardVisible = visible;
    }

    public void Dispose()
    {
        _backgroundManager?.Dispose();
    }
}
