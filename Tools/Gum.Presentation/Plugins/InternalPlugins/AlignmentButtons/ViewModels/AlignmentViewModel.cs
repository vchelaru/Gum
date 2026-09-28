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
using System.Drawing;
using System.Globalization;

namespace Gum.Plugins.InternalPlugins.AlignmentButtons.ViewModels;

public class AlignmentViewModel : ViewModel
{
    private readonly CommonControlLogic _commonControlLogic;
    private readonly ISelectedState _selectedState;
    private readonly IUndoManager _undoManager;
    private readonly IStateEditingIndicatorService _stateEditingIndicatorService;
    private readonly IOutputManager _outputManager;

    public bool HasStateInformation
    {
        get => Get<bool>();
        set => Set(value);
    }

    public string? StateInformation
    {
        get => Get<string?>();
        set => Set(value);
    }

    public Color StateBackground
    {
        get => Get<Color>();
        set => Set(value);
    }

    public float DockMargin
    {
        get => Get<float>();
        set => Set(value);
    }

    public string DockMarginText
    {
        get => Get<string>();
        set
        {
            if (Set(value))
            {
                DockMargin = float.TryParse(value, NumberStyles.Float, CultureInfo.CurrentCulture, out var parsed)
                    ? parsed
                    : 0f;
            }
        }
    }

    [DependsOn(nameof(DockMargin))]
    public string MarginText => $"Applies {DockMargin}px margin";

    [DependsOn(nameof(DockMargin))]
    public bool IsMarginTextVisible => DockMargin != 0;

    public AlignmentViewModel(CommonControlLogic commonControlLogic, ISelectedState selectedState,
        IUndoManager undoManager, IStateEditingIndicatorService stateEditingIndicatorService,
        IOutputManager outputManager)
    {
        _outputManager = outputManager;
        _commonControlLogic = commonControlLogic;
        _selectedState = selectedState;
        _undoManager = undoManager;
        _stateEditingIndicatorService = stateEditingIndicatorService;
        DockMarginText = "0";
    }

    public void RefreshStateLabel()
    {
        var info = _stateEditingIndicatorService.GetInfo();
        HasStateInformation = info.HasStateInformation;
        StateInformation = info.StateInformation;
        StateBackground = info.StateBackground;
    }

    // When DockMargin is 0, the expression `NormalizeNegativeZero(-DockMargin * 2)` evaluates to IEEE 754
    // negative zero, which serializes as "-0" in the project file. Route every
    // dock-size computation through this helper so future handlers can't reintroduce it.
    internal static float NormalizeNegativeZero(float value) => value == 0f ? 0f : value;

    /// <summary>
    /// Runs one button's writes as a single undo step. A locked instance is left alone, the same as
    /// a canvas drag or an arrow-key nudge leaves it: the writes skip it, and the Output tab names it.
    /// </summary>
    private void ApplyToSelection(Action setValues)
    {
        List<InstanceSave> selectedInstances = _selectedState.SelectedInstances.ToList();
        List<InstanceSave> lockedInstances = selectedInstances.Where(instance => instance.Locked).ToList();

        if (lockedInstances.Count > 0)
        {
            _outputManager.AddOutput(
                $"The Alignment tab left locked instances unchanged: {string.Join(", ", lockedInstances.Select(instance => instance.Name))}");

            if (lockedInstances.Count == selectedInstances.Count)
            {
                return;
            }
        }

        using (_undoManager.RequestLock())
        {
            setValues();
            _commonControlLogic.RefreshAndSave();
        }
    }

    #region Anchor Actions

    public void TopLeftButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
            PositionUnitType.PixelsFromLeft, DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop, DockMargin);
        });
    }

    public void TopButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
            PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop, DockMargin);
        });
    }

    public void TopRightButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
            PositionUnitType.PixelsFromRight, -DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Top,
                PositionUnitType.PixelsFromTop, DockMargin);
        });
    }

    public void MiddleLeftButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft, DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);
        });
    }

    public void MiddleMiddleButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
            PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);
        });
    }

    public void MiddleRightButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight, -DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);
        });
    }

    public void BottomLeftButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Left,
                PositionUnitType.PixelsFromLeft, DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom, -DockMargin);
        });
    }

    public void BottomMiddleButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
                PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom, -DockMargin);
        });
    }

    public void BottomRightButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
                global::RenderingLibrary.Graphics.HorizontalAlignment.Right,
                PositionUnitType.PixelsFromRight, -DockMargin);

            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Bottom,
                PositionUnitType.PixelsFromBottom, -DockMargin);
        });
    }

    public void AnchorCenterHorizontally_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(
            global::RenderingLibrary.Graphics.HorizontalAlignment.Center,
            PositionUnitType.PixelsFromCenterX);
        });
    }

    public void AnchorCenterVertically_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetYValues(
                global::RenderingLibrary.Graphics.VerticalAlignment.Center,
                PositionUnitType.PixelsFromCenterY);
        });
    }

    #endregion

    #region Dock Actions

    public void DockTopButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Top, PositionUnitType.PixelsFromTop, DockMargin);

            _commonControlLogic.SetAndCallReact("Width", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    public void SizeToChildren_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetAndCallReact("Width", DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToChildren, typeof(DimensionUnitType).Name);

            _commonControlLogic.SetAndCallReact("Height", DockMargin * 2, "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToChildren, typeof(DimensionUnitType).Name);
        });
    }

    public void DockLeftButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Left, PositionUnitType.PixelsFromLeft, DockMargin);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Height", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    public void DockFillButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Width", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);

            _commonControlLogic.SetAndCallReact("Height", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    public void DockRightButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Right, PositionUnitType.PixelsFromRight, -DockMargin);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Height", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    public void DockBottomButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Bottom, PositionUnitType.PixelsFromBottom, -DockMargin);

            _commonControlLogic.SetAndCallReact("Width", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    public void DockFillVerticallyButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetYValues(global::RenderingLibrary.Graphics.VerticalAlignment.Center, PositionUnitType.PixelsFromCenterY);

            _commonControlLogic.SetAndCallReact("Height", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("HeightUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    public void DockFillHorizontallyButton_Click()
    {
        ApplyToSelection(() =>
        {
            _commonControlLogic.SetXValues(global::RenderingLibrary.Graphics.HorizontalAlignment.Center, PositionUnitType.PixelsFromCenterX);

            _commonControlLogic.SetAndCallReact("Width", NormalizeNegativeZero(-DockMargin * 2), "float");
            _commonControlLogic.SetAndCallReact("WidthUnits", DimensionUnitType.RelativeToParent, typeof(DimensionUnitType).Name);
        });
    }

    #endregion
}
