using CommunityToolkit.Mvvm.Input;
using Gum.Commands;
using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
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

public partial class EditorViewModel : ViewModel
{
    private readonly PluginManager _pluginManager;
    private readonly IFileCommands _fileCommands;
    private readonly IWireframeObjectManager _wireframeObjectManager;

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

    static readonly CustomCanvasSize[] DefaultCanvasSizes = new CustomCanvasSize[]
    {
        new CustomCanvasSize{ Width = null, Height=null,   FriendlyName="Project Default" },
        new CustomCanvasSize{ Width = 640, Height=480,   FriendlyName="480p" },
        new CustomCanvasSize{ Width = 1280, Height=720,   FriendlyName="720p" },
        new CustomCanvasSize{ Width = 1280, Height=800,   FriendlyName="Steam Deck" },
        new CustomCanvasSize{ Width = 1920, Height=1080,   FriendlyName="1080p" },
        new CustomCanvasSize{ Width = 3840, Height=2160,   FriendlyName="4k" },
    };


    public CustomCanvasSize SelectedCustomCanvasSize
    {
        get => Get<CustomCanvasSize>();
        set
        {
            if(Set(value ?? DefaultCanvasSizes[0]))
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

    public EditorViewModel(PluginManager pluginManager, 
        IFileCommands fileCommands,
        IWireframeObjectManager wireframeObjectManager)
    {
        _pluginManager = pluginManager;
        _fileCommands = fileCommands;
        _wireframeObjectManager = wireframeObjectManager;
        PercentZoomLevel = ZoomLevels.First(item => item.Value == 100);

        CustomCanvasSizes = DefaultCanvasSizes;

        SetWithoutNotifying(CustomCanvasSizes[0], nameof(SelectedCustomCanvasSize));
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

    internal void HandleProjectLoad(GumProjectSave save)
    {
        RefreshCanvasSize();

        if(save.CustomCanvasSizes == null || save.CustomCanvasSizes.Count == 0)
        {
            save.CustomCanvasSizes = DefaultCanvasSizes.ToList();

            if(!string.IsNullOrEmpty(save.FullFileName))
            {
                _fileCommands.TryAutoSaveProject();
            }
        }

        this.CustomCanvasSizes = save.CustomCanvasSizes.ToArray();

        this.SelectedCustomCanvasSize = this.CustomCanvasSizes[0];
    }
}


public class ZoomLevel
{
    public int Value { get; set; }
    public string ZoomDisplay => $"{Value}%";
}