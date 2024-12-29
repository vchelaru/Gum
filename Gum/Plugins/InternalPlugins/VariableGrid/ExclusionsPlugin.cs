using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins.BaseClasses;
using Gum.ToolStates;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;

namespace Gum.Plugins.InternalPlugins.VariableGrid;

[Export(typeof(PluginBase))]
public class ExclusionsPlugin : InternalPlugin
{

    // These properties may require some changes to the grid, so we refresh the tree view
    // and entire grid.
    // There's lots of work that can/should be done here:
    // 1. We should have the plugins that handle excluding variables also
    //    report whether a variable requires refreshing
    // 2. We could only refresh the grid for some variables like UseCustomFont
    // 3. We could have only certain variable refresh themselves instead of the entire 
    //    grid.
    public static HashSet<string> VariablesRequiringRefresh = new HashSet<string>
    {
        "Parent",
        "Name",
        "UseCustomFont",
        "Texture Address",
        "Base Type",
        "TextOverflowVerticalMode",

    };
    private readonly ISelectedState _selectedState;
    private ObjectFinder _objectFinder;

    public ExclusionsPlugin()
    {
        _selectedState = SelectedState.Self;
        _objectFinder = ObjectFinder.Self;
    }

    public override void StartUp()
    {
        this.VariableExcluded += HandleGetIfVariableIsExcluded;
        this.VariableSet += HandleVariableSet;
    }

    private void HandleVariableSet(ElementSave save1, InstanceSave save2, string variableName, object oldValue)
    {
        if(variableName == "Children Layout")
        {
            // Changing children layout can result in different values being shown in the property grid
            GumCommands.Self.GuiCommands.RefreshVariables(force:true);
        }
    }

    private bool HandleGetIfVariableIsExcluded(VariableSave variable, RecursiveVariableFinder finder)
    {
        switch(variable.Name)
        {
            case "AutoGridHorizontalCells":
            case "AutoGridVerticalCells":
                return GetIfAutoGridIsExcluded(finder);
            case "Base Type":
                return GetIfBaseTypeIsExcluded(finder);
            case "StackSpacing":
            case "Wraps Children":
                return GetIfSpacingAndWrapsChildrenIsExcluded(finder);
            case "TextOverflowHorizontalMode":
                return GetIfOverflowHorizontalModeExcluded(finder);
            case "Wrap":
                return GetIfWrapIsExcluded(finder);

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
        var textureAddress = finder.GetVariable("Texture Address")?.Value;

        if(textureAddress is TextureAddress.EntireTexture)
        {
            return true;
        }
        return false;
    }

    private static bool GetIfSpacingAndWrapsChildrenIsExcluded(RecursiveVariableFinder finder)
    {
        var childrenLayoutVariable = finder.GetVariable("Children Layout");
        var isStack = false;
        if (childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
        {
            isStack = childrenLayout == ChildrenLayout.LeftToRightStack || childrenLayout == ChildrenLayout.TopToBottomStack;
        }
        return !isStack;
    }

    private static bool GetIfAutoGridIsExcluded(RecursiveVariableFinder finder)
    {
        var childrenLayoutVariable = finder.GetVariable("Children Layout");

        var isAuto = false;
        if (childrenLayoutVariable?.Value is ChildrenLayout childrenLayout)
        {
            isAuto = childrenLayout == ChildrenLayout.AutoGridHorizontal || childrenLayout == ChildrenLayout.AutoGridVertical;
        }
        return !isAuto;
    }
}
