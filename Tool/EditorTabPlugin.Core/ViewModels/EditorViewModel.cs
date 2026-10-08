using CommunityToolkit.Mvvm.Input;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Services;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using Gum.Services;
using Gum.Wireframe;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;

namespace EditorTabPlugin_XNA.ViewModels;

/// <summary>
/// Implements <see cref="IZoomController"/> so <c>CameraController</c> (headless, Gum.Presentation)
/// can drive zoom without depending on this concrete, still-WPF/WinForms-side ViewModel.
/// </summary>
public partial class EditorViewModel : ViewModel, IZoomController
{
    private readonly IPluginManager _pluginManager;
    private readonly IFileCommands _fileCommands;
    private readonly IWireframeObjectManager _wireframeObjectManager;
    private readonly IGridSnapWarningService _gridSnapWarningService;
    private readonly IProjectManager _projectManager;
    private readonly IPreviewLauncher _previewLauncher;
    private bool _isSyncingPreviewPinned;

    public bool HasGridSnapWarning
    {
        get => Get<bool>();
        set => Set(value);
    }

    public string? GridSnapWarningText
    {
        get => Get<string?>();
        set => Set(value);
    }

    /// <summary>
    /// Recomputes <see cref="HasGridSnapWarning"/>/<see cref="GridSnapWarningText"/>. Call after
    /// selection changes, a variable is set, or Snap to Grid is toggled.
    /// </summary>
    public void RefreshGridSnapWarning()
    {
        var info = _gridSnapWarningService.GetInfo();
        HasGridSnapWarning = info.HasWarning;
        GridSnapWarningText = info.WarningText;
    }

    public bool SnapToGrid
    {
        get => Get<bool>();
        set
        {
            if (Set(value))
            {
                ApplyGridSettingToProject(nameof(GumProjectSave.SnapToGrid), gumProject => gumProject.SnapToGrid = value);
            }
        }
    }

    public int GridSize
    {
        get => Get<int>();
        set
        {
            if (Set(value))
            {
                ApplyGridSettingToProject(nameof(GumProjectSave.GridSize), gumProject => gumProject.GridSize = value);
            }
        }
    }

    private void ApplyGridSettingToProject(string propertyName, Action<GumProjectSave> applyToProject)
    {
        var project = _projectManager.GumProjectSave;
        if (project == null)
        {
            return;
        }

        applyToProject(project);
        _pluginManager.ProjectPropertySet(propertyName);
        _fileCommands.TryAutoSaveProject();
    }

    SystemManagers? SystemManagers
    {
        get; set;
    }

    Ruler? LeftRuler 
    {
        get; 
        set;
    }
    Ruler? TopRuler { get; set; }

    // Preset font scale steps for + / - buttons
    static readonly float[] FontScaleSteps = new float[]
    {
        0.5f, 0.75f, 1.0f, 1.25f, 1.5f, 1.75f, 2.0f, 2.5f, 3.0f, 4.0f
    };

    public float GlobalFontScale
    {
        get => Get<float>();
        set
        {
            if (Set(value))
            {
                GraphicalUiElement.GlobalFontScale = value;
                _wireframeObjectManager.RefreshAll(forceLayout: true);
            }
        }
    }

    [DependsOn(nameof(GlobalFontScale))]
    public string GlobalFontScaleDisplay => $"{GlobalFontScale:0.##}x";

    [RelayCommand]
    public void FontScaleIncrease()
    {
        var index = GetFontScaleIndex();
        if (index < FontScaleSteps.Length - 1)
        {
            GlobalFontScale = FontScaleSteps[index + 1];
        }
    }

    [RelayCommand]
    public void FontScaleDecrease()
    {
        var index = GetFontScaleIndex();
        if (index > 0)
        {
            GlobalFontScale = FontScaleSteps[index - 1];
        }
    }

    private int GetFontScaleIndex()
    {
        var current = GlobalFontScale;
        for (int i = FontScaleSteps.Length - 1; i >= 0; i--)
        {
            if (FontScaleSteps[i] <= current)
            {
                return i;
            }
        }
        return 0;
    }

