using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.Services;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

/// <summary>
/// Handles variable exclusions for common (non-plugin) standard element types.
/// Plugins can add their own exclusions through the VariableExcluded event.
/// For example, the Skia plugin (MainSkiaPlugin / DefaultStateManager) handles
/// exclusions for stroke, gradient, and dropshadow variables.
/// </summary>
[Export(typeof(PluginBase))]
public class ExclusionsPlugin : InternalPlugin
{
    private readonly ISelectedState _selectedState;
    private ObjectFinder _objectFinder;

    public ExclusionsPlugin()
    {
        _selectedState = Locator.GetRequiredService<ISelectedState>();
        _objectFinder = ObjectFinder.Self;
    }

    public override void StartUp()
    {
        this.VariableExcluded += HandleGetIfVariableIsExcluded;
        this.VariableSet += HandleVariableSet;
    }

    private void HandleVariableSet(ElementSave save1, InstanceSave save2, string variableName, object oldValue)
    {
        if(variableName == "ChildrenLayout")
        {
            // Changing these can result in different values being shown in the property grid
            _guiCommands.RefreshVariables(force:true);
        }
    }

    private bool HandleGetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder finder)
    {
        var rootName = variable.GetRootName();

        switch(rootName)
        {
            case "Alpha":
                return GetIfAlphaIsExcluded(finder);
            case "Blend":
                return GetIfBlendIsExcluded(finder);
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

        // Sprite texture exclusions
        if(GetIfSpriteVariableIsExcluded(variable, rootName, finder, out bool spriteExcluded))
        {
            return spriteExcluded;
        }

        // Text font exclusions
        if(GetIfTextVariableIsExcluded(rootName, finder, out bool textExcluded))
        {
            return textExcluded;
        }

        return false;
    }


    bool IsSelectionContainer
    {
        get
        {
            ElementSave element = _selectedState.SelectedElement;

            if (element != null && _selectedState.SelectedInstance != null)
            {
                element = ObjectFinder.Self.GetElementSave(_selectedState.SelectedInstance);
            }

            ElementSave baseElement = null;

            if (element != null)
            {
                baseElement = ObjectFinder.Self.GetRootStandardElementSave(element);
            }

            var isContainer =
                baseElement?.Name == "Container";
            return isContainer;
        }
    }

    private bool GetIfBlendIsExcluded(RecursiveVariableFinder finder)
    {
        if (IsSelectionContainer)
        {
            return finder.GetValue("IsRenderTarget") as bool? == false;
        }
        return false;
    }


    private bool GetIfAlphaIsExcluded(RecursiveVariableFinder finder)
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

        if(currentElement is ScreenSave currentScreen && _selectedState.SelectedInstance == null)
        {
            // exclude it if the value is null and there is only one screen:
            var isOnlyScreen = _objectFinder.GumProjectSave.Screens.Count == 1;
            var isEmpty = string.IsNullOrEmpty(currentScreen.BaseType);

            return isOnlyScreen && isEmpty;
        }

        return false;
    }

    private bool GetIfOverflowHorizontalModeExcluded(RecursiveVariableFinder finder)
    {
        var overflowVertical = finder.GetVariable("TextOverflowVerticalMode")?.Value;

        if(overflowVertical is TextOverflowVerticalMode textOverflowVerticalMode &&
            textOverflowVerticalMode == TextOverflowVerticalMode.SpillOver)
        {
            return true;
        }

        return false;
    }

    private bool GetIfWrapIsExcluded(RecursiveVariableFinder finder)
    {
        var textureAddress = finder.GetVariable("TextureAddress")?.Value;

        if(textureAddress is TextureAddress.EntireTexture)
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

                ;
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

        if(string.IsNullOrEmpty(prefix) && !string.IsNullOrEmpty(rvf.ElementStack.Last().InstanceName) && rvf.ContainerType != RecursiveVariableFinder.VariableContainerType.InstanceSave)
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
        if(rootName == "Font" || rootName == "FontSize" || rootName == "OutlineThickness" || rootName == "UseFontSmoothing" || rootName == "IsItalic" || rootName == "IsBold")
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
