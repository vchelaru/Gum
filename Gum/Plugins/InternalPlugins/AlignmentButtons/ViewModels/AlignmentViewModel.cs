using Gum.DataTypes;
using Gum.Managers;
using Gum.Mvvm;
using Gum.Plugins.AlignmentButtons;
using Gum.Services;
using Gum.ToolStates;
using Gum.Undo;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;

public class AlignmentViewModel : ViewModel
{
    private readonly CommonControlLogic _commonControlLogic;
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;

    public float DockMargin
    {
        get => Get<float>();
        set => Set(value);
    }

    public AlignmentViewModel(CommonControlLogic commonControlLogic)
    {
        _commonControlLogic = commonControlLogic;
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _undoManager = Locator.GetRequiredService<IUndoManager>();
    }

    #region Anchor Actions

    public void TopLeftButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
            PositionUnitType.PixelsFromLeft, DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop, DockMargin);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void TopButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
            PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop, DockMargin);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void TopRightButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
            PositionUnitType.PixelsFromRight, -DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop, DockMargin);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void MiddleLeftButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft, DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void MiddleMiddleButton_Click()
    {
        using (_undoManager.RequestLock())
        {
            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
            PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void MiddleRightButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight, -DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void BottomLeftButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft, DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom, -DockMargin);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void BottomMiddleButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom, -DockMargin);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void BottomRightButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight, -DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom, -DockMargin);

            _commonControlLogic.RefreshAndSave();
        }
    }

    #endregion

    #region Dock Actions

    public void DockTopButton_Click()
    {
        using (_undoManager.RequestLock())
        {
            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop, DockMargin);

            _commonControlLogic.SetAndCallReact("Width", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);


            _commonControlLogic.RefreshAndSave();
        }
    }

    public void SizeToChildren_Click()
    {
        using (_undoManager.RequestLock())
        {
            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetAndCallReact("Width", DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToChildren, typeof(DimensionUnitType).Name);

            _commonControlLogic.SetAndCallReact("Height", DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToChildren, typeof(DimensionUnitType).Name);
        }
    }

    public void DockLeftButton_Click()
    {
        using (_undoManager.RequestLock())
        {
            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft, DockMargin);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Height", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void DockFillButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Width", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.SetAndCallReact("Height", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void DockRightButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight, -DockMargin);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Height", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void DockBottomButton_Click()
    {
        using (_undoManager.RequestLock())
        {
            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom, -DockMargin);

            _commonControlLogic.SetAndCallReact("Width", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void DockFillVerticallyButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Height", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.RefreshAndSave();
        }
    }

    public void DockFillHorizontallyButton_Click()
    {
        using (_undoManager.RequestLock())
        {

            var state = _selectedState.SelectedStateSave;

            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetAndCallReact("Width", -DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.RefreshAndSave();
        }
    }

    #endregion
}