    public ZoomLevel[] ZoomLevels { get; init; } = new ZoomLevel[]
    {
        new ZoomLevel{Value = 1600 },
        new ZoomLevel{Value = 1200 },
        new ZoomLevel{Value = 1000 },
        new ZoomLevel{Value = 800 },
        new ZoomLevel{Value = 700 },
        new ZoomLevel{Value = 600 },
        new ZoomLevel{Value = 500 },
        new ZoomLevel{Value = 400 },
        new ZoomLevel{Value = 350 },
        new ZoomLevel{Value = 300 },
        new ZoomLevel{Value = 250 },
        new ZoomLevel{Value = 200 },
        new ZoomLevel{Value = 175 },
        new ZoomLevel{Value = 150 },
        new ZoomLevel{Value = 125 },
        new ZoomLevel{Value = 100 },
        new ZoomLevel{Value = 87 },
        new ZoomLevel{Value = 75 },
        new ZoomLevel{Value = 63 },
        new ZoomLevel{Value = 50 },
        new ZoomLevel{Value = 33 },
        new ZoomLevel{Value = 25 },
        new ZoomLevel{Value = 10 },
        new ZoomLevel{Value = 5 }
    };

    public CustomCanvasSize[] CustomCanvasSizes 
    {
        get => Get<CustomCanvasSize[]>();
        set => Set(value);
    }

    public CustomCanvasSize SelectedCustomCanvasSize
    {
        get => Get<CustomCanvasSize>();
        set
        {
            // A combo box pushes null when its items are replaced and its selection is not in the new
            // list. That is not a choice, so keep the current size: substituting one here raises the
            // change while the combo is still applying its new items, so it misses it and stays blank
            // when the load then selects that same size (#5374).
            if (value == null)
            {
                return;
            }
            if(Set(value))
            {
                RefreshCanvasSize();

                // We need to tell the view to refresh:
                _wireframeObjectManager.RefreshAll(forceLayout: true);
            }
        }
    }

    public void RefreshCanvasSize()
    {
        int? width = null;
        int? height = null;
        var customSize = SelectedCustomCanvasSize;

        if (customSize.Width == null || customSize.Height == null)
        {
            if (ObjectFinder.Self.GumProjectSave != null)
            {
                width = ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;
                height = ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight;
            }
        }
        else
        {
            width = customSize.Width;
            height = customSize.Height;
        }

        if (width != null && height != null)
        {
            GraphicalUiElement.CanvasWidth = width.Value;
            GraphicalUiElement.CanvasHeight = height.Value;


        }
    }

    [DependsOn(nameof(PercentZoomLevel))]
    public int CurrentZoomIndex
    {
        get
        {
            var percentZoom = PercentZoomLevel.Value;
            return Array.FindIndex(ZoomLevels, z => z.Value == percentZoom);
        }
        set
        {
            PercentZoomLevel = ZoomLevels[value];
        }
    }

    public ZoomLevel PercentZoomLevel
    {
        get => Get<ZoomLevel>();
        set
        {
            if (Set(value))
            {
                SetZoomOnCamera();
            }
        }
    }

    public int PercentZoom
    {
        set
        {
            var percentZoom = ZoomLevels.FirstOrDefault(z => z.Value == value);
            if(percentZoom != null)
            {
                PercentZoomLevel = percentZoom;
            }
        }
    }

    public EditorViewModel(IPluginManager pluginManager,
        IFileCommands fileCommands,
        IWireframeObjectManager wireframeObjectManager,
        IGridSnapWarningService gridSnapWarningService,
        IProjectManager projectManager,
        IPreviewLauncher previewLauncher)
    {
        _pluginManager = pluginManager;
        _fileCommands = fileCommands;
        _wireframeObjectManager = wireframeObjectManager;
        _gridSnapWarningService = gridSnapWarningService;
        _projectManager = projectManager;
        _previewLauncher = previewLauncher;
        _previewLauncher.PinnedChanged += SyncPreviewPinned;
        PercentZoomLevel = ZoomLevels.First(item => item.Value == 100);

        CustomCanvasSizes = ProjectLoadFills.DefaultCanvasSizes.ToArray();

        SetWithoutNotifying(CustomCanvasSizes[0], nameof(SelectedCustomCanvasSize));
        SetWithoutNotifying(GraphicalUiElement.GlobalFontScale, nameof(GlobalFontScale));
    }

