using System;
using System.Linq;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using WpfDataUi.Controls;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// The options of the Variables tab's toggle-button editors (units, origins, text alignment and
/// overflow, children layout): each option's name, value, and icon. Width units, height units, and
/// Y origin drop the values the selected object's standard element excludes. Each head turns these
/// into its own buttons; the same option instances are returned every time so a head can cache.
/// </summary>
public class VariableGridToggleOptions
{
    private readonly ISelectedState _selectedState;

    /// <summary>Creates the option sets.</summary>
    public VariableGridToggleOptions(ISelectedState selectedState)
    {
        _selectedState = selectedState;

        XUnits = new[]
        {
            new ToggleButtonOption("Pixels From Left", PositionUnitType.PixelsFromLeft) { GumIconName = "XUnitsLeft" },
            new ToggleButtonOption("Pixels From Center", PositionUnitType.PixelsFromCenterX) { GumIconName = "XUnitsCenter" },
            new ToggleButtonOption("Pixels From Right", PositionUnitType.PixelsFromRight) { GumIconName = "XUnitsRight" },
            new ToggleButtonOption("Percentage Parent Width", PositionUnitType.PercentageWidth) { GumIconName = "XUnitsPercentageParent" },
        };
        YUnits = new[]
        {
            new ToggleButtonOption("Pixels From Top", PositionUnitType.PixelsFromTop) { GumIconName = "YUnitsTop" },
            new ToggleButtonOption("Pixels From Center", PositionUnitType.PixelsFromCenterY) { GumIconName = "YUnitsCenter" },
            new ToggleButtonOption("Pixels From Bottom", PositionUnitType.PixelsFromBottom) { GumIconName = "YUnitsBottom" },
            new ToggleButtonOption("Percentage Parent Height", PositionUnitType.PercentageHeight) { GumIconName = "YUnitsPercentageParent" },
            new ToggleButtonOption("Pixels From Baseline", PositionUnitType.PixelsFromBaseline) { GumIconName = "YUnitsBaseline" },
        };
        XOrigin = new[]
        {
            new ToggleButtonOption("Left", HorizontalAlignment.Left) { GumIconName = "XOriginStart" },
            new ToggleButtonOption("Center", HorizontalAlignment.Center) { GumIconName = "XOriginCenter" },
            new ToggleButtonOption("Right", HorizontalAlignment.Right) { GumIconName = "XOriginEnd" },
        };
        AllYOrigins = new[]
        {
            new ToggleButtonOption("Top", VerticalAlignment.Top) { GumIconName = "YOriginStart" },
            new ToggleButtonOption("Center", VerticalAlignment.Center) { GumIconName = "YOriginCenter" },
            new ToggleButtonOption("Bottom", VerticalAlignment.Bottom) { GumIconName = "YOriginEnd" },
            new ToggleButtonOption("Baseline", VerticalAlignment.TextBaseline) { GumIconName = "YOriginBaseline" },
        };
        AllWidthUnits = new[]
        {
            new ToggleButtonOption("Absolute", DimensionUnitType.Absolute) { GumIconName = "WidthUnitsAbsolute" },
            new ToggleButtonOption("Relative to Parent", DimensionUnitType.RelativeToParent) { GumIconName = "WidthUnitsRelativeToParent" },
            new ToggleButtonOption("Percentage of Parent", DimensionUnitType.PercentageOfParent) { GumIconName = "WidthUnitsPercentageOfParent" },
            new ToggleButtonOption("Ratio of Parent", DimensionUnitType.Ratio) { GumIconName = "WidthUnitsRatioOfParent" },
            new ToggleButtonOption("Relative to Children", DimensionUnitType.RelativeToChildren) { GumIconName = "WidthUnitsRelativeToChildren" },
            new ToggleButtonOption("Percentage of Height", DimensionUnitType.PercentageOfOtherDimension) { GumIconName = "WidthUnitsPercentageOfHeight" },
            new ToggleButtonOption("Percentage of File Width", DimensionUnitType.PercentageOfSourceFile) { GumIconName = "WidthUnitsPercentageOfFileWidth" },
            new ToggleButtonOption("Maintain File Aspect Ratio Width", DimensionUnitType.MaintainFileAspectRatio) { GumIconName = "WidthUnitsMaintainFileAspectRatio" },
            new ToggleButtonOption("Absolute Multiplied by Font Scale", DimensionUnitType.AbsoluteMultipliedByFontScale) { GumIconName = "WidthUnitsAbsoluteMultipliedByFontScale" },
            new ToggleButtonOption("Relative to Max of Children or Parent", DimensionUnitType.RelativeToMaxParentOrChildren) { GumIconName = "WidthRelativeToMaxChildrenOrParent" },
        };
        AllHeightUnits = new[]
        {
            new ToggleButtonOption("Absolute", DimensionUnitType.Absolute) { GumIconName = "HeightUnitsAbsolute" },
            new ToggleButtonOption("Relative to Parent", DimensionUnitType.RelativeToParent) { GumIconName = "HeightUnitsRelativeToParent" },
            new ToggleButtonOption("Percentage of Parent", DimensionUnitType.PercentageOfParent) { GumIconName = "HeightUnitsPercentageOfParent" },
            new ToggleButtonOption("Ratio of Parent", DimensionUnitType.Ratio) { GumIconName = "HeightUnitsRatioOfParent" },
            new ToggleButtonOption("Relative to Children", DimensionUnitType.RelativeToChildren) { GumIconName = "HeightUnitsRelativeToChildren" },
            new ToggleButtonOption("Percentage of Width", DimensionUnitType.PercentageOfOtherDimension) { GumIconName = "HeightUnitsPercentageOfWidth" },
            new ToggleButtonOption("Percentage of File Height", DimensionUnitType.PercentageOfSourceFile) { GumIconName = "HeightUnitsPercentageOfFileHeight" },
            new ToggleButtonOption("Maintain File Aspect Ratio Height", DimensionUnitType.MaintainFileAspectRatio) { GumIconName = "HeightUnitsMaintainFileAspectRatio" },
            new ToggleButtonOption("Absolute Multiplied by Font Scale", DimensionUnitType.AbsoluteMultipliedByFontScale) { GumIconName = "HeightUnitsAbsoluteMultipliedByFontScale" },
            new ToggleButtonOption("Relative to Max of Children or Parent", DimensionUnitType.RelativeToMaxParentOrChildren) { GumIconName = "HeightRelativeToMaxChildrenOrParent" },
        };
        TextHorizontalAlignment = new[]
        {
            new ToggleButtonOption("Left", HorizontalAlignment.Left) { IconName = "TextAlignLeft", ImagePath = "Content/Icons/Alignment/LeftAlign.png" },
            new ToggleButtonOption("Center", HorizontalAlignment.Center) { IconName = "TextAlignCenter", ImagePath = "Content/Icons/Alignment/CenterAlign.png" },
            new ToggleButtonOption("Right", HorizontalAlignment.Right) { IconName = "TextAlignRight", ImagePath = "Content/Icons/Alignment/RightAlign.png" },
        };
        TextVerticalAlignment = new[]
        {
            new ToggleButtonOption("Top", VerticalAlignment.Top) { IconName = "TextboxAlignTop", ImagePath = "Content/Icons/Alignment/TopAlign.png" },
            new ToggleButtonOption("Center", VerticalAlignment.Center) { IconName = "TextboxAlignMiddle", ImagePath = "Content/Icons/Alignment/VerticalCenterAlign.png" },
            new ToggleButtonOption("Bottom", VerticalAlignment.Bottom) { IconName = "TextboxAlignBottom", ImagePath = "Content/Icons/Alignment/BottomAlign.png" },
        };
        ChildrenLayout = new[]
        {
            new ToggleButtonOption("Regular", Managers.ChildrenLayout.Regular) { GumIconName = "ChildrenLayoutRegular" },
            new ToggleButtonOption("Top to Bottom Stack", Managers.ChildrenLayout.TopToBottomStack) { GumIconName = "ChildrenLayoutTopToBottomStack" },
            new ToggleButtonOption("Left to Right Stack", Managers.ChildrenLayout.LeftToRightStack) { GumIconName = "ChildrenLayoutLeftToRightStack" },
            new ToggleButtonOption("Auto Grid Horizontal", Managers.ChildrenLayout.AutoGridHorizontal) { GumIconName = "ChildrenLayoutAutoGridHorizontal" },
            new ToggleButtonOption("Auto Grid Vertical", Managers.ChildrenLayout.AutoGridVertical) { GumIconName = "ChildrenLayoutAutoGridVertical" },
        };
        TextOverflowHorizontalMode = new[]
        {
            new ToggleButtonOption("Truncate Word", global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.TruncateWord) { GumIconName = "TextOverflowHorizontalTruncateWord" },
            new ToggleButtonOption("Ellipsis Letter", global::RenderingLibrary.Graphics.TextOverflowHorizontalMode.EllipsisLetter) { GumIconName = "TextOverflowHorizontalEllipsisLetter" },
        };
        TextOverflowVerticalMode = new[]
        {
            new ToggleButtonOption("Spill", global::RenderingLibrary.Graphics.TextOverflowVerticalMode.SpillOver) { GumIconName = "TextOverflowVerticalSpill" },
            new ToggleButtonOption("Truncate Line", global::RenderingLibrary.Graphics.TextOverflowVerticalMode.TruncateLine) { GumIconName = "TextOverflowVerticalTruncateLine" },
        };
    }

    public ToggleButtonOption[] XUnits { get; }
    public ToggleButtonOption[] YUnits { get; }
    public ToggleButtonOption[] XOrigin { get; }
    public ToggleButtonOption[] TextHorizontalAlignment { get; }
    public ToggleButtonOption[] TextVerticalAlignment { get; }
    public ToggleButtonOption[] ChildrenLayout { get; }
    public ToggleButtonOption[] TextOverflowHorizontalMode { get; }
    public ToggleButtonOption[] TextOverflowVerticalMode { get; }

    /// <summary>Every Y origin, before exclusions.</summary>
    public ToggleButtonOption[] AllYOrigins { get; }

    /// <summary>Every width unit, before exclusions.</summary>
    public ToggleButtonOption[] AllWidthUnits { get; }

    /// <summary>Every height unit, before exclusions.</summary>
    public ToggleButtonOption[] AllHeightUnits { get; }

    /// <summary>The width units the selected object's standard element allows.</summary>
    public ToggleButtonOption[] GetWidthUnits() => Exclude(AllWidthUnits, "WidthUnits", GetSelectedRootElementName());

    /// <summary>The height units the selected object's standard element allows.</summary>
    public ToggleButtonOption[] GetHeightUnits() => Exclude(AllHeightUnits, "HeightUnits", GetSelectedRootElementName());

    /// <summary>The Y origins the selected object's standard element allows.</summary>
    public ToggleButtonOption[] GetYOrigins() => Exclude(AllYOrigins, "YOrigin", GetSelectedRootElementName());

    /// <summary>
    /// <paramref name="options"/> minus the values the default state of standard element
    /// <paramref name="rootElementName"/> lists in <paramref name="variableName"/>'s excluded enum values.
    /// </summary>
    public ToggleButtonOption[] Exclude(ToggleButtonOption[] options, string variableName, string? rootElementName)
    {
        StateSave? state = StandardElementsManager.Self.GetDefaultStateFor(rootElementName);
        VariableSave? variable = state?.Variables.FirstOrDefault(item => item.Name == variableName);

        if (variable?.ExcludedValuesForEnum?.Any() != true)
        {
            return options;
        }

        return options
            .Where(option => !variable.ExcludedValuesForEnum.Any(excluded => Convert.ToInt32(excluded) == Convert.ToInt32(option.Value)))
            .ToArray();
    }

    /// <summary>The standard element the selected instance or element ultimately derives from.</summary>
    public string? GetSelectedRootElementName()
    {
        if (_selectedState.SelectedInstance != null)
        {
            return ObjectFinder.Self.GetRootStandardElementSave(_selectedState.SelectedInstance)?.Name;
        }
        if (_selectedState.SelectedElement != null)
        {
            return ObjectFinder.Self.GetRootStandardElementSave(_selectedState.SelectedElement)?.Name;
        }
        return null;
    }
}

/// <summary>
/// The value logic of the corner-radius composite editor: formatting and parsing its fields and
/// composing the uniform radius and four optional per-corner overrides from them.
/// </summary>
public class CornerRadiusDisplayLogic
{
    /// <summary>A radius as field text.</summary>
    public string FormatFloat(float value) => value.ToString("0.####", System.Globalization.CultureInfo.InvariantCulture);

    /// <summary>An optional corner radius as field text; empty when it inherits the uniform value.</summary>
    public string FormatNullableFloat(float? value) => value == null ? string.Empty : FormatFloat(value.Value);

    /// <summary>Field text as a radius, or null when it is not a number.</summary>
    public float? ParseFloat(string? text) =>
        float.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : null;

    /// <summary>
    /// The composite for the fields: linked uses only the uniform radius; unlinked adds each corner
    /// (empty corners inherit). An unparseable uniform field keeps <paramref name="current"/>'s.
    /// </summary>
    public CornerRadiusComposite Compose(bool isLinked, string? uniformText, string? topLeftText, string? topRightText,
        string? bottomLeftText, string? bottomRightText, CornerRadiusComposite current)
    {
        float uniform = ParseFloat(uniformText) ?? current.Uniform;

        return isLinked
            ? new CornerRadiusComposite(uniform, null, null, null, null)
            : new CornerRadiusComposite(uniform, ParseFloat(topLeftText), ParseFloat(topRightText),
                ParseFloat(bottomLeftText), ParseFloat(bottomRightText));
    }
}
