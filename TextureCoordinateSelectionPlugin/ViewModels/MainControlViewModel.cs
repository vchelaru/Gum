using Gum.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TextureCoordinateSelectionPlugin.ViewModels;

public class MainControlViewModel : ViewModel
{
    public bool IsSnapToGridChecked 
    {
        get => Get<bool>();
        set => Set(value);
    }

    public int SelectedSnapToGridValue
    {
        get => Get<int>();
        set => Set(value);
    }

    public List<int> AvailableSnapToGridValues
    {
        get; private set;
    } = new List<int>
    {
        4, 8, 12, 16, 24, 32, 48, 64
    };

    public IList<int> AvailableZoomLevels { get; set;} = new int[]
    {
        3200, 1600, 800, 400, 200, 100, 50, 25, 12
    };

    public int SelectedZoomLevel
    {
        get => Get<int>();
        set => Set(value);
    }

    [DependsOn(nameof(IsSnapToGridChecked))]
    public bool IsSnapToGridComboBoxEnabled => IsSnapToGridChecked;

    public MainControlViewModel()
    {
        SelectedSnapToGridValue = 16;
        SelectedZoomLevel = 100;
    }

    internal void ZoomIn()
    {
        var index = AvailableZoomLevels.IndexOf(SelectedZoomLevel);
        if(index > 0)
        {
            SelectedZoomLevel = AvailableZoomLevels[index - 1];
        }
    }

    internal void ZoomOut()
    {
        var index = AvailableZoomLevels.IndexOf(SelectedZoomLevel);
        if(index < AvailableZoomLevels.Count - 1)
        {
            SelectedZoomLevel = AvailableZoomLevels[index + 1];
        }
    }
}