    public void InitializeXnaView(SystemManagers systemManagers, Ruler topRuler, Ruler leftRuler)
    {
        if(topRuler == null)
        {
            throw new ArgumentNullException("topRuler");
        }
        SystemManagers = systemManagers;

        LeftRuler = leftRuler;
        TopRuler = topRuler;

        SetZoomOnCamera();
    }

    private void SetZoomOnCamera()
    {
        var zoomRatio = PercentZoomLevel.Value / 100.0f;
        if (SystemManagers != null)
        {
            SystemManagers.Renderer.Camera.Zoom = zoomRatio;
            LeftRuler!.ZoomValue = zoomRatio;
            TopRuler!.ZoomValue = zoomRatio;
        }
    }

    [RelayCommand]
    public void ZoomOut()
    {
        int index = CurrentZoomIndex;

        if (index < ZoomLevels.Length - 1)
        {
            index++;
            CurrentZoomIndex = index;
        }
    }

    [RelayCommand]
    public void ZoomIn()
    {
        int index = CurrentZoomIndex;

        if (index > 0)
        {
            index--;
            CurrentZoomIndex = index;
        }
    }

    /// <summary>Whether a screen or component is currently selected, so Preview has something to show.</summary>
    public bool HasSelectedElement
    {
        get => Get<bool>();
        private set
        {
            if (Set(value))
            {
                PreviewCommand.NotifyCanExecuteChanged();
            }
        }
    }

    /// <summary>Call whenever the tool's selected element changes, so Preview enables/disables accordingly.</summary>
    public void UpdateHasSelectedElement(ElementSave? element) => HasSelectedElement = element != null;

    private bool CanPreview() => HasSelectedElement;

    /// <summary>
    /// Whether the running preview is pinned to the element it shows instead of following the tool's
    /// selection (issue #3078). Turning it on with no preview running turns it back off.
    /// </summary>
    public bool IsPreviewPinned
    {
        get => Get<bool>();
        set
        {
            if (!Set(value) || _isSyncingPreviewPinned)
            {
                return;
            }
            if (value)
            {
                _previewLauncher.Pin();
            }
            else
            {
                _previewLauncher.Unpin();
            }
            SyncPreviewPinned();
        }
    }

    private void SyncPreviewPinned()
    {
        _isSyncingPreviewPinned = true;
        IsPreviewPinned = _previewLauncher.PinnedElement != null;
        _isSyncingPreviewPinned = false;
    }

    [RelayCommand(CanExecute = nameof(CanPreview))]
    public void Preview() => _previewLauncher.Launch();

    internal void HandleProjectLoad(GumProjectSave save)
    {
        RefreshCanvasSize();

        // The project manager fills a loaded project's missing sizes; a project handed in without
        // them still shows the defaults.
        this.CustomCanvasSizes = save.CustomCanvasSizes is { Count: > 0 }
            ? save.CustomCanvasSizes.ToArray()
            : ProjectLoadFills.DefaultCanvasSizes.ToArray();

        this.SelectedCustomCanvasSize = this.CustomCanvasSizes[0];

        // Set without the setters so the values are not written back into the project, then notify
        // so a toolbar bound before the load (the Avalonia head builds its tab at startup) updates.
        SetWithoutNotifying(save.SnapToGrid, nameof(SnapToGrid));
        SetWithoutNotifying(save.GridSize, nameof(GridSize));
        NotifyPropertyChanged(nameof(SnapToGrid));
        NotifyPropertyChanged(nameof(GridSize));
    }
}


public class ZoomLevel
{
    public int Value { get; set; }
    public string ZoomDisplay => $"{Value}%";
}