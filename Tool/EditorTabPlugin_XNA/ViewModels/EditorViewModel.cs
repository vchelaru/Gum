using CommunityToolkit.Mvvm.Input;
using Gum.Mvvm;
using Gum.Plugins.InternalPlugins.EditorTab.Views;
using RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EditorTabPlugin_XNA.ViewModels;

public partial class EditorViewModel : ViewModel
{
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

    public EditorViewModel()
    {
        PercentZoomLevel = ZoomLevels.First(item => item.Value == 100);
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
}


public class ZoomLevel
{
    public int Value { get; set; }
    public string ZoomDisplay => $"{Value}%";
}