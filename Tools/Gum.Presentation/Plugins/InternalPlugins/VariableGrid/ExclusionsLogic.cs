using Gum.Commands;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Decision logic for hiding variables in the variable grid for common (non-plugin) standard
/// element types, including gradient / dropshadow / stroke channel hiding for shape elements
/// (Circle / Rectangle / ColoredCircle / RoundedRectangle / Arc / Line). Extracted from
/// <c>ExclusionsPlugin</c> (which stays a thin MEF wrapper subscribing to the
/// VariableExcluded/VariableSet plugin events) so this logic can be constructed and tested
/// without plugin composition.
/// </summary>
public class ExclusionsLogic
{
    private readonly ISelectedState _selectedState;
    private readonly IGuiCommands _guiCommands;
    private readonly ObjectFinder _objectFinder;
    private readonly ShapeVariableExclusionLogic _shapeVariableExclusionLogic = new();

    public ExclusionsLogic(ISelectedState selectedState, IGuiCommands guiCommands)
    {
        _selectedState = selectedState;
        _guiCommands = guiCommands;
        _objectFinder = ObjectFinder.Self;
    }

    /// <summary>
    /// Responds to a variable being set. Currently only reacts to ChildrenLayout changes, which
    /// can change which other variables are shown, so it forces the grid to refresh.
    /// </summary>
    public void HandleVariableSet(ElementSave save1, InstanceSave? save2, string variableName, object? oldValue)
    {
        if (variableName == "ChildrenLayout")
        {
            // Changing these can result in different values being shown in the property grid
            _guiCommands.RefreshVariables(force: true);
        }
    }

    /// <summary>
    /// Returns whether <paramref name="variable"/> should be hidden from the variable grid given
    /// the current values reachable through <paramref name="finder"/>.
    /// </summary>
    public bool GetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder finder)
    {
        var rootName = variable.GetRootName();

        switch (rootName)
        {
            case "Alpha":
            case "Blend":
            case "SourceShaderFile":
                return GetIfRenderTargetOnlyVariableIsExcluded(finder);
            case "AutoGridHorizontalCells":
            case "AutoGridVerticalCells":
                return GetIfAutoGridIsExcluded(finder);
            case "BaseType":
                return GetIfBaseTypeIsExcluded(finder);
            case "StackSpacing":
                return GetIfStackSpacingIsExcluded(finder);
            case "WrapsChildren":
                return GetIfWrapsChildrenIsExcluded(finder);
            case "TextOverflowHorizontalMode":
                return GetIfOverflowHorizontalModeExcluded(finder);
            case "Wrap":
                return GetIfWrapIsExcluded(finder);
        }

        // Shape gradient / dropshadow / stroke channel exclusions
        var prefix = string.IsNullOrEmpty(variable.SourceObject) ? "" : variable.SourceObject + '.';
        if (_shapeVariableExclusionLogic.GetIfShapeVariableIsExcluded(rootName, finder, GetCurrentRootStandardTypeName(), prefix, out bool shapeExcluded))
        {
            return shapeExcluded;
        }

        // Sprite texture exclusions
        if (GetIfSpriteVariableIsExcluded(variable, rootName, finder, out bool spriteExcluded))
        {
            return spriteExcluded;
        }

        // Text font exclusions
        if (GetIfTextVariableIsExcluded(rootName, finder, out bool textExcluded))
        {
            return textExcluded;
        }

        return false;
    }

    #region Shape Exclusions (gradient / dropshadow / stroke)

    private string? GetCurrentRootStandardTypeName()
    {
        ElementSave? element = _selectedState.SelectedElement;

        if (element != null && _selectedState.SelectedInstance != null)
        {
            element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance);
        }

        if (element != null)
        {
            return ObjectFinder.Self.GetRootStandardElementSave(element)?.Name;
        }
        return null;
    }

    #endregion

    private bool IsSelectionContainer
    {
        get
        {
            ElementSave? element = _selectedState.SelectedElement;

            if (element != null && _selectedState.SelectedInstance != null)
            {
                element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance);
            }

            ElementSave? baseElement = null;

            if (element != null)
            {
                baseElement = ObjectFinder.Self.GetRootStandardElementSave(element);
            }

            var isContainer =
                baseElement?.Name == "Container";
            return isContainer;
        }
    }

    // Alpha, Blend, and SourceShaderFile only have an effect on a render-target Container, so hide
    // them on a non-render-target Container (mirroring how the runtime ignores them there).
    private bool GetIfRenderTargetOnlyVariableIsExcluded(RecursiveVariableFinder finder)
    {
        if (IsSelectionContainer)
        {
            return finder.GetValue("IsRenderTarget") as bool? == false;
        }
        return false;
    }

    private bool GetIfBaseTypeIsExcluded(RecursiveVariableFinder finder)
    {
        // only if we are dealing with a screen:
        var currentElement = _selectedState.SelectedScreen;

        if (currentElement is ScreenSave currentScreen && _selectedState.SelectedInstance == null)
        {
            // exclude it if the value is null and there is only one screen:
            var isOnlyScreen = _objectFinder.GumProjectSave?.Screens.Count == 1;
            var isEmpty = string.IsNullOrEmpty(currentScreen.BaseType);

            return isOnlyScreen && isEmpty;
        }

        return false;
    }

    private bool GetIfOverflowHorizontalModeExcluded(RecursiveVariableFinder finder)
    {
        var overflowVertical = finder.GetVariable("TextOverflowVerticalMode")?.Value;

        if (overflowVertical is TextOverflowVerticalMode textOverflowVerticalMode &&
            textOverflowVerticalMode == TextOverflowVerticalMode.SpillOver)
        {
            return true;
        }

        return false;
    }

    private bool GetIfWrapIsExcluded(RecursiveVariableFinder finder)
    {
        var textureAddress = finder.GetVariable("TextureAddress")?.Value;

        if (textureAddress is TextureAddress.EntireTexture)
        {
            return true;
        }
        return false;
    }

    private bool GetIfStackSpacingIsExcluded(RecursiveVariableFinder finder)
    {
        var childrenLayoutVariable = finder.GetVariable("ChildrenLayout");
        var showSpacing = false;
        if (childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
        {
            showSpacing =
                childrenLayout == ChildrenLayout.LeftToRightStack ||
                childrenLayout == ChildrenLayout.TopToBottomStack ||
                childrenLayout == ChildrenLayout.AutoGridHorizontal ||
                childrenLayout == ChildrenLayout.AutoGridVertical;
        }
        return !showSpacing;
    }

    private bool GetIfWrapsChildrenIsExcluded(RecursiveVariableFinder finder)
    {
        var childrenLayoutVariable = finder.GetVariable("ChildrenLayout");
        var isStack = false;
        if (childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
        {
            isStack = childrenLayout == ChildrenLayout.LeftToRightStack || childrenLayout == ChildrenLayout.TopToBottomStack;
        }
        return !isStack;
    }

    private bool GetIfAutoGridIsExcluded(RecursiveVariableFinder finder)
    {
        var childrenLayoutVariable = finder.GetVariable("ChildrenLayout");

        var isAuto = false;
        if (childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
        {
            isAuto = childrenLayout == ChildrenLayout.AutoGridHorizontal || childrenLayout == ChildrenLayout.AutoGridVertical;
        }
        return !isAuto;
    }

    #region Sprite Exclusions

    private bool GetIfSpriteVariableIsExcluded(VariableSave variable, string rootName, RecursiveVariableFinder rvf, out bool shouldExclude)
    {
        string prefix = string.IsNullOrEmpty(variable.SourceObject) ? "" : variable.SourceObject + ".";

        if (string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(rvf.ElementStack.Last().InstanceName) && rvf.ContainerType != RecursiveVariableFinder.VariableContainerType.InstanceSave)
        {
            prefix = rvf.ElementStack.Last().InstanceName + ".";
        }

        if (rootName == "TextureTop" || rootName == "TextureLeft")
        {
            var addressMode = rvf.GetValue<TextureAddress>($"{prefix}TextureAddress");
            shouldExclude = addressMode == TextureAddress.EntireTexture;
            return true;
        }

        if (rootName == "TextureWidth" || rootName == "TextureHeight")
        {
            var addressMode = rvf.GetValue<TextureAddress>($"{prefix}TextureAddress");
            shouldExclude = addressMode == TextureAddress.EntireTexture ||
                addressMode == TextureAddress.DimensionsBased;
            return true;
        }

        if (rootName == "TextureWidthScale" || rootName == "TextureHeightScale")
        {
            var addressMode = rvf.GetValue<TextureAddress>($"{prefix}TextureAddress");
            shouldExclude = addressMode == TextureAddress.EntireTexture ||
                addressMode == TextureAddress.Custom;
            return true;
        }

        shouldExclude = false;
        return false;
    }

    #endregion

    #region Text Exclusions

    private bool GetIfTextVariableIsExcluded(string rootName, RecursiveVariableFinder rvf, out bool shouldExclude)
    {
        if (rootName == "Font" || rootName == "FontSize" || rootName == "OutlineThickness" || rootName == "UseFontSmoothing" || rootName == "IsItalic" || rootName == "IsBold")
        {
            bool useCustomFont = rvf.GetValue<bool>("UseCustomFont");
            shouldExclude = useCustomFont;
            return true;
        }
        else if (rootName == "CustomFontFile")
        {
            bool useCustomFont = rvf.GetValue<bool>("UseCustomFont");
            shouldExclude = !useCustomFont;
            return true;
        }

        shouldExclude = false;
        return false;
    }

    #endregion
}
