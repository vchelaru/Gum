using CodeOutputPlugin.Models;
using Gum;
using Gum.Converters;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.ToolStates;
using Newtonsoft.Json.Linq;
using RenderingLibrary.Graphics;
using RenderingLibrary.Math;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using System.Xml.Linq;
using ToolsUtilities;
using static System.Windows.Forms.AxHost;

namespace CodeOutputPlugin.Manager
{
    #region Enums

    public enum VisualApi
    {
        Gum,
        XamarinForms
    }

    #endregion

    #region CodeGenerationContext Class

    public struct CodeGenerationContext
    {
        /// <summary>
        /// the prefix with no period, such as "casted"
        /// </summary>
        public string ThisPrefix { get; set; }
        public InstanceSave Instance { get; set; }
        public ElementSave Element { get; set; }

        public CodeOutputProjectSettings CodeOutputProjectSettings { get; set; }

        public string GumVariablePrefix
        {
            get
            {
                if (Instance == null)
                {
                    return String.Empty;
                }
                else
                {
                    return Instance.Name + ".";
                }
            }
        }

        /// <summary>
        /// The prefix of the code, with tabs and no trailing period
        /// </summary>
        public string CodePrefix
        {
            get
            {
                return ToTabs(TabCount) + CodePrefixNoTabs;
            }
        }

        public string CodePrefixNoTabs
        {
            get
            {
                if (Instance == null)
                {
                    if (string.IsNullOrEmpty(ThisPrefix))
                    {
                        return "this";
                    }
                    else
                    {
                        return ThisPrefix;
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(ThisPrefix))
                    {
                        return "this." + Instance.Name;
                    }
                    else
                    {
                        return ThisPrefix + "." + Instance.Name;
                    }
                }
            }
        }

        private static string ToTabs(int tabCount) => new string(' ', tabCount * 4);

        public string Tabs => new string(' ', TabCount * 4);

        public int TabCount { get; internal set; }

        public VisualApi VisualApi => CodeGenerator.GetVisualApiForElement(Element);

    }

    #endregion

    public static class CodeGenerator
    {
        #region Fields/Properties

        public static int CanvasWidth { get; set; } = 480;
        public static int CanvasHeight { get; set; } = 854;

        /// <summary>
        /// if true, then pixel sizes are maintained regardless of pixel density. This allows layouts to maintain pixel-perfect.
        /// Update: This is now set to false because .... well, it makes it hard to create flexible layouts. It's best to set a resolution of 
        /// 320 wide and let density scale things up
        /// </summary>
        static bool AdjustPixelValuesForDensity { get; set; } = false;

        #endregion

        #region Initialize

        static bool DoesElementInheritFromCodeGeneratedElement(ElementSave element, CodeOutputProjectSettings projectSettings)
        {
            var foundBase = ObjectFinder.Self.GetElementSave(element.BaseType);
            var isDerived = foundBase != null && (foundBase is StandardElementSave) == false;

            if (isDerived && projectSettings?.BaseTypesNotCodeGenerated != null)
            {
                // check the settings:
                var split = projectSettings?.BaseTypesNotCodeGenerated.Split('\n').Select(item => item.Trim()).ToArray();

                if (split.Contains(element.BaseType))
                {
                    isDerived = false;
                }
            }

            return isDerived;
        }

        private static void GenerateInitializeInstancesMethod(ElementSave element, VisualApi visualApi, int tabCount, StringBuilder stringBuilder, CodeOutputProjectSettings settings)
        {
            var isDerived = DoesElementInheritFromCodeGeneratedElement(element, settings);

            var virtualOrOverride = isDerived
                ? "override"
                : "virtual";

            var line = $"protected {virtualOrOverride} void InitializeInstances()";
            stringBuilder.AppendLine(ToTabs(tabCount) + line);
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            CodeGenerationContext context = new CodeGenerationContext();
            context.Instance = null;
            context.Element = element;
            context.TabCount = tabCount;

            if (isDerived)
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + "base.InitializeInstances();");
            }

            foreach (var instance in element.Instances)
            {
                if (!instance.DefinedByBase)
                {
                    context.Instance = instance;

                    FillWithInstanceInstantiation(instance, element, stringBuilder, tabCount);
                }
            }

            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
        }

        #endregion

        #region Position / Size

        private static void ProcessXamarinFormsPositionAndSize(List<VariableSave> variablesToConsider, StateSave state, InstanceSave instance, ElementSave container, StringBuilder stringBuilder, CodeGenerationContext context)
        {
            //////////////////Early out/////////////////////
            if (container is ScreenSave && instance == null)
            {
                // screens can't be positioned
                return;
            }
            /////////////// End Early Out/////////////

            string variablePrefix = instance?.Name == null ? "" : "" + instance.Name + ".";

            bool setsAny = GetIfStateSetsAnyPositionValues(state, variablePrefix, variablesToConsider);

            InstanceSave parent = null;
            if (instance != null)
            {
                var parentName = state.GetValueRecursive(instance.Name + ".Parent") as string;
                if (!string.IsNullOrEmpty(parentName))
                {
                    parent = container.GetInstance(parentName);
                }
            }

            string parentType = parent?.BaseType;
            if (parent == null)
            {
                if(instance == null && context.VisualApi == VisualApi.XamarinForms)
                {
                    // The most common layout is in a XamForms absolute layout so let's use that. This may also
                    // contain values whcih are okay for stacklayout
                    parentType = "/AbsoluteLayout";
                }
                else if (container is ScreenSave)
                {
                    parentType = "/AbsoluteLayout";
                }
                else
                {
                    parentType = container.BaseType;
                }
            }

            // Only run this code if any of the properties are set or if we're in default. Otherwise
            // categorized states may screw up the positioning of an object.
            if (setsAny || state == container.DefaultState)
            {
                if (parentType?.EndsWith("/AbsoluteLayout") == true)
                {
                    SetAbsoluteLayoutPosition(variablesToConsider, state, context, stringBuilder, parentType);
                }
                else //if(parent?.BaseType?.EndsWith("/StackLayout") == true)
                {
                    SetNonAbsoluteLayoutPosition(variablesToConsider, state, context, stringBuilder, parentType);
                }
            }

        }

        private static void SetNonAbsoluteLayoutPosition(List<VariableSave> variablesToConsider, StateSave defaultState, CodeGenerationContext context,
            StringBuilder stringBuilder, string parentBaseType)
        {
            var variableFinder = new RecursiveVariableFinder(defaultState);

            var variablePrefix = context.GumVariablePrefix;

            bool setsAny = GetIfStateSetsAnyPositionValues(defaultState, variablePrefix, variablesToConsider);

            var isVariableOwnerAbsoluteLayout = false;
            if (context.Instance != null)
            {
                isVariableOwnerAbsoluteLayout = context.Instance.BaseType?.EndsWith("/AbsoluteLayout") == true;
            }
            else
            {
                isVariableOwnerAbsoluteLayout = context.Element.BaseType?.EndsWith("/AbsoluteLayout") == true;
            }
            var isContainedInStackLayout = parentBaseType?.EndsWith("/StackLayout") == true;

            #region Get recursive values for position and size
            var x = variableFinder.GetValue<float>(variablePrefix + "X");
            var y = variableFinder.GetValue<float>(variablePrefix + "Y");
            var width = variableFinder.GetValue<float>(variablePrefix + "Width");
            var height = variableFinder.GetValue<float>(variablePrefix + "Height");

            var xUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "X Units");
            var yUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "Y Units");
            var widthUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "Width Units");
            var heightUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "Height Units");

            var xOrigin = variableFinder.GetValue<HorizontalAlignment>(variablePrefix + "X Origin");
            var yOrigin = variableFinder.GetValue<VerticalAlignment>(variablePrefix + "Y Origin");

            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Width");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Height");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X Units");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y Units");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Width Units");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Height Units");

            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X Origin");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y Origin");

            #endregion

            var codePrefix = context.CodePrefix;

            float leftMargin = 0;
            float rightMargin = 0;
            float topMargin = 0;
            float bottomMargin = 0;

            float leftPadding = 0;
            float rightPadding = 0;
            float topPadding = 0;
            float bottomPadding = 0;

            #region Apply WidthUnits

            if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                var multiple = "1.0f";
                if (widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                {
                    multiple = "RenderingLibrary.SystemManagers.GlobalFontScale";
                }
                if (AdjustPixelValuesForDensity)
                {
                    multiple += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                stringBuilder.AppendLine(
                    $"{codePrefix}.WidthRequest = {width.ToString(CultureInfo.InvariantCulture)}f * {multiple};");
            }

            #endregion

            #region Apply HeightUnits

            if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                var multiple = "1.0f";

                if (heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                {
                    multiple = "RenderingLibrary.SystemManagers.GlobalFontScale";
                }
                if (AdjustPixelValuesForDensity)
                {
                    multiple += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }

                stringBuilder.AppendLine(
                    $"{codePrefix}.HeightRequest = {height.ToString(CultureInfo.InvariantCulture)}f * {multiple};");
            }
            else if(heightUnits == DimensionUnitType.RelativeToChildren)
            {
                if(isVariableOwnerAbsoluteLayout)
                {
                    stringBuilder.AppendLine(context.Tabs + $"Intentional error: The object {context.Instance?.Name ?? "this"} height depends on its children, but it is an absolute layout. Use a StackLayout instead!");
                }
            }

            // If it's in a stack layout and it uses a height request of RelativeToParent, generate a compile error. This is not allowed!
            if(heightUnits == DimensionUnitType.RelativeToContainer && isContainedInStackLayout)
            {
                stringBuilder.AppendLine(context.Tabs + 
                    $"Intentional compile error - the object {context.Instance?.Name ?? context.Element.Name} has a parent which is not an absolute layout, but its height is RelativeToContainer. This is not allowed in Xamarin Forms. The parent should be an Absolute layout in this case.");
            }
            #endregion



            #region Apply XUnits

            if (xUnits == PositionUnitType.PixelsFromLeft)
            {
                leftMargin = x;
            }
            if (xUnits == PositionUnitType.PixelsFromLeft && widthUnits == DimensionUnitType.RelativeToContainer)
            {
                rightMargin = -width - x;
            }
            if (xUnits == PositionUnitType.PixelsFromCenterX &&
                xOrigin == HorizontalAlignment.Center)
            {
                if (widthUnits == DimensionUnitType.RelativeToContainer)
                {
                    leftMargin = x - width / 2.0f;
                    rightMargin = -x - width / 2.0f;
                }
                else if (widthUnits == DimensionUnitType.Absolute ||
                    widthUnits == DimensionUnitType.RelativeToChildren ||
                    widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale ||
                    widthUnits == DimensionUnitType.MaintainFileAspectRatio ||
                    widthUnits == DimensionUnitType.PercentageOfOtherDimension ||
                    widthUnits == DimensionUnitType.PercentageOfSourceFile ||
                    widthUnits == DimensionUnitType.Ratio
                    )
                {
                    // Vic says - not sure why but we have to 2x the margin here...
                    leftMargin = x * 2;
                }
            }
            else if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
            {
                rightMargin = -x;
                if (widthUnits == DimensionUnitType.RelativeToContainer)
                {
                    leftMargin = -width;
                }
            }

            #endregion

            #region Apply YUnits

            if (yUnits == PositionUnitType.PixelsFromTop)
            {
                topMargin = y;
                if (heightUnits == DimensionUnitType.RelativeToChildren)
                {
                    if (isContainedInStackLayout == false)
                    {
                        // If it's a stack layout, we don't want to subtract from here.
                        // Update Feb 14, 2022
                        // Not sure why we subtract the height...
                        //bottomMargin = -height - y;
                        // If a Gum object is relative to children with
                        // a height of 10, that means it should be 10 units
                        // bigger than its children, so we should add 10
                        // Update November 16, 2022
                        // Why do we subtract the Y value? Shouldn't it just be
                        // whatever the extra height? Not sure if it should be in all situations
                        // or only when YOrigin is top, so let's be safe and check to only change the
                        // one case:
                        if (yOrigin == VerticalAlignment.Top)
                        {
                            bottomMargin = height;
                        }
                        else
                        {
                            bottomMargin = height - y;
                        }
                    }
                    else
                    {
                        // in a stack layout, so give the margin according to the y origin:
                        if (yOrigin == VerticalAlignment.Top)
                        {
                            // Adding a margin will move the next item after this, but it doesn't make "this" bigger...
                            //bottomMargin = height;
                            //... so instead, we use padding
                            bottomPadding = height;
                        }
                        else if (yOrigin == VerticalAlignment.Center)
                        {
                            // does this need padding...?
                            topMargin += height / 2.0f;
                            bottomMargin = height / 2.0f;
                        }
                        else if (yOrigin == VerticalAlignment.Bottom)
                        {
                            //... or this?
                            topMargin += height;
                        }
                    }
                }
            }
            if (yUnits == PositionUnitType.PixelsFromCenterY &&
                yOrigin == VerticalAlignment.Center)
            {
                if (heightUnits == DimensionUnitType.RelativeToContainer)
                {
                    topMargin = y - height / 2.0f;
                    bottomMargin = -y - height / 2.0f;
                }
                // else - should we copy the code above for width units?
            }

            #endregion

            if (isVariableOwnerAbsoluteLayout && heightUnits == DimensionUnitType.RelativeToChildren)
            {
                stringBuilder.AppendLine($"Error: The object {context.Instance?.ToString() ?? context.Element?.ToString()} uses a HeightUnits of RelativeToChildren, but it is an AbsoluteLayout which is not supported in Xamarin.Forms");
            }

            if (setsAny)
            {
                if (AdjustPixelValuesForDensity)
                {
                    stringBuilder.AppendLine($"{codePrefix}.Margin = new Thickness(" +
                        $"{leftMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, " +
                        $"{topMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, " +
                        $"{rightMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, " +
                        $"{bottomMargin.ToString(CultureInfo.InvariantCulture)}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density);");

                }
                else
                {
                    stringBuilder.AppendLine($"{codePrefix}.Margin = new Thickness(" +
                        $"{leftMargin.ToString(CultureInfo.InvariantCulture)}, " +
                        $"{topMargin.ToString(CultureInfo.InvariantCulture)}, " +
                        $"{rightMargin.ToString(CultureInfo.InvariantCulture)}, " +
                        $"{bottomMargin.ToString(CultureInfo.InvariantCulture)});");
                }
            }

            #region Write Padding
            var hasPadding = topPadding != 0 || leftPadding != 0 || rightPadding != 0 || bottomPadding != 0;
            if (hasPadding)
            {
                stringBuilder.AppendLine($"{codePrefix}.Padding = new Thickness(" +
                    $"{leftPadding.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{topPadding.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{rightPadding.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{bottomPadding.ToString(CultureInfo.InvariantCulture)});");
            }
            #endregion

            #region Write HorizontalOptions
            if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.RelativeToChildren || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                if(setsAny)
                {
                    if (xUnits == PositionUnitType.PixelsFromCenterX && xOrigin == HorizontalAlignment.Center)
                    {
                        stringBuilder.AppendLine(
                            $"{codePrefix}.HorizontalOptions = LayoutOptions.Center;");
                    }
                    else if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
                    {
                        stringBuilder.AppendLine(
                            $"{codePrefix}.HorizontalOptions = LayoutOptions.End;");
                    }
                    else
                    {
                        stringBuilder.AppendLine(
                            $"{codePrefix}.HorizontalOptions = LayoutOptions.Start;");

                    }
                }
            }
            else if (widthUnits == DimensionUnitType.RelativeToContainer ||
                widthUnits == DimensionUnitType.Percentage)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.HorizontalOptions = LayoutOptions.Fill;");
            }

            #endregion

            #region Write Vertical Options

            if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.RelativeToChildren || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                if (yUnits == PositionUnitType.PixelsFromCenterY && xOrigin == HorizontalAlignment.Center)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.VerticalOptions = LayoutOptions.Center;");
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.VerticalOptions = LayoutOptions.Start;");
                }
            }
            else if (heightUnits == DimensionUnitType.RelativeToContainer ||
                heightUnits == DimensionUnitType.Percentage)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.Fill;");
            }

            #endregion

            bool isVariableOwnerSkiaGumCanvasView = IsVariableOwnerSkiaGumCanvasView(ref context);

            if (isVariableOwnerSkiaGumCanvasView)
            {
                if (heightUnits == DimensionUnitType.RelativeToChildren)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.AutoSizeHeightAccordingToContents = true;");
                }
                if (widthUnits == DimensionUnitType.RelativeToChildren)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.AutoSizeWidthAccordingToContents = true;");
                }
            }
        }

        #region Const values
        const string WidthProportionalFlag = "AbsoluteLayoutFlags.WidthProportional";
        const string HeightProportionalFlag = "AbsoluteLayoutFlags.HeightProportional";
        const string XProportionalFlag = "AbsoluteLayoutFlags.XProportional";
        const string YProportionalFlag = "AbsoluteLayoutFlags.YProportional";
        #endregion

        private static void SetAbsoluteLayoutPosition(List<VariableSave> variablesToConsider, StateSave state, CodeGenerationContext context, StringBuilder stringBuilder, string parentBaseType)
        {
            var variableFinder = new RecursiveVariableFinder(state);

            var variablePrefix = context.GumVariablePrefix;

            // October 18, 2022
            // If an instance has
            // its position controlled
            // by a state, then the state
            // is responsible for doing the
            // layout. However, if we always
            // append the bounds text, then we
            // will overwrite the state setting.
            // Therefore, we should only do it if
            // a position-related value is set:
            bool setsAny = GetIfStateSetsAnyPositionValues(state, variablePrefix, variablesToConsider);

            var isVariableOwnerAbsoluteLayout = false;
            if (context.Instance != null)
            {
                isVariableOwnerAbsoluteLayout = context.Instance.BaseType?.EndsWith("/AbsoluteLayout") == true;
            }
            else
            {
                isVariableOwnerAbsoluteLayout = context.Element.BaseType?.EndsWith("/AbsoluteLayout") == true;
            }

            #region Get recursive values for position and size

            var x = variableFinder.GetValue<float>(variablePrefix + "X");
            var y = variableFinder.GetValue<float>(variablePrefix + "Y");
            var width = variableFinder.GetValue<float>(variablePrefix + "Width");
            var height = variableFinder.GetValue<float>(variablePrefix + "Height");
            var originalHeight = height;

            var xUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "X Units");
            var yUnits = variableFinder.GetValue<PositionUnitType>(variablePrefix + "Y Units");
            var widthUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "Width Units");
            var heightUnits = variableFinder.GetValue<DimensionUnitType>(variablePrefix + "Height Units");

            var xOrigin = variableFinder.GetValue<HorizontalAlignment>(variablePrefix + "X Origin");
            var yOrigin = variableFinder.GetValue<VerticalAlignment>(variablePrefix + "Y Origin");

            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Width");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Height");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X Units");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y Units");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Width Units");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Height Units");

            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "X Origin");
            variablesToConsider.RemoveAll(item => item.Name == variablePrefix + "Y Origin");
            #endregion

            var codePrefix = context.CodePrefix;

            var proportionalFlags = new HashSet<string>();

            int leftMargin = 0;
            int topMargin = 0;
            int rightMargin = 0;
            int bottomMargin = 0;

            float leftPadding = 0;
            float rightPadding = 0;
            float topPadding = 0;
            float bottomPadding = 0;

            #region Apply Width Units

            if (widthUnits == DimensionUnitType.Percentage)
            {
                width /= 100.0f;
                proportionalFlags.Add(WidthProportionalFlag);
            }
            else if (widthUnits == DimensionUnitType.RelativeToContainer)
            {
                if (xOrigin == HorizontalAlignment.Center)
                {
                    // we'll achieve margins with offsets
                    leftMargin = MathFunctions.RoundToInt(x - width / 2);
                    rightMargin = MathFunctions.RoundToInt(-x - width / 2);
                }
                else if (xOrigin == HorizontalAlignment.Right)
                {
                    rightMargin = MathFunctions.RoundToInt(-x - width);
                    leftMargin = MathFunctions.RoundToInt(x - width);
                }
                else
                {
                    // we'll achieve margins with offsets
                    rightMargin = MathFunctions.RoundToInt(-x - width);
                }
                width = 1;
                proportionalFlags.Add(WidthProportionalFlag);
            }
            else if (widthUnits == DimensionUnitType.RelativeToChildren)
            {
                // in this case we want to auto-size, which is what -1 indicates
                width = -1;
            }

            #endregion

            #region Apply Height Units

            if (heightUnits == DimensionUnitType.Percentage)
            {
                height /= 100.0f;
                proportionalFlags.Add(HeightProportionalFlag);
            }
            else if (heightUnits == DimensionUnitType.RelativeToContainer)
            {
                // just like width units, achieve this with margins:
                if (yOrigin == VerticalAlignment.Center)
                {
                    topMargin = MathFunctions.RoundToInt(y - height / 2);
                    bottomMargin = MathFunctions.RoundToInt(-y - height / 2);
                }
                else
                {
                    bottomMargin = MathFunctions.RoundToInt(-y - height);
                }
                height = 1;
                proportionalFlags.Add(HeightProportionalFlag);
            }
            else if (heightUnits == DimensionUnitType.RelativeToChildren)
            {
                if (isVariableOwnerAbsoluteLayout)
                {
                    stringBuilder.AppendLine(context.Tabs + $"Intentional error: The object {context.Instance?.Name ?? "this"} height depends on its children, but it is an absolute layout. Use a StackLayout instead!");
                }
                // see above on width relative to container for information
                height = -1;
            }

            #endregion

            var isContainedInStackLayout = parentBaseType?.EndsWith("/StackLayout") == true;


            #region Apply XUnits

            // special case
            // If we're using the center with x=0 we'll pretend it's the same as 50% 
            if (xUnits == PositionUnitType.PixelsFromCenterX
                // why does the width unit even matter? Should be the same regardless of width unit...
                //widthUnits == DimensionUnitType.Absolute && 
                )
            {
                // Always do this (instead of only when X==0), because the margins will offset from center
                //if (x == 0)
                // treat it like it's 50%:
                if (xOrigin == HorizontalAlignment.Left)
                {
                    if (widthUnits == DimensionUnitType.Absolute)
                    {
                        leftMargin += (int)(width / 2.0f);
                        rightMargin -= (int)(width / 2.0f);
                    }
                }
                else if (xOrigin == HorizontalAlignment.Right)
                {
                    if (widthUnits == DimensionUnitType.Absolute)
                    {
                        leftMargin -= (int)(width / 2.0f);
                        rightMargin += (int)(width / 2.0f);
                    }
                }

                // we know the x units so just add:
                leftMargin += (int)x;
                rightMargin -= (int)x;

                // add X before setting X to .5

                x = .5f;
                proportionalFlags.Add(XProportionalFlag);
            }
            // Xamarin forms uses a weird anchoring system to combine both position and anchor into one value. Gum splits those into two values
            // We need to convert from the gum units to xamforms units:
            // for now assume it's all %'s:

            else if (xUnits == PositionUnitType.PercentageWidth)
            {
                x /= 100.0f;

                if (widthUnits == DimensionUnitType.Percentage)
                {
                    var adjustedCanvasWidth = 1 - width;
                    if (adjustedCanvasWidth > 0)
                    {
                        x /= adjustedCanvasWidth;
                    }
                }
                proportionalFlags.Add(XProportionalFlag);
            }
            else if (xUnits == PositionUnitType.PixelsFromLeft)
            {
                if (widthUnits == DimensionUnitType.RelativeToContainer)
                {
                    leftMargin = MathFunctions.RoundToInt(x);
                    x = 0;
                }

                if (xOrigin == HorizontalAlignment.Center)
                {
                    if (widthUnits == DimensionUnitType.Absolute)
                    {
                        leftMargin -= (int)(width / 2.0f);
                        rightMargin += (int)(width / 2.0f);
                    }
                }
                else if (xOrigin == HorizontalAlignment.Right)
                {
                    if (widthUnits == DimensionUnitType.Absolute)
                    {
                        leftMargin -= (int)(width);
                        rightMargin += (int)(width);
                    }
                }
            }
            else if (xUnits == PositionUnitType.PixelsFromRight)
            {
                if (xOrigin == HorizontalAlignment.Right)
                {
                    rightMargin = MathFunctions.RoundToInt(-x);
                }
                else if (xOrigin == HorizontalAlignment.Center)
                {
                    if (widthUnits == DimensionUnitType.Absolute)
                    {
                        rightMargin = MathFunctions.RoundToInt(-x - width / 2);
                        leftMargin = MathFunctions.RoundToInt(x + width / 2);
                    }
                }
                else if (xOrigin == HorizontalAlignment.Left)
                {
                    if (widthUnits == DimensionUnitType.Absolute)
                    {
                        rightMargin = MathFunctions.RoundToInt(-x - width);
                        leftMargin = MathFunctions.RoundToInt(x + width);
                    }
                }
                x = 1;
                proportionalFlags.Add(XProportionalFlag);
            }

            #endregion

            #region Apply YUnits

            if (yUnits == PositionUnitType.PixelsFromTop)
            {
                if (heightUnits == DimensionUnitType.RelativeToContainer)
                {
                    topMargin = MathFunctions.RoundToInt(y);
                    y = 0;

                    // originalHeight greater than 0 means we are going to make this bigger, but not have the 
                    // inner controls be abel to use that space
                    if (yOrigin == VerticalAlignment.Top && originalHeight > 0)
                    {
                        bottomPadding = originalHeight;
                    }

                }
                else if(heightUnits == DimensionUnitType.RelativeToChildren)
                {
                    if (yOrigin == VerticalAlignment.Top && originalHeight > 0)
                    {
                        bottomPadding = originalHeight;
                    }

                }
            }
            else if (yUnits == PositionUnitType.PixelsFromCenterY)
            {
                if (yOrigin == VerticalAlignment.Center)
                {
                    // If relative to container, it's already handled up above
                    if (heightUnits != DimensionUnitType.RelativeToContainer)
                    {
                        topMargin = MathFunctions.RoundToInt(y);
                        bottomMargin = MathFunctions.RoundToInt(-y);
                    }
                    y = .5f;
                    proportionalFlags.Add(YProportionalFlag);
                }
                else if (yOrigin == VerticalAlignment.Top)
                {
                    // the margin should be the height of this / 2
                    topMargin = MathFunctions.RoundToInt(y + height / 2.0f);
                    bottomMargin = -topMargin;
                    y = .5f;
                    proportionalFlags.Add(YProportionalFlag);
                }
                else if (yOrigin == VerticalAlignment.Top)
                {
                    // the margin should be the height of this / 2
                    topMargin = MathFunctions.RoundToInt(y - height / 2.0f);
                    y = .5f;
                    proportionalFlags.Add(YProportionalFlag);
                }
            }

            else if (yUnits == PositionUnitType.PercentageHeight)
            {
                y /= 100.0f;
                var adjustedCanvasHeight = 1 - height;
                if (adjustedCanvasHeight > 0)
                {
                    y /= adjustedCanvasHeight;
                }
                proportionalFlags.Add(YProportionalFlag);
            }
            else if (yUnits == PositionUnitType.PixelsFromBottom)
            {
                if (yOrigin == VerticalAlignment.Bottom)
                {
                    bottomMargin = MathFunctions.RoundToInt(-y);
                    y = 1;
                    proportionalFlags.Add(YProportionalFlag);
                }
                else
                {
                    // We could be smarter about this but we'll add this
                    // when it's needed.
                    y += CanvasHeight;
                }
            }


            #endregion

            #region Write Padding

            var hasPadding = topPadding != 0 || leftPadding != 0 || rightPadding != 0 || bottomPadding != 0;
            if (hasPadding)
            {
                stringBuilder.AppendLine($"{codePrefix}.Padding = new Thickness(" +
                    $"{leftPadding.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{topPadding.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{rightPadding.ToString(CultureInfo.InvariantCulture)}, " +
                    $"{bottomPadding.ToString(CultureInfo.InvariantCulture)});");
            }

            #endregion

            #region Write HorizontalOptions

            //If the object is width proportional, then it must use a .HorizontalOptions = LayoutOptions.Fill; or else the proportional width won't apply
            if (proportionalFlags.Contains(WidthProportionalFlag))
            {
                stringBuilder.AppendLine($"{context.CodePrefix}.HorizontalOptions = LayoutOptions.Fill;");
            }
            else if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.RelativeToChildren || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                // See setsAny variable definition for discussion about this check
                if(setsAny)
                {
                    if (xUnits == PositionUnitType.PixelsFromCenterX && xOrigin == HorizontalAlignment.Center)
                    {
                        stringBuilder.AppendLine(
                            $"{codePrefix}.HorizontalOptions = LayoutOptions.Center;");
                    }
                    else if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
                    {
                        stringBuilder.AppendLine(
                            $"{codePrefix}.HorizontalOptions = LayoutOptions.End;");
                    }
                    else
                    {
                        stringBuilder.AppendLine(
                            $"{codePrefix}.HorizontalOptions = LayoutOptions.Start;");

                    }
                }
            }
            else if (widthUnits == DimensionUnitType.RelativeToContainer ||
                widthUnits == DimensionUnitType.Percentage)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.HorizontalOptions = LayoutOptions.Fill;");
            }

            #endregion

            #region Write Vertical Options

            if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.RelativeToChildren || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                if (yUnits == PositionUnitType.PixelsFromCenterY && xOrigin == HorizontalAlignment.Center)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.VerticalOptions = LayoutOptions.Center;");
                }
                else if(yUnits == PositionUnitType.PixelsFromBottom && yOrigin == VerticalAlignment.Bottom)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.VerticalOptions = LayoutOptions.End;");
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.VerticalOptions = LayoutOptions.Start;");
                }
            }
            else if (heightUnits == DimensionUnitType.RelativeToContainer ||
                heightUnits == DimensionUnitType.Percentage)
            {
                stringBuilder.AppendLine(
                    $"{codePrefix}.VerticalOptions = LayoutOptions.Fill;");
            }

            #endregion

            var xString = x.ToString(CultureInfo.InvariantCulture) + "f";
            var yString = y.ToString(CultureInfo.InvariantCulture) + "f";
            var widthString = width.ToString(CultureInfo.InvariantCulture) + "f";
            var heightString = height.ToString(CultureInfo.InvariantCulture) + "f";

            if (heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                heightString = $"({heightString} * RenderingLibrary.SystemManagers.GlobalFontScale)";
            }
            if (widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                widthString = $"({widthString} * RenderingLibrary.SystemManagers.GlobalFontScale)";
            }

            // When using AbsoluteLayout in XamarinForms, adding a margin will actually shrink the object. Therefore, if using margin to 
            // position an object relative to its right or bottom edges, the object's width and height should be increased by the offset amount.

            if (xUnits == PositionUnitType.PixelsFromRight && xOrigin == HorizontalAlignment.Right)
            {
                if (widthUnits == DimensionUnitType.RelativeToChildren)
                {
                    widthString = $"({widthString})";
                }
                // Vic asks - do we still want to do this? Isn't this handled by the margins above:
                //else
                else if (widthUnits != DimensionUnitType.RelativeToContainer)
                {
                    widthString = $"({widthString} + {rightMargin})";
                }
            }
            if (yUnits == PositionUnitType.PixelsFromBottom && yOrigin == VerticalAlignment.Bottom)
            {
                if (heightUnits == DimensionUnitType.RelativeToChildren)
                {
                    heightString = $"({heightString})";
                }
                else
                {
                    heightString = $"({heightString} + {bottomMargin})";
                }
            }

            if (AdjustPixelValuesForDensity)
            {
                if (proportionalFlags.Contains(XProportionalFlag) == false)
                {
                    xString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                if (proportionalFlags.Contains(YProportionalFlag) == false)
                {
                    yString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                if (proportionalFlags.Contains(WidthProportionalFlag) == false && widthUnits != DimensionUnitType.RelativeToChildren)
                {
                    widthString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
                if (proportionalFlags.Contains(HeightProportionalFlag) == false && heightUnits != DimensionUnitType.RelativeToChildren)
                {
                    heightString += "/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                }
            }



            string boundsText =
                $"{ToTabs(context.TabCount)}AbsoluteLayout.SetLayoutBounds({context.CodePrefixNoTabs}, new Rectangle({xString}, {yString}, {widthString}, {heightString} ));";
            string flagsText = null;

            if (proportionalFlags.Count == 0)
            {
                // A default state could set values, we override it but don't use any porportional, we probably still want to adjust here...
                proportionalFlags.Add("AbsoluteLayoutFlags.None");
            }

            if (proportionalFlags.Count > 0)
            {
                string flagsArguments = null;
                int i = 0;
                foreach (var flag in proportionalFlags)
                {
                    if (i > 0)
                    {
                        flagsArguments += " | ";
                    }
                    flagsArguments += flag;
                    i++;
                }
                flagsText = $"{ToTabs(context.TabCount)}AbsoluteLayout.SetLayoutFlags({context.CodePrefixNoTabs}, {flagsArguments});";
            }

            // See variable definition for discussion:
            if(setsAny)
            {
                if (string.IsNullOrWhiteSpace(flagsText))
                {
                    stringBuilder.AppendLine(boundsText);
                }
                else
                {
                    stringBuilder.AppendLine($"{boundsText}\n{flagsText}");
                }
            }

            // not sure why these apply even though we're using values on the AbsoluteLayout
            if (!proportionalFlags.Contains(WidthProportionalFlag) && (widthUnits == DimensionUnitType.RelativeToContainer || widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale))
            {
                if (widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                {
                    stringBuilder.AppendLine($"{context.CodePrefix}.WidthRequest = {width.ToString(CultureInfo.InvariantCulture)}f * RenderingLibrary.SystemManagers.GlobalFontScale;");
                }
                else
                {
                    stringBuilder.AppendLine($"{context.CodePrefix}.WidthRequest = {width.ToString(CultureInfo.InvariantCulture)}f;");
                }
            }
            if (!proportionalFlags.Contains(HeightProportionalFlag) && (heightUnits == DimensionUnitType.RelativeToContainer || heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale))
            {
                if (heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
                {
                    stringBuilder.AppendLine($"{context.CodePrefix}.HeightRequest = {height.ToString(CultureInfo.InvariantCulture)}f * RenderingLibrary.SystemManagers.GlobalFontScale;");
                }
                else
                {
                    stringBuilder.AppendLine($"{context.CodePrefix}.HeightRequest = {height.ToString(CultureInfo.InvariantCulture)}f;");
                }
            }

            // If there are no margins, we should still explicitly set them. Otherwise, states that modify margins will not properly be set back
            //if (leftMargin != 0 || rightMargin != 0 || topMargin != 0 || bottomMargin != 0)
            if(AdjustPixelValuesForDensity)
            {
                stringBuilder.AppendLine($"{context.CodePrefix}.Margin = new Thickness({leftMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, {topMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, {rightMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density, {bottomMargin}/Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density);");
            }
            else
            {
                stringBuilder.AppendLine($"{context.CodePrefix}.Margin = new Thickness({leftMargin}, {topMargin}, {rightMargin}, {bottomMargin});");
            }
            // should we do the same to vertical? Maybe, but waiting for a natural use case to test it

            bool isVariableOwnerSkiaGumCanvasView = IsVariableOwnerSkiaGumCanvasView(ref context);

            if (isVariableOwnerSkiaGumCanvasView)
            {
                if (heightUnits == DimensionUnitType.RelativeToChildren)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.AutoSizeHeightAccordingToContents = true;");
                }
                if (widthUnits == DimensionUnitType.RelativeToChildren)
                {
                    stringBuilder.AppendLine(
                        $"{codePrefix}.AutoSizeWidthAccordingToContents = true;");
                }
            }
        }

        private static bool IsVariableOwnerSkiaGumCanvasView(ref CodeGenerationContext context)
        {
            var isVariableOwnerSkiaGumCanvasView = false;
            if (context.Instance != null)
            {
                isVariableOwnerSkiaGumCanvasView = context.Instance.BaseType?.EndsWith("/SkiaGumCanvasView") == true;

                if (!isVariableOwnerSkiaGumCanvasView)
                {
                    var element = ObjectFinder.Self.GetElementSave(context.Instance.BaseType);
                    return IsElementSkiaGumCanvasView(element);
                }
            }
            else
            {
                isVariableOwnerSkiaGumCanvasView = IsElementSkiaGumCanvasView(context.Element);
            }

            return isVariableOwnerSkiaGumCanvasView;
        }

        private static bool IsElementSkiaGumCanvasView(ElementSave element)
        {
            if (element.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
            {
                return true;
            }
            else
            {
                return ObjectFinder.Self.GetBaseElements(element)
                    .Any(item => item.BaseType?.EndsWith("/SkiaGumCanvasView") == true);
            }

        }

        private static float CalculateAbsoluteWidth(InstanceSave instance, ElementSave container, RecursiveVariableFinder variableFinder)
        {
            string prefix = instance?.Name == null ? "" : instance.Name + ".";


            var x = variableFinder.GetValue<float>(prefix + "X");
            var width = variableFinder.GetValue<float>(prefix + "Width");

            var xUnits = variableFinder.GetValue<PositionUnitType>(prefix + "X Units");
            var widthUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Width Units");

            var xOrigin = variableFinder.GetValue<HorizontalAlignment>(prefix + "X Origin");

            var parentName = variableFinder.GetValue<string>(prefix + "Parent");

            var parent = container.GetInstance(parentName);

            float toReturn = 0;
            if (instance == null)
            {
                toReturn = width; // handle this eventually?
            }
            else if (widthUnits == DimensionUnitType.Absolute || widthUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                toReturn = width;
            }
            else if (widthUnits == DimensionUnitType.RelativeToContainer)
            {
                if (parent == null)
                {
                    toReturn = width + GumState.Self.ProjectState.GumProjectSave.DefaultCanvasWidth;
                }
                else
                {
                    var parentWidth = CalculateAbsoluteWidth(parent, container, variableFinder);

                    toReturn = parentWidth + width;
                }
            }

            return toReturn;
        }

        private static float CalculateAbsoluteHeight(InstanceSave instance, ElementSave container, RecursiveVariableFinder variableFinder)
        {
            string prefix = instance?.Name == null ? "" : instance.Name + ".";


            var y = variableFinder.GetValue<float>(prefix + "Y");
            var height = variableFinder.GetValue<float>(prefix + "Height");

            var yUnits = variableFinder.GetValue<PositionUnitType>(prefix + "Y Units");
            var heightUnits = variableFinder.GetValue<DimensionUnitType>(prefix + "Height Units");

            var yOrigin = variableFinder.GetValue<HorizontalAlignment>(prefix + "Y Origin");

            var parentName = variableFinder.GetValue<string>(prefix + "Parent");

            var parent = container.GetInstance(parentName);

            float toReturn = 0;
            if (instance == null)
            {
                toReturn = height; // handle this eventually?
            }
            else if (heightUnits == DimensionUnitType.Absolute || heightUnits == DimensionUnitType.AbsoluteMultipliedByFontScale)
            {
                toReturn = height;
            }
            else if (heightUnits == DimensionUnitType.RelativeToContainer)
            {
                if (parent == null)
                {
                    toReturn = height + GumState.Self.ProjectState.GumProjectSave.DefaultCanvasHeight;
                }
                else
                {
                    var parentWidth = CalculateAbsoluteHeight(parent, container, variableFinder);

                    toReturn = parentWidth + height;
                }
            }

            return toReturn;
        }

        #endregion

        #region Parent

        private static void FillWithParentAssignments(InstanceSave instance, ElementSave container, StringBuilder stringBuilder, int tabCount = 0)
        {
            // Some history on this:
            // Initially parent assignment
            // was mixed in with normal variable
            // assignment. This was separated out
            // into its own method on July 14 so that
            // a derived element can assign parent and
            // children elements all at once. To do this, 
            // we'll get the recursive parent value instead
            // of relying on top-level variables:

            var defaultState = container.DefaultState;

            var rfv = new RecursiveVariableFinder(defaultState);

            var parentVariable = rfv.GetVariable($"{instance.Name}.Parent");

            //var parentVariables = GetVariablesForValueAssignmentCode(instance, container)
            //    // make "Parent" first
            //    // .. actually we need to make parent last so that it can properly assign parent on scrollables
            //    .Where(item => item.GetRootName() == "Parent")
            //    .ToList();

            VisualApi visualApi = GetVisualApiForInstance(instance, container);

            var tabs = new String(' ', 4 * tabCount);

            //FillWithVariableAssignments(instance, container, stringBuilder, tabCount, parentVariables);
            var context = new CodeGenerationContext();
            context.Element = container;
            context.Instance = instance;

            var parentValue = parentVariable?.Value as string;
            var hasParent = parentValue != null &&
                container.GetInstance(parentValue) != null;

            if (hasParent)
            {
                var codeLine = GetCodeLine(parentVariable, container, visualApi, defaultState, context);


                // the line of code could be " ", a string with a space. This happens
                // if we want to skip a variable so we dont return null or empty.
                // But we also don't want a ton of spaces generated.
                if (!string.IsNullOrWhiteSpace(codeLine))
                {
                    stringBuilder.AppendLine(tabs + codeLine);
                }

                var suffixCodeLine = GetSuffixCodeLine(instance, parentVariable, visualApi);
                if (!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(tabs + suffixCodeLine);
                }
            }

            // For scrollable GumContainers we need to have the parent assigned *after* the AbsoluteLayout rectangle:
            #region Assign Parent


            if (!hasParent)
            {
                if (visualApi == VisualApi.Gum)
                {
                    // add it to "this"
                    stringBuilder.AppendLine($"{tabs}this.Children.Add({instance.Name});");
                }
                else // forms
                {
                    var instanceBaseType = instance.BaseType;
                    var isContainerStackLayout = container.BaseType?.EndsWith("/StackLayout") == true;
                    var isContainerScrollView = container.BaseType?.EndsWith("/ScrollView") == true;
                    var isContainerAbsoluteLayout = container.BaseType?.EndsWith("/AbsoluteLayout") == true;

                    var instanceName = instance.Name;

                    if (instanceBaseType.EndsWith("/GumCollectionView"))
                    {
                        stringBuilder.AppendLine($"{tabs}var tempFor{instance.Name} = GumScrollBar.CreateScrollableAbsoluteLayout({instance.Name}, ScrollableLayoutParentPlacement.Free);");
                        instanceName = $"tempFor{instance.Name}";
                    }

                    if (isContainerStackLayout || isContainerAbsoluteLayout)
                    {
                        stringBuilder.AppendLine($"{tabs}this.Children.Add({instanceName});");
                    }
                    else if (DoesTypeHaveContent(container.BaseType))
                    {
                        stringBuilder.Append($"{tabs}this.Content = {instanceName};");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"{tabs}MainLayout.Children.Add({instanceName});");
                    }
                }
            }

            #endregion
        }

        private static void GenerateAddToParentsMethod(ElementSave element, VisualApi visualApi, int tabCount, StringBuilder stringBuilder, CodeOutputProjectSettings projectSettings)
        {
            var isDerived = DoesElementInheritFromCodeGeneratedElement(element, projectSettings);

            var virtualOrOverride = isDerived
                ? "override"
                : "virtual";

            var line = $"protected {virtualOrOverride} void AssignParents()";
            stringBuilder.AppendLine(ToTabs(tabCount) + line);
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            if (isDerived)
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + "// Intentionally do not call base.AssignParents so that this class can determine the addition of order");
            }

            foreach (var instance in element.Instances)
            {
                FillWithParentAssignments(instance, element, stringBuilder, tabCount);
            }

            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
        }

        #endregion

        #region File Locations

        public static FilePath GetGeneratedFileName(ElementSave selectedElement, CodeOutputElementSettings elementSettings,
            CodeOutputProjectSettings codeOutputProjectSettings, string forcedElementName = null, VisualApi? visualApi = null)
        {
            var elementName = forcedElementName ?? selectedElement.Name;
            var effectiveVisualApi = visualApi ?? CodeGenerator.GetVisualApiForElement(selectedElement);

            string generatedFileName = elementSettings.GeneratedFileName;

            if (string.IsNullOrEmpty(generatedFileName) && !string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
            {
                string prefix = selectedElement is ScreenSave ? "Screens"
                    : selectedElement is ComponentSave ? "Components"
                    : "Standards";
                var splitName = (prefix + "/" + elementName).Split('/');
                var nameWithNamespaceArray = splitName.Take(splitName.Length - 1).Append(CodeGenerator.GetClassNameForType(elementName, effectiveVisualApi));

                var folder = codeOutputProjectSettings.CodeProjectRoot;
                if (FileManager.IsRelative(folder))
                {
                    folder = GumState.Self.ProjectState.ProjectDirectory + folder;
                }

                generatedFileName = folder + string.Join("\\", nameWithNamespaceArray) + ".Generated.cs";
            }

            if (!string.IsNullOrEmpty(generatedFileName) && FileManager.IsRelative(generatedFileName))
            {
                generatedFileName = ProjectState.Self.ProjectDirectory + generatedFileName;
            }

            return generatedFileName;
        }

        public static FilePath GetCustomCodeFileName(ElementSave selectedElement, CodeOutputElementSettings elementSettings,
            CodeOutputProjectSettings codeOutputProjectSettings, string forcedElementName = null, VisualApi? visualApi = null)
        {
            var generatedFileName = GetGeneratedFileName(selectedElement, elementSettings, codeOutputProjectSettings, forcedElementName, visualApi);
            var fullPath = generatedFileName.FullPath;
            var customCodeFileName = fullPath.Substring(0, fullPath.Length - ".Generated.cs".Length) + ".cs";
            return customCodeFileName;
        }

        #endregion

        #region Constructor

        private static void GenerateConstructor(ElementSave element, VisualApi visualApi, int tabCount, StringBuilder stringBuilder, CodeOutputProjectSettings projectSettings)
        {
            var elementName = GetClassNameForType(element.Name, visualApi);

            if (visualApi == VisualApi.Gum)
            {
                #region Constructor Header

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {elementName}(bool fullInstantiation = true)");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                #endregion

                #region Gum-required constructor code

                stringBuilder.AppendLine(ToTabs(tabCount) + "if(fullInstantiation)");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                if (element.BaseType == "Container")
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "this.SetContainedObject(new InvisibleRenderable());");
                }

                stringBuilder.AppendLine();
                #endregion
            }
            else // xamarin forms
            {
                #region Constructor Header
                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {elementName}(bool fullInstantiation = true)");

                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                #endregion

                #region XamarinForms-required constructor code

                stringBuilder.AppendLine(ToTabs(tabCount) + "var wasSuspended = GraphicalUiElement.IsAllLayoutSuspended;");
                stringBuilder.AppendLine(ToTabs(tabCount) + "GraphicalUiElement.IsAllLayoutSuspended = true;");

                var elementBaseType = element?.BaseType;
                var baseElements = ObjectFinder.Self.GetBaseElements(element);

                var isThisAbsoluteLayout = elementBaseType?.EndsWith("/AbsoluteLayout") == true;
                if (!isThisAbsoluteLayout)
                {
                    isThisAbsoluteLayout = baseElements.Any(item => item.BaseType?.EndsWith("/AbsoluteLayout") == true);
                }


                var isStackLayout = elementBaseType?.EndsWith("/StackLayout") == true;
                if (!isStackLayout)
                {
                    isStackLayout = baseElements.Any(item => item.BaseType?.EndsWith("/StackLayout") == true);
                }

                var isSkiaCanvasView = elementBaseType?.EndsWith("/SkiaGumCanvasView") == true;
                if (!isSkiaCanvasView)
                {
                    // see if this inherits from a skia gum canvas view
                    isSkiaCanvasView = baseElements.Any(item => item.BaseType?.EndsWith("/SkiaGumCanvasView") == true);
                }

                if (isThisAbsoluteLayout)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "var MainLayout = this;");
                }
                else if (!isSkiaCanvasView && !isStackLayout)
                {
                    bool shouldAddMainLayout = GetIfShouldAddMainLayout(element, projectSettings);

                    if (shouldAddMainLayout)
                    {
                        stringBuilder.AppendLine(ToTabs(tabCount) + "MainLayout = new AbsoluteLayout();");
                        stringBuilder.AppendLine(ToTabs(tabCount) + "BaseGrid.Children.Add(MainLayout);");
                    }
                }
                #endregion
            }

            CodeGenerationContext context = new CodeGenerationContext();
            context.Instance = null;
            context.Element = element;
            context.TabCount = tabCount;
            context.CodeOutputProjectSettings = projectSettings;
            FillWithVariableAssignments(visualApi, stringBuilder, context);

            stringBuilder.AppendLine();

            if (!DoesElementInheritFromCodeGeneratedElement(element, projectSettings))
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + "InitializeInstances();");

                if(context.CodeOutputProjectSettings.GenerateGumDataTypes)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + "AssignGumReferences();");
                }
            }

            stringBuilder.AppendLine();



            // fill with variable binding after the instances have been created
            if (visualApi == VisualApi.XamarinForms)
            {
                FillWithVariableBinding(element, stringBuilder, tabCount);
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + "if(fullInstantiation)");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            stringBuilder.AppendLine(ToTabs(tabCount) + "ApplyDefaultVariables();");
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");

            if (!DoesElementInheritFromCodeGeneratedElement(element, projectSettings))
            {
                // AssignParents doesn't call base so that the derived can control the ultimate order.
                // However, it overrides and the base calls AssignParents. Therefore, no need for us to
                // call it here, I don't think...
                stringBuilder.AppendLine(ToTabs(tabCount) + "AssignParents();");
            }

            stringBuilder.AppendLine(ToTabs(tabCount) + "CustomInitialize();");

            if (visualApi == VisualApi.Gum)
            {
                // close the if check
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + "GraphicalUiElement.IsAllLayoutSuspended = wasSuspended;");

            }


            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
        }

        private static bool GetIfShouldAddMainLayout(ElementSave element, CodeOutputProjectSettings projectSettings)
        {
            var shouldAddMainLayout = true;
            if (element is ScreenSave && !string.IsNullOrEmpty(element.BaseType) && !projectSettings.BaseTypesNotCodeGenerated.Contains(element.BaseType))
            {
                shouldAddMainLayout = false;
            }

            return shouldAddMainLayout;
        }

        #endregion

        public static string GetGeneratedCodeForElement(ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
        {
            #region Initial Values

            AdjustPixelValuesForDensity = projectSettings.AdjustPixelValuesForDensity;
            VisualApi visualApi = GetVisualApiForElement(element);

            var stringBuilder = new StringBuilder();
            int tabCount = 0;

            #endregion

            #region Using Statements

            if (!string.IsNullOrWhiteSpace(projectSettings?.CommonUsingStatements))
            {
                stringBuilder.AppendLine(projectSettings.CommonUsingStatements);
            }

            if (!string.IsNullOrEmpty(elementSettings?.UsingStatements))
            {
                stringBuilder.AppendLine(elementSettings.UsingStatements);
            }
            #endregion

            #region Namespace Header/Opening {

            string namespaceName = GetElementNamespace(element, elementSettings, projectSettings);

            if (!string.IsNullOrEmpty(namespaceName))
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"namespace {namespaceName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
            }

            #endregion

            #region Class Header/Opening {

            const string access = "public";

            stringBuilder.AppendLine(ToTabs(tabCount) + $"{access} partial class {GetClassNameForType(element.Name, visualApi)}");
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;
            #endregion


            var context = new CodeGenerationContext();
            context.Element = element;
            context.TabCount = tabCount;
            context.CodeOutputProjectSettings = projectSettings;

            #region States

            FillWithStateEnums(element, stringBuilder, tabCount);
            FillWithStateProperties(element, stringBuilder, tabCount, projectSettings);

            #endregion

            #region Instances

            foreach (var instance in element.Instances.Where(item => item.DefinedByBase == false))
            {
                FillWithInstanceDeclaration(instance, element, stringBuilder, tabCount);
            }

            AddAbsoluteLayoutIfNecessary(element, tabCount, stringBuilder, projectSettings);

            stringBuilder.AppendLine();

            #endregion

            if(projectSettings.GenerateGumDataTypes)
            {
                GenerateGumSaveObjects(context, stringBuilder);
            }

            FillWithExposedVariables(element, stringBuilder, visualApi, tabCount);
            // -- no need for AppendLine here since FillWithExposedVariables does it after every variable --

            GenerateConstructor(element, visualApi, tabCount, stringBuilder, projectSettings);

            GenerateInitializeInstancesMethod(element, visualApi, tabCount, stringBuilder, projectSettings);

            GenerateAddToParentsMethod(element, visualApi, tabCount, stringBuilder, projectSettings);

            GenerateApplyDefaultVariables(context, stringBuilder);

            if (projectSettings.GenerateGumDataTypes)
            {
                GenerateAssignGumReferences(element, visualApi, tabCount, stringBuilder);
            }

            GenerateApplyLocalizationMethod(element, tabCount, stringBuilder);

            stringBuilder.AppendLine(ToTabs(tabCount) + "partial void CustomInitialize();");

            #region Class Closing }
            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            #endregion

            if (!string.IsNullOrEmpty(namespaceName))
            {
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }

            return stringBuilder.ToString();
        }

        public static void GenerateCodeForElement(ElementSave selectedElement, Models.CodeOutputElementSettings elementSettings, CodeOutputProjectSettings codeOutputProjectSettings, bool showPopups)
        {
            var generatedFileName = CodeGenerator.GetGeneratedFileName(selectedElement, elementSettings, codeOutputProjectSettings);

            ////////////////////////////////////////Early Out/////////////////////////////
            if (generatedFileName == null)
            {
                if (showPopups)
                {
                    GumCommands.Self.GuiCommands.ShowMessage("Generated file name must be set first");
                }
                return;
            }
            //////////////////////////////////////End Early Out//////////////////////////

            // We used to use the view model code, but the viewmodel may have
            // an instance within the element selected. Instead, we want to output
            // the code for the whole selected element.
            //var contents = ViewModel.Code;

            string contents = CodeGenerator.GetGeneratedCodeForElement(selectedElement, elementSettings, codeOutputProjectSettings);
            contents = $"//Code for {selectedElement.ToString()}\n{contents}";

            string message = string.Empty;

            var codeDirectory = generatedFileName.GetDirectoryContainingThis();
            if (!System.IO.Directory.Exists(codeDirectory.FullPath))
            {
                try
                {
                    GumCommands.Self.TryMultipleTimes(() =>
                        System.IO.Directory.CreateDirectory(codeDirectory.FullPath));
                }
                catch (Exception e)
                {
                    GumCommands.Self.GuiCommands.PrintOutput($"Error creating directory {codeDirectory}:\n{e.Message}");
                }
            }

            GumCommands.Self.TryMultipleTimes(() => System.IO.File.WriteAllText(generatedFileName.FullPath, contents));

            // show a message somewhere?
            message += $"Generated code to {FileManager.RemovePath(generatedFileName.FullPath)}";

            if (!string.IsNullOrEmpty(codeOutputProjectSettings.CodeProjectRoot))
            {

                // nope! This strips out periods in folders. We don't want to do that:
                //var splitFileWithoutGenerated = generatedFileName.Split('.').ToArray();
                //var customCodeFileName = string.Join("\\", splitFileWithoutGenerated.Take(splitFileWithoutGenerated.Length - 2)) + ".cs";
                // Instead, just strip it off the end:
                var fullPath = generatedFileName.FullPath;
                var customCodeFileName = fullPath.Substring(0, fullPath.Length - ".Generated.cs".Length) + ".cs";

                // todo - only save this if it doesn't already exist
                if (!System.IO.File.Exists(customCodeFileName))
                {
                    var directory = FileManager.GetDirectory(customCodeFileName);
                    if (!System.IO.Directory.Exists(directory))
                    {
                        System.IO.Directory.CreateDirectory(directory);
                    }
                    var customCodeContents = CustomCodeGenerator.GetCustomCodeForElement(selectedElement, elementSettings, codeOutputProjectSettings);
                    System.IO.File.WriteAllText(customCodeFileName, customCodeContents);
                }
            }


            if (showPopups)
            {
                GumCommands.Self.GuiCommands.ShowMessage(message);
            }
            else
            {
                GumCommands.Self.GuiCommands.PrintOutput(message);
            }

        }

        private static void GenerateApplyDefaultVariables(CodeGenerationContext context, StringBuilder stringBuilder)
        {
            var line = "private void ApplyDefaultVariables()";
            stringBuilder.AppendLine(context.Tabs + line);
            stringBuilder.AppendLine(context.Tabs + "{");
            context.TabCount++;

            foreach (var instance in context.Element.Instances)
            {
                context.Instance = instance;

                FillWithNonParentVariableAssignments(context, stringBuilder);

                TryGenerateApplyLocalizationForInstance(context, stringBuilder, instance);

                var instanceApi = GetVisualApiForInstance(instance, context.Element);
                var screenOrComponent = context.Element is ScreenSave
                    ? "ScreenSave"
                    : "ComponentSave";
                if(instanceApi == VisualApi.Gum && context.CodeOutputProjectSettings.GenerateGumDataTypes)
                {
                    stringBuilder.AppendLine(context.Tabs + $"if({screenOrComponent}?.DefaultState != null);");
                    context.TabCount++;
                    stringBuilder.AppendLine(context.Tabs + $"GumRuntime.ElementSaveExtensions.ApplyVariableReferences({instance.Name}, {screenOrComponent}.DefaultState);");
                    context.TabCount--;

                }

                stringBuilder.AppendLine();
            }

            context.TabCount--;
            stringBuilder.AppendLine(context.Tabs + "}");

        }

        private static void GenerateAssignGumReferences(ElementSave element, VisualApi visualApi, int tabCount, StringBuilder stringBuilder)
        {
            var line = "private void AssignGumReferences()";

            stringBuilder.AppendLine(ToTabs(tabCount) + line);
            stringBuilder.AppendLine(ToTabs(tabCount) + "{");
            tabCount++;

            stringBuilder.AppendLine(ToTabs(tabCount) + "var gumProjectSave = ObjectFinder.Self.GumProjectSave;");

            stringBuilder.AppendLine(ToTabs(tabCount) + "//////////////Early Out/////////////");
            stringBuilder.AppendLine(ToTabs(tabCount) + "if (gumProjectSave == null) return;");
            stringBuilder.AppendLine(ToTabs(tabCount) + "////////////End Early Out///////////");

            string screenOrComponent = "UNKNOWN";
            if(element is ScreenSave)
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"ScreenSave = gumProjectSave.Screens.Find(item => item.Name == \"{element.Name}\");");
                screenOrComponent = "ScreenSave";
            }
            else if(element is ComponentSave)
            {
                stringBuilder.AppendLine(ToTabs(tabCount) + $"ComponentSave = gumProjectSave.Components.Find(item => item.Name == \"{element.Name}\");");
                screenOrComponent = "ComponentSave";
            }

            foreach(var instance in element.Instances)
            {
                var instanceVisualApi = GetVisualApiForInstance(instance, element);
                if(instanceVisualApi == VisualApi.Gum)
                {
                    // todo - will need Forms too, but we'll do this for now:
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"{instance.Name}.Tag = {screenOrComponent}.Instances.Find(item => item.Name == nameof({instance.Name}));");
                }
            }

            tabCount--;
            stringBuilder.AppendLine(ToTabs(tabCount) + "}");
        }

        public static string GetElementNamespace(ElementSave element, CodeOutputElementSettings elementSettings, CodeOutputProjectSettings projectSettings)
        {
            var namespaceName = elementSettings?.Namespace;

            if (string.IsNullOrEmpty(namespaceName) && !string.IsNullOrWhiteSpace(projectSettings.RootNamespace))
            {
                namespaceName = projectSettings.RootNamespace;
                if (element is ScreenSave)
                {
                    namespaceName += ".Screens";
                }
                else if (element is ComponentSave)
                {
                    namespaceName += ".Components";
                }
                else // standard element
                {
                    namespaceName += ".Standards";
                }

                var splitElementName = element.Name.Split('\\').ToArray();
                var splitPrefix = splitElementName.Take(splitElementName.Length - 1).ToArray();
                var whatToAppend = string.Join(".", splitPrefix);
                if (!string.IsNullOrEmpty(whatToAppend))
                {
                    namespaceName += "." + whatToAppend;
                }
            }

            return namespaceName;
        }

        public static VisualApi GetVisualApiForElement(ElementSave element)
        {
            VisualApi visualApi;
            //if(element is ScreenSave)
            //{
            // screens are always XamarinForms
            //visualApi = VisualApi.XamarinForms;
            // Update August 23, 2022
            // No, the code gen may be 
            // be used for entirely Skia
            // pages such as PDF generation.
            // Therefore, we should always look
            // to the IsXamarinFormsControl value:
            //visualApi = VisualApi.XamarinForms;
            //}
            //else
            {
                var defaultState = element.DefaultState;
                var rvf = new RecursiveVariableFinder(defaultState);
                var isXamForms = rvf.GetValue<bool>("IsXamarinFormsControl");
                if (isXamForms == true)
                {
                    visualApi = VisualApi.XamarinForms;
                }
                else
                {
                    visualApi = VisualApi.Gum;
                }
            }
            return visualApi;

        }

        static bool IsStackLayout(InstanceSave instance) => IsOfXamarinFormsType(instance, "StackLayout");

        static bool IsOfXamarinFormsType(InstanceSave instance, string xamarinFormsType)
        {

            var element = ObjectFinder.Self.GetElementSave(instance);

            bool isRightType = element.Name.EndsWith("/" + xamarinFormsType);
            if (!isRightType)
            {
                var elementBaseType = element?.BaseType;

                isRightType = elementBaseType?.EndsWith("/" + xamarinFormsType) == true;
            }

            if (!isRightType)
            {
                var baseElements = ObjectFinder.Self.GetBaseElements(element);
                isRightType = baseElements.Any(item => item.BaseType?.EndsWith("/" + xamarinFormsType) == true);
            }

            return isRightType;

        }

        private static void AddAbsoluteLayoutIfNecessary(ElementSave element, int tabCount, StringBuilder stringBuilder, CodeOutputProjectSettings projectSettings)
        {
            var elementBaseType = element?.BaseType;
            var isThisAbsoluteLayout = elementBaseType?.EndsWith("/AbsoluteLayout") == true;
            var isThisStackLayout = elementBaseType?.EndsWith("/StackLayout") == true;

            var isSkiaCanvasView = elementBaseType?.EndsWith("/SkiaGumCanvasView") == true;

            var isContainer = elementBaseType == "Container";

            if (!isThisAbsoluteLayout && !isSkiaCanvasView && !isContainer && !isThisStackLayout && projectSettings.OutputLibrary == OutputLibrary.XamarinForms)
            {
                var shouldAddMainLayout =
                    GetIfShouldAddMainLayout(element, projectSettings);


                if (shouldAddMainLayout)
                {
                    stringBuilder.Append(ToTabs(tabCount) + "protected AbsoluteLayout MainLayout{get; private set;}");
                }
            }
        }


        private static void TryGenerateApplyLocalizationForInstance(CodeGenerationContext context, StringBuilder stringBuilder, InstanceSave instance)
        {
            var component = ObjectFinder.Self.GetComponent(instance);

            if (component != null)
            {
                var instanceComponentSettings = CodeOutputElementSettingsManager.LoadOrCreateSettingsFor(component);

                if (instanceComponentSettings?.LocalizeElement == true)
                {
                    stringBuilder.AppendLine(context.Tabs + $"{instance.Name}.ApplyLocalization();");

                }
            }
        }

        private static void GenerateApplyLocalizationMethod(ElementSave element, int tabCount, StringBuilder stringBuilder)
        {
            if (LocalizationManager.HasDatabase)
            {
                // Vic says - we may want this to be recursive eventually, but that introduces
                // some complexity. How do we know which views have a call available? 
                var line = "public void ApplyLocalization()";
                stringBuilder.AppendLine(ToTabs(tabCount) + line);
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                var context = new CodeGenerationContext();
                context.TabCount = tabCount;
                context.Element = element;
                foreach (var variable in element.DefaultState.Variables)
                {
                    InstanceSave instance = null;
                    if (!string.IsNullOrEmpty(variable.SourceObject))
                    {
                        instance = element.GetInstance(variable.SourceObject);
                    }

                    context.Instance = instance;
                    if (instance != null)
                    {
                        if (GetIsShouldBeLocalized(variable, context.Element.DefaultState))
                        {
                            string assignment = GetLocaliedLine(instance, variable, context);
                            stringBuilder.AppendLine(ToTabs(tabCount) + assignment);
                        }
                        //else if(!string.IsNullOrEmpty(instance.BaseType))
                        //{
                        //    var instanceBase = ObjectFinder.Self.GetElementSave(instance.BaseType);

                        //    var isComponent = instanceBase is ComponentSave;

                        //    var shouldCallLocalize = !isComponent;

                        //    if(shouldCallLocalize)
                        //    {
                        //        stringBuilder.AppendLine(ToTabs(tabCount) + $"{instance.Name}.ApplyLocalization();");
                        //    }
                        //}
                    }

                    // if a component is a subcomponent which can be localized, call it:

                }
                // Why don't we call base.ApplyLocalization?
                //stringBuilder.AppendLine(ToTabs(tabCount) + "base.ApplyLocalization();");

                foreach (var instance in element.Instances)
                {
                    context.Instance = instance;

                    TryGenerateApplyLocalizationForInstance(context, stringBuilder, instance);
                }


                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
        }

        private static void FillWithVariableBinding(ElementSave element, StringBuilder stringBuilder, int tabCount)
        {
            var boundVariables = new List<VariableSave>();

            foreach (var variable in element.DefaultState.Variables)
            {
                if (!string.IsNullOrEmpty(variable.ExposedAsName))
                {
                    var instanceName = variable.SourceObject;
                    // make sure this instance is a XamForms object otherwise we don't need to set the binding
                    var isXamForms = (element.DefaultState.GetValueRecursive($"{instanceName}.IsXamarinFormsControl") as bool?) ?? false;

                    if (isXamForms)
                    {
                        var instance = element.GetInstance(instanceName);
                        var instanceType = GetClassNameForType(instance.BaseType, VisualApi.XamarinForms);
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"{instanceName}.SetBinding({instanceType}.{variable.GetRootName()}Property, nameof({variable.ExposedAsName}));");
                    }
                }

            }
        }

        public static string GetCodeForState(ElementSave container, StateSave stateSave, VisualApi visualApi)
        {
            var stringBuilder = new StringBuilder();

            var context = new CodeGenerationContext();
            context.Element = container;

            FillWithVariablesInState(stateSave, stringBuilder, 0, context);

            var code = stringBuilder.ToString();
            return code;
        }

        private static void FillWithVariablesInState(StateSave stateSave, StringBuilder stringBuilder, int tabCount, CodeGenerationContext context)
        {
            VariableSave[] variablesToConsider = GetVariablesToAssignOnState(stateSave);

            var variableGroups = variablesToConsider.GroupBy(item => item.SourceObject);

            foreach (var group in variableGroups)
            {
                InstanceSave instance = null;
                var instanceName = group.Key;

                if (instanceName != null)
                {
                    instance = context.Element.GetInstance(instanceName);
                }
                context.Instance = instance;

                #region Determine visual API (Gum or Forms)

                VisualApi visualApi = VisualApi.Gum;

                var defaultState = context.Element.DefaultState;
                bool? isXamForms = false;
                if (instance == null)
                {
                    isXamForms = defaultState.GetValueRecursive($"IsXamarinFormsControl") as bool?;
                }
                else
                {
                    isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
                }
                if (isXamForms == true)
                {
                    visualApi = VisualApi.XamarinForms;
                }

                #endregion

                ElementSave baseElement = null;
                if (instance == null)
                {
                    baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(context.Element.BaseType) ?? context.Element;
                }
                else
                {
                    baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance?.BaseType);
                }

                // could be null if the element references an element that doesn't exist.
                if (baseElement != null)
                {
                    var baseDefaultState = baseElement?.DefaultState;
                    RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);


                    List<VariableSave> variablesForThisInstance = group
                        .Where(item => GetIfVariableShouldBeIncludedForInstance(instance, item, baseRecursiveVariableFinder))
                        .ToList();


                    ProcessVariableGroups(variablesForThisInstance, stateSave, visualApi, stringBuilder, context);

                    // Now that they've been processed, we can process the remainder regularly
                    foreach (var variable in variablesForThisInstance)
                    {
                        var codeLine = GetCodeLine(variable, context.Element, visualApi, stateSave, context);
                        stringBuilder.AppendLine(ToTabs(tabCount) + codeLine);
                        var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                        if (!string.IsNullOrEmpty(suffixCodeLine))
                        {
                            stringBuilder.AppendLine(ToTabs(tabCount) + suffixCodeLine);
                        }
                    }
                }

            }
        }

        private static VariableSave[] GetVariablesToAssignOnState(StateSave stateSave)
        {
            VariableSave[] variablesToConsider = stateSave.Variables
                // make "Parent" first
                .Where(item => item.GetRootName() != "Parent")
                .ToArray();
            return variablesToConsider;
        }

        #region States

        private static void FillWithStateEnums(ElementSave element, StringBuilder stringBuilder, int tabCount)
        {
            // for now we'll just do categories. We may need to get uncategorized at some point...
            foreach (var category in element.Categories)
            {
                string enumName = category.Name;

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public enum {category.Name}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;

                foreach (var state in category.States)
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"{state.Name},");
                }

                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
        }

        private static void FillWithStateProperties(ElementSave element, StringBuilder stringBuilder, int tabCount, CodeOutputProjectSettings codeProjectSettings)
        {
            var isXamarinForms = GetVisualApiForElement(element) == VisualApi.XamarinForms;
            var containerClassName = GetClassNameForType(element.Name, GetVisualApiForElement(element));
            foreach (var category in element.Categories)
            {
                // If it's Xamarin Forms we want to have the states be bindable

                stringBuilder.AppendLine();

                // Enum types need to be nullable because there could be no category set:
                string enumName = category.Name + "?";

                if (isXamarinForms)
                {

                    stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {category.Name}StateProperty = " +
                        $"BindableProperty.Create(nameof({category.Name}State),typeof({enumName}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay, propertyChanged:Handle{category.Name}StatePropertyChanged);");

                    stringBuilder.AppendLine(ToTabs(tabCount) + $"public {enumName} {category.Name}State");
                    stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                    tabCount++;
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({enumName})GetValue({category.Name}StateProperty);");
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({category.Name}StateProperty, value);");
                    tabCount--;
                    stringBuilder.AppendLine(ToTabs(tabCount) + "}");

                    stringBuilder.AppendLine(ToTabs(tabCount) + $"private static void Handle{category.Name}StatePropertyChanged(BindableObject bindable, object oldValue, object newValue)");
                    stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                    tabCount++;
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"var casted = bindable as {containerClassName};");
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"var value = ({enumName})newValue;");
                    CodeGenerationContext context = new CodeGenerationContext();
                    context.Element = element;
                    context.ThisPrefix = "casted";
                    context.TabCount = tabCount;
                    context.CodeOutputProjectSettings = codeProjectSettings;

                    AddAssignFromElement(context, stringBuilder);

                    stringBuilder.AppendLine(context.Tabs + "if(!appliedDynamically)");
                    stringBuilder.AppendLine(context.Tabs + "{");
                    context.TabCount++;

                    CreateStateVariableAssignmentSwitch(stringBuilder, category, context);

                    context.TabCount--;
                    stringBuilder.AppendLine(context.Tabs + "}");

                    // We may need to invalidate surfaces here if any objects that have variables assigned are skia canvases
                    // Update November 29, 2022
                    // Currently we brute-force it by calling InvalidateSurface on all objects and their EffectiveManagers.
                    // Could this be expensive? I think that this just flips a flag, and it will happen so fast that actual
                    // redraws only occur 1 time. But if it's slow, we could hashset which manages have been invalidated and
                    // make sure each one is only invalidated one time.
                    foreach (var item in element.Instances)
                    {
                        if (item.BaseType.EndsWith("/SkiaSharpCanvasView"))
                        {
                            stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{item.Name}.InvalidateSurface();");
                        }
                        else if(GetVisualApiForInstance(item, element) == VisualApi.Gum)
                        {
                            stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{item.Name}.EffectiveManagers?.InvalidateSurface();");
                        }
                    }
                    if (element.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
                    {
                        stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.InvalidateSurface();");
                    }

                    tabCount--;
                    stringBuilder.AppendLine(ToTabs(tabCount) + "}");

                }
                else
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"{category.Name} m{category.Name}State;");


                    stringBuilder.AppendLine(ToTabs(tabCount) + $"public {category.Name} {category.Name}State");

                    stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                    tabCount++;
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"get => m{category.Name}State;");
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"set");

                    stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                    tabCount++;
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"m{category.Name}State = value;");
                    CodeGenerationContext context = new CodeGenerationContext();
                    context.Element = element;
                    context.TabCount = tabCount;
                    context.CodeOutputProjectSettings = codeProjectSettings;

                    AddAssignFromElement(context, stringBuilder);

                    stringBuilder.AppendLine(context.Tabs + "if(!appliedDynamically)");
                    stringBuilder.AppendLine(context.Tabs + "{");
                    context.TabCount++;

                    CreateStateVariableAssignmentSwitch(stringBuilder, category, context);

                    context.TabCount--;
                    stringBuilder.AppendLine(context.Tabs + "}");


                    context.TabCount--;
                    stringBuilder.AppendLine(ToTabs(context.TabCount) + "}");

                    context.TabCount--;
                    stringBuilder.AppendLine(ToTabs(context.TabCount) + "}");
                }

            }
        }

        private static void AddAssignFromElement(CodeGenerationContext context, StringBuilder stringBuilder)
        {

            stringBuilder.AppendLine(context.Tabs + "var appliedDynamically = false;");

            if(context.CodeOutputProjectSettings.GenerateGumDataTypes && 
                // todo - do we care about this? Maybe eventually?
                context.VisualApi == VisualApi.Gum)
            {
                stringBuilder.AppendLine(context.Tabs + "if (BindableGraphicalUiElement.ShouldApplyDynamicStates)");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;

                var elementPropertyName = "ElementSave";
                if(context.VisualApi == VisualApi.XamarinForms)
                {
                    elementPropertyName =
                        context.Element is ScreenSave ? "casted.ScreenSave"
                        : context.Element is ComponentSave ? "casted.ComponentSave"
                        : null;
                }


                stringBuilder.AppendLine(context.Tabs + "var qualifiedValueName = value.ToString();");
                stringBuilder.AppendLine(context.Tabs + "var name = qualifiedValueName;");
                stringBuilder.AppendLine(context.Tabs + "if (qualifiedValueName.Contains(\".\"))");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;
                stringBuilder.AppendLine(context.Tabs + "var lastIndex = qualifiedValueName.LastIndexOf('.');");
                stringBuilder.AppendLine(context.Tabs + "name = qualifiedValueName.Substring(lastIndex + 1);");
                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");
                stringBuilder.AppendLine(context.Tabs + "var foundState = ElementSave?.GetStateSaveRecursively(name);");

                //stringBuilder.AppendLine(context.Tabs + $"var foundState = {elementPropertyName}?.GetStateSaveRecursively(value.ToString());");
                stringBuilder.AppendLine(context.Tabs + "if (foundState != null)");
                stringBuilder.AppendLine(context.Tabs + "{");
                context.TabCount++;
                if(context.VisualApi == VisualApi.XamarinForms)
                {
                    stringBuilder.AppendLine(context.Tabs + "// Need to apply the variables in the state");
                }
                else
                {
                    stringBuilder.AppendLine(context.Tabs + "this.ApplyState(foundState);");
                }
                stringBuilder.AppendLine(context.Tabs + "appliedDynamically = true;");
                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");
                context.TabCount--;
                stringBuilder.AppendLine(context.Tabs + "}");
            }
        }

        #endregion

        private static void GenerateGumSaveObjects(CodeGenerationContext context, StringBuilder stringBuilder)
        {
            var element = context.Element;
            if(element is ScreenSave)
            {
                stringBuilder.AppendLine(context.Tabs + "global::Gum.DataTypes.ScreenSave ScreenSave { get; set; }");
            }
            else if(element is ComponentSave)
            {
                if(context.VisualApi == VisualApi.XamarinForms)
                {
                    stringBuilder.AppendLine(context.Tabs + "global::Gum.DataTypes.ComponentSave ComponentSave { get; set; }");
                }
                else
                {
                    stringBuilder.AppendLine(context.Tabs + "global::Gum.DataTypes.ComponentSave ComponentSave");
                    stringBuilder.AppendLine(context.Tabs + "{");
                    context.TabCount++;
                    stringBuilder.AppendLine(context.Tabs + "get => ElementSave as global::Gum.DataTypes.ComponentSave;");
                    stringBuilder.AppendLine(context.Tabs + "set => ElementSave = value;");
                    context.TabCount--;
                    stringBuilder.AppendLine(context.Tabs + "}");
                }
            }
        }

        private static void CreateStateVariableAssignmentSwitch(StringBuilder stringBuilder, StateSaveCategory category, CodeGenerationContext context)
        {
            stringBuilder.AppendLine(ToTabs(context.TabCount) + $"switch (value)");
            stringBuilder.AppendLine(ToTabs(context.TabCount) + "{");
            context.TabCount++;

            foreach (var state in category.States)
            {
                stringBuilder.AppendLine(ToTabs(context.TabCount) + $"case {category.Name}.{state.Name}:");
                context.TabCount++;

                FillWithVariablesInState(state, stringBuilder, context.TabCount, context);

                stringBuilder.AppendLine(ToTabs(context.TabCount) + $"break;");
                context.TabCount--;
            }

            context.TabCount--;
            stringBuilder.AppendLine(ToTabs(context.TabCount) + "}");
        }

        private static void FillWithExposedVariables(ElementSave element, StringBuilder stringBuilder, VisualApi visualApi, int tabCount)
        {
            var exposedVariables = element.DefaultState.Variables
                .Where(item => !string.IsNullOrEmpty(item.ExposedAsName))
                .ToArray();

            foreach (var exposedVariable in exposedVariables)
            {
                // 
                FillWithExposedVariable(exposedVariable, element, stringBuilder, tabCount);
                stringBuilder.AppendLine();
            }
        }

        private static void FillWithExposedVariable(VariableSave exposedVariable, ElementSave container, StringBuilder stringBuilder, int tabCount)
        {

            // if both the container and the instance are xamarin forms objects, then we can try to do some bubble-up binding
            var instanceName = exposedVariable.SourceObject;
            var foundInstance = container.GetInstance(instanceName);
            ///////////////Early Out//////////////////////
            if(foundInstance == null)
            {
                return;
            }
            //////////////End Early Out///////////////////
            
            var bindingBehavior = GetBindingBehavior(container, instanceName);
            var type = exposedVariable.Type;

            if (exposedVariable.IsState(container, out ElementSave stateContainer, out StateSaveCategory category))
            {

                string stateContainerType;
                VisualApi visualApi = GetVisualApiForElement(stateContainer);
                stateContainerType = GetClassNameForType(stateContainer.Name, visualApi);
                type = $"{stateContainerType}.{category.Name}";
            }

            if (bindingBehavior == BindingBehavior.BindablePropertyWithBoundInstance)
            {
                var containerClassName = GetClassNameForType(container.Name, VisualApi.XamarinForms);
                stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {exposedVariable.ExposedAsName}Property = " +
                    $"BindableProperty.Create(nameof({exposedVariable.ExposedAsName}),typeof({type}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay);");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({type})GetValue({exposedVariable.ExposedAsName.Replace(" ", "")}Property);");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({exposedVariable.ExposedAsName.Replace(" ", "")}Property, value);");
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else if (bindingBehavior == BindingBehavior.BindablePropertyWithEventAssignment)
            {
                var rcv = new RecursiveVariableFinder(container.DefaultState);
                var defaultValue = rcv.GetValue(exposedVariable.Name);
                var defaultValueAsString = VariableValueToGumCodeValue(exposedVariable, container, forcedValue: defaultValue);
                var containerClassName = GetClassNameForType(container.Name, VisualApi.XamarinForms);

                string defaultAssignmentWithComma = null;

                if (!string.IsNullOrEmpty(defaultValueAsString))
                {
                    defaultAssignmentWithComma = $", defaultValue:{defaultValueAsString}";
                }

                stringBuilder.AppendLine($"{ToTabs(tabCount)}public static readonly BindableProperty {exposedVariable.ExposedAsName}Property = " +
                    $"BindableProperty.Create(nameof({exposedVariable.ExposedAsName}),typeof({type}),typeof({containerClassName}), defaultBindingMode: BindingMode.TwoWay, propertyChanged:Handle{exposedVariable.ExposedAsName}PropertyChanged{defaultAssignmentWithComma});");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => ({type})GetValue({exposedVariable.ExposedAsName.Replace(" ", "")}Property);");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => SetValue({exposedVariable.ExposedAsName.Replace(" ", "")}Property, value);");
                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");

                stringBuilder.AppendLine(ToTabs(tabCount) + $"private static void Handle{exposedVariable.ExposedAsName}PropertyChanged(BindableObject bindable, object oldValue, object newValue)");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"var casted = bindable as {containerClassName};");

                if (!string.IsNullOrWhiteSpace(exposedVariable.SourceObject))
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{exposedVariable.SourceObject}.{exposedVariable.GetRootName()} = ({type})newValue;");
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{exposedVariable.SourceObject}?.EffectiveManagers?.InvalidateSurface();");
                }
                else
                {
                    stringBuilder.AppendLine(ToTabs(tabCount) + $"casted.{exposedVariable.Name.Replace(" ", "")} = ({type})newValue;");
                }

                tabCount--;
                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
            else
            {

                stringBuilder.AppendLine(ToTabs(tabCount) + $"public {type} {exposedVariable.ExposedAsName}");
                stringBuilder.AppendLine(ToTabs(tabCount) + "{");
                tabCount++;
                stringBuilder.AppendLine(ToTabs(tabCount) + $"get => {exposedVariable.Name.Replace(" ", "")};");
                stringBuilder.AppendLine(ToTabs(tabCount) + $"set => {exposedVariable.Name.Replace(" ", "")} = value;");
                tabCount--;

                stringBuilder.AppendLine(ToTabs(tabCount) + "}");
            }
        }

        enum BindingBehavior
        {
            NoBinding,
            BindablePropertyWithEventAssignment,
            BindablePropertyWithBoundInstance
        }

        private static BindingBehavior GetBindingBehavior(ElementSave container, string instanceName)
        {
            var isContainerXamarinForms = (container.DefaultState.GetValueRecursive("IsXamarinFormsControl") as bool?) ?? false;
            var isInstanceXamarinForms = (container.DefaultState.GetValueRecursive($"{instanceName}.IsXamarinFormsControl") as bool?) ?? false;

            if (isContainerXamarinForms && isInstanceXamarinForms)
            {
                return BindingBehavior.BindablePropertyWithBoundInstance;
            }
            else if (isContainerXamarinForms) // container xamforms, child is SkiaGum
            {
                return BindingBehavior.BindablePropertyWithEventAssignment;
            }
            else
            {
                return BindingBehavior.NoBinding;
            }
        }

        public static string GetCodeForInstance(InstanceSave instance, ElementSave element, CodeOutputProjectSettings codeOutputProjectSettings)
        {
            var stringBuilder = new StringBuilder();

            FillWithInstanceDeclaration(instance, element, stringBuilder);

            FillWithInstanceInstantiation(instance, element, stringBuilder);

            var context = new CodeGenerationContext();
            context.Instance = instance;
            context.Element = element;
            context.CodeOutputProjectSettings = codeOutputProjectSettings;

            FillWithNonParentVariableAssignments(context, stringBuilder);

            FillWithParentAssignments(instance, element, stringBuilder);

            var code = stringBuilder.ToString();
            return code;
        }



        private static void FillWithInstanceInstantiation(InstanceSave instance, ElementSave element, StringBuilder stringBuilder, int tabCount = 0)
        {
            var strippedType = instance.BaseType;
            if (strippedType.Contains("/"))
            {
                strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
            }
            var tabs = new String(' ', 4 * tabCount);

            var visualApi = GetVisualApiForInstance(instance, element);

            stringBuilder.AppendLine($"{tabs}{instance.Name} = new {GetClassNameForType(instance.BaseType, visualApi)}();");

            var shouldSetBinding =
                visualApi == VisualApi.XamarinForms && element.DefaultState.Variables.Any(item => !string.IsNullOrEmpty(item.ExposedAsName) && item.SourceObject == instance.Name);
            // If it's xamarin forms and we have exposed variables, then let's set up binding to this
            if (shouldSetBinding)
            {
                stringBuilder.AppendLine($"{tabs}{instance.Name}.BindingContext = this;");
            }

            if (visualApi == VisualApi.Gum)
            {
                stringBuilder.AppendLine($"{tabs}{instance.Name}.Name = \"{instance.Name}\";");
            }
            else
            {
                // If defined by base, then the automation ID will already be set there, and 
                // Xamarin.Forms doesn't like an automation ID being set 2x
                if (instance.DefinedByBase == false)
                {
                    stringBuilder.AppendLine($"{tabs}{instance.Name}.AutomationId = \"{instance.Name}\";");
                }
            }
        }

        public static VisualApi GetVisualApiForInstance(InstanceSave instance, ElementSave element)
        {
            var defaultState = element.DefaultState;
            var isXamForms = (defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?) ?? false;
            var visualApi = VisualApi.Gum;
            if (isXamForms)
            {
                visualApi = VisualApi.XamarinForms;
            }

            return visualApi;
        }

        private static void FillWithVariableAssignments(VisualApi visualApi, StringBuilder stringBuilder, CodeGenerationContext context)
        {
            var element = context.Element;

            #region Get variables to consider
            var defaultState = element.DefaultState;

            var baseElement = ObjectFinder.Self.GetElementSave(element.BaseType);
            RecursiveVariableFinder recursiveVariableFinder = null;

            // This is null if it's a screen, or there's some bad reference
            if (baseElement != null)
            {
                recursiveVariableFinder = new RecursiveVariableFinder(baseElement.DefaultState);
            }

            var variablesToConsider = defaultState.Variables
                .Where(item =>
                {
                    var shouldInclude =
                        item.Value != null &&
                        item.SetsValue &&
                        string.IsNullOrEmpty(item.SourceObject);

                    if (shouldInclude)
                    {
                        if (recursiveVariableFinder != null)
                        {
                            // We want to make sure that the variable is defined in the base object. If it isn't, then
                            // it could be a leftover variable caused by having this object be of one type, using a variable
                            // specific to that type, then changing it to another type. Gum holds on to these varibles in case
                            // the type change was accidental, but it means we have to watch for these orphan variables when generating.
                            var foundVariable = recursiveVariableFinder.GetVariable(item.Name);
                            shouldInclude = foundVariable != null;
                        }
                        else
                        {
                            if (item.Name.EndsWith("State"))
                            {
                                var type = item.Type.Substring(item.Type.Length - 5);
                                var hasCategory = element.GetStateSaveCategoryRecursively(type) != null;

                                if (!hasCategory)
                                {
                                    shouldInclude = false;
                                }
                            }
                        }

                    }

                    return shouldInclude;
                })
                .ToList();

            #endregion

            var tabs = new String(' ', 4 * context.TabCount);

            ProcessVariableGroups(variablesToConsider, defaultState, visualApi, stringBuilder, context);

            foreach (var variable in variablesToConsider)
            {
                var codeLine = GetCodeLine(variable, element, visualApi, defaultState, context);
                stringBuilder.AppendLine(tabs + codeLine);

                var suffixCodeLine = GetSuffixCodeLine(null, variable, visualApi);
                if (!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(tabs + suffixCodeLine);
                }
            }
        }


        private static void FillWithNonParentVariableAssignments(CodeGenerationContext context, StringBuilder stringBuilder)
        {
            #region Get variables to consider

            var variablesToAssignValues = GetVariablesForValueAssignmentCode(context.Instance, context.Element)
                .Where(item => item.GetRootName() != "Parent")
                .ToList();

            #endregion

            FillWithVariableAssignments(context, stringBuilder, variablesToAssignValues);

            var variableListsToAssign = context.Element.DefaultState.VariableLists.Where(item => item.SourceObject == context.Instance.Name)
                .ToArray();

            FillWithVariableListAssignments(context, stringBuilder, variableListsToAssign);
        }

        private static void FillWithVariableListAssignments(CodeGenerationContext context, StringBuilder stringBuilder, VariableListSave[] variableListsToAssign)
        {
            VisualApi visualApi = GetVisualApiForInstance(context.Instance, context.Element);

            foreach (var variableList in variableListsToAssign)
            {
                var codeLine = GetCodeLine(variableList, context, visualApi);


                // the line of code could be " ", a string with a space. This happens
                // if we want to skip a variable so we dont return null or empty.
                // But we also don't want a ton of spaces generated.
                if (!string.IsNullOrWhiteSpace(codeLine))
                {
                    stringBuilder.AppendLine(codeLine);
                }
            }
        }

        #region Variable Assignments


        private static string GetCodeLine(VariableSave variable, ElementSave container, VisualApi visualApi, StateSave state, CodeGenerationContext context)
        {
            if (visualApi == VisualApi.Gum)
            {
                var fullLineReplacement = TryGetFullGumLineReplacement(context.Instance, variable, context);

                if (fullLineReplacement != null)
                {
                    return fullLineReplacement;
                }
                else
                {
                    var variableName = GetGumVariableName(variable, container);



                    return $"{context.CodePrefixNoTabs}.{variableName} = {VariableValueToGumCodeValue(variable, container)};";
                }

            }
            else // xamarin forms
            {
                var fullLineReplacement = TryGetFullXamarinFormsLineReplacement(context.Instance, container, variable, state, context);
                if (fullLineReplacement != null)
                {
                    return fullLineReplacement;
                }
                else
                {
                    return $"{context.CodePrefixNoTabs}.{GetXamarinFormsVariableName(variable)} = {VariableValueToXamarinFormsCodeValue(variable, container)};";
                }

            }
        }

        private static string GetCodeLine(VariableListSave variable, CodeGenerationContext context, VisualApi visualApi)
        {
            // for now we actually don't do anything with this - I used to think we would, but the variable lists are part of the Gum save objects, not rutnime.

            return "";
            if (visualApi == VisualApi.Gum)
            {
                //var fullLineReplacement = TryGetFullGumLineReplacement(context.Instance, variable, context);

                //if (fullLineReplacement != null)
                //{
                //    return fullLineReplacement;
                //}
                //else
                {
                    //var variableName = GetGumVariableName(variable, container);
                    var variableName = variable.GetRootName();


                    return $"{context.CodePrefix}.{variableName} = {VariableValueToGumCodeValue(variable, context.Element)};";
                }

            }
            else // xamarin forms
            {
                //var fullLineReplacement = TryGetFullXamarinFormsLineReplacement(context.Instance, container, variable, state, context);
                //if (fullLineReplacement != null)
                //{
                //    return fullLineReplacement;
                //}
                //else
                {
                    //var variableName = GetXamarinFormsVariableName(variable);
                    var variableName = variable.Name;

                    return $"{context.CodePrefix}.{variableName} = {VariableValueToXamarinFormsCodeValue(variable, context.Element)};";
                }

            }
        }

        private static string VariableValueToGumCodeValue(VariableSave variable, ElementSave container, object forcedValue = null)
        {
            var value = forcedValue ?? variable.Value;
            var rootName = variable.GetRootName();
            var isState = variable.IsState(container, out ElementSave categoryContainer, out StateSaveCategory category);

            return VariableValueToGumCode(value, rootName, isState, categoryContainer, category);
        }

        private static string VariableValueToGumCodeValue(VariableListSave variable, ElementSave container, object forcedValue = null)
        {
            var value = forcedValue ?? variable.ValueAsIList;
            var rootName = variable.GetRootName();
            var isState = false;

            return VariableValueToGumCode(value, rootName, isState, null, null);
        }

        private static string VariableValueToGumCode(object value, string rootName, bool isState, ElementSave categoryContainer, StateSaveCategory category)
        {
            if (value is float asFloat)
            {
                return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
            }
            else if (value is string asString)
            {
                if (rootName == "Parent")
                {
                    return asString;
                }
                else if (isState)
                {
                    if (categoryContainer != null && category != null)
                    {
                        string containerClassName = "VariableState";
                        if (categoryContainer != null)
                        {
                            containerClassName = GetClassNameForType(categoryContainer.Name, VisualApi.Gum);
                        }
                        return $"{containerClassName}.{category.Name}.{asString}";
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    return "\"" + asString.Replace("\n", "\\n") + "\"";
                }
            }
            else if (value is bool)
            {
                return value.ToString().ToLowerInvariant();
            }
            else if (value?.GetType().IsEnum == true)
            {
                var type = value.GetType();
                if (type == typeof(PositionUnitType))
                {
                    var converted = UnitConverter.ConvertToGeneralUnit(value);
                    return $"GeneralUnitType.{converted}";
                }
                else
                {
                    return value.GetType().Name + "." + value.ToString();
                }
            }
            else
            {
                return value?.ToString();
            }
        }

        private static string VariableValueToXamarinFormsCodeValue(object value, string rootName, bool isState, ElementSave categoryContainer, StateSaveCategory category)
        {
            if (value is float asFloat)
            {
                // X and Y go to PixelX and PixelY
                if (rootName == "X" || rootName == "Y")
                {
                    return asFloat.ToString(CultureInfo.InvariantCulture) + "f";
                }
                else if (rootName == "CornerRadius")
                {
                    if (AdjustPixelValuesForDensity)
                    {
                        return $"(int)({asFloat.ToString(CultureInfo.InvariantCulture)} / Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density)";
                    }
                    else
                    {
                        return $"(int){asFloat.ToString(CultureInfo.InvariantCulture)}";
                    }
                }
                else
                {
                    if (AdjustPixelValuesForDensity)
                    {
                        return $"{asFloat.ToString(CultureInfo.InvariantCulture)} / Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density";
                    }
                    else
                    {
                        return asFloat.ToString(CultureInfo.InvariantCulture);
                    }
                }
            }
            else if (value is int asInt)
            {
                if (rootName == "FontSize")
                {
                    if(AdjustPixelValuesForDensity)
                    {
                        return $"(int)({asInt} / Xamarin.Essentials.DeviceDisplay.MainDisplayInfo.Density)";
                    }
                    else
                    {
                        return asInt.ToString();
                    }
                }
            }
            else if (value is string asString)
            {
                if (rootName == "Parent")
                {
                    return value.ToString();
                }
                else if(rootName == "Font")
                {
                    return $"CustomFont.{value.ToString()?.Replace(" ", "_")}";
                }
                else if (isState)
                {
                    var containerClassName = GetClassNameForType(categoryContainer.Name, VisualApi.XamarinForms);
                    if (category == null)
                    {
                        return $"{containerClassName}.VariableState.{value}";

                    }
                    else
                    {
                        return $"{containerClassName}.{category.Name}.{value}";
                    }
                }
                else
                {
                    return "\"" + asString.Replace("\n", "\\n") + "\"";
                }
            }
            else if (value is bool)
            {
                return value.ToString().ToLowerInvariant();
            }
            else if (value.GetType().IsEnum)
            {
                var type = value.GetType();
                if (type == typeof(PositionUnitType))
                {
                    var converted = UnitConverter.ConvertToGeneralUnit(value);
                    return $"GeneralUnitType.{converted}";
                }
                else if (type == typeof(HorizontalAlignment))
                {
                    switch ((HorizontalAlignment)value)
                    {
                        case HorizontalAlignment.Left:
                            return "Xamarin.Forms.TextAlignment.Start";
                        case HorizontalAlignment.Center:
                            return "Xamarin.Forms.TextAlignment.Center";
                        case HorizontalAlignment.Right:
                            return "Xamarin.Forms.TextAlignment.End";
                        default:
                            return "";
                    }
                }
                else if (type == typeof(VerticalAlignment))
                {
                    switch ((VerticalAlignment)value)
                    {
                        case VerticalAlignment.Top:
                            return "Xamarin.Forms.TextAlignment.Start";
                        case VerticalAlignment.Center:
                            return "Xamarin.Forms.TextAlignment.Center";
                        case VerticalAlignment.Bottom:
                            return "Xamarin.Forms.TextAlignment.End";
                        default:
                            return "";
                    }
                }
                else
                {
                    return value.GetType().Name + "." + value.ToString();
                }
            }

            return value?.ToString();
        }


        private static void FillWithVariableAssignments(CodeGenerationContext context, StringBuilder stringBuilder, List<VariableSave> variablesToAssignValues)
        {
            var defaultState = context.Element.DefaultState;
            VisualApi visualApi = GetVisualApiForInstance(context.Instance, context.Element);

            var tabs = new String(' ', 4 * context.TabCount);

            var container = context.Element;
            var instance = context.Instance;

            // We used to do this, but now spacing is supported in FRB:
            //if (visualApi == VisualApi.XamarinForms && instance.BaseType?.EndsWith("/StackLayout") == true)
            //{
            //    stringBuilder.AppendLine($"{tabs}{instance.Name}.Spacing = 0;");
            //}

            // States come before anything, so run those first
            foreach (var variable in variablesToAssignValues.Where(item => item.IsState(container)))
            {
                var codeLine = GetCodeLine(variable, container, visualApi, defaultState, context);

                // the line of code could be " ", a string with a space. This happens
                // if we want to skip a variable so we dont return null or empty.
                // But we also don't want a ton of spaces generated.
                if (!string.IsNullOrWhiteSpace(codeLine))
                {
                    stringBuilder.AppendLine(codeLine);
                }

                var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                if (!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(suffixCodeLine);
                }
            }
            variablesToAssignValues.RemoveAll(item => item.IsState(container));

            // sometimes variables have to be processed in groups. For example, RGB values
            // have to be assigned all at once in a Color value in XamForms;
            ProcessVariableGroups(variablesToAssignValues, container.DefaultState, visualApi, stringBuilder, context);

            foreach (var variable in variablesToAssignValues)
            {
                var innerVisualApi = GetVisualApiForVariable(variable, context) ?? visualApi;
                var codeLine = GetCodeLine(variable, container, innerVisualApi, defaultState, context);

                // the line of code could be " ", a string with a space. This happens
                // if we want to skip a variable so we dont return null or empty.
                // But we also don't want a ton of spaces generated.
                if (!string.IsNullOrWhiteSpace(codeLine))
                {
                    stringBuilder.AppendLine(codeLine);
                }

                var suffixCodeLine = GetSuffixCodeLine(instance, variable, visualApi);
                if (!string.IsNullOrEmpty(suffixCodeLine))
                {
                    stringBuilder.AppendLine(tabs + suffixCodeLine);
                }
            }
        }

        private static VisualApi? GetVisualApiForVariable(VariableSave variable, CodeGenerationContext context)
        {
            if(context.Instance != null)
            {
                // If this element is ignored in codegen, then we don't go inside to find the variable:
                var isIgnoredInCodeGen = context.CodeOutputProjectSettings?.BaseTypesNotCodeGenerated?.Contains(context.Instance.BaseType) == true;

                if(!isIgnoredInCodeGen)
                {
                    var instanceElement = ObjectFinder.Self.GetElementSave(context.Instance);


                    var variableRoot = variable.GetRootName();

                    var matchingExposed = instanceElement?.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == variableRoot);
                    if (matchingExposed != null)
                    {
                        var instanceInInstanceElement = instanceElement.GetInstance(matchingExposed.SourceObject);

                        if(instanceInInstanceElement != null)
                        {
                            return GetVisualApiForInstance(instanceInInstanceElement, instanceElement);
                        }
                    }
                }
            }
            return null;
        }

        #endregion

        private static void ProcessVariableGroups(List<VariableSave> variablesToConsider, StateSave defaultState, VisualApi visualApi, StringBuilder stringBuilder, CodeGenerationContext context)
        {
            if (visualApi == VisualApi.XamarinForms)
            {
                string baseType = null;
                if (context.Instance != null)
                {
                    var standardElement = ObjectFinder.Self.GetRootStandardElementSave(context.Instance);
                    baseType = standardElement?.Name;
                }
                else
                {
                    baseType = context.Element.BaseType;
                }
                switch (baseType)
                {
                    case "Text":
                        ProcessColorForLabel(variablesToConsider, defaultState, context.Instance, stringBuilder, context);
                        ProcessXamarinFormsPositionAndSize(variablesToConsider, defaultState, context.Instance, context.Element, stringBuilder, context);
                        ProcessXamarinFormsLabelBold(variablesToConsider, defaultState, context.Instance, context.Element, stringBuilder, context);
                        break;
                    default:
                        ProcessXamarinFormsPositionAndSize(variablesToConsider, defaultState, context.Instance, context.Element, stringBuilder, context);
                        break;
                }

                // January 31, 2022
                // Does it matter if
                // we do this on all objects?
                // Does it cause performance issues?
                // Update - not all controls support this, so we need to check:
                if (context.Instance != null)
                {
                    var isOfRightType =
                        IsOfXamarinFormsType(context.Instance, "StackLayout") ||
                        IsOfXamarinFormsType(context.Instance, "AbsoluteLayout") ||
                        IsOfXamarinFormsType(context.Instance, "Frame");
                    ;

                    if (isOfRightType)
                    {
                        var rfv = new RecursiveVariableFinder(defaultState);

                        var clips = rfv.GetValue<bool>(context.GumVariablePrefix + "ClipsChildren");

                        stringBuilder.AppendLine($"{context.CodePrefix}.IsClippedToBounds = {clips.ToString().ToLowerInvariant()};");
                    }
                }
            }
        }

        private static void ProcessColorForLabel(List<VariableSave> variablesToConsider, StateSave defaultState, InstanceSave instance, StringBuilder stringBuilder, CodeGenerationContext context)
        {
            var rfv = new RecursiveVariableFinder(defaultState);

            var gumPrefix = context.GumVariablePrefix;

            var red = rfv.GetValue<int>(gumPrefix + "Red");
            var green = rfv.GetValue<int>(gumPrefix + "Green");
            var blue = rfv.GetValue<int>(gumPrefix + "Blue");
            var alpha = rfv.GetValue<int>(gumPrefix + "Alpha");

            var isExplicitlySet = variablesToConsider.Any(item =>
                item.Name == gumPrefix + "Red" ||
                item.Name == gumPrefix + "Green" ||
                item.Name == gumPrefix + "Blue" ||
                item.Name == gumPrefix + "Alpha");

            if (isExplicitlySet)
            {
                variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Red");
                variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Green");
                variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Blue");
                variablesToConsider.RemoveAll(item => item.Name == gumPrefix + "Alpha");

                stringBuilder.AppendLine($"{context.CodePrefix}.TextColor = Color.FromRgba({red}, {green}, {blue}, {alpha});");
            }
        }


        private static void ProcessXamarinFormsLabelBold(List<VariableSave> variablesToConsider, StateSave state, InstanceSave instance, ElementSave container, StringBuilder stringBuilder, CodeGenerationContext context)
        {
            var boldName = context.GumVariablePrefix + "IsBold";

            var isBold = state.GetValueOrDefault<bool>(boldName);

            variablesToConsider.RemoveAll(item => item.Name == boldName);

            if (isBold)
            {
                stringBuilder.AppendLine($"{context.CodePrefix}.FontAttributes = Xamarin.Forms.FontAttributes.Bold;");

            }

        }

        private static bool GetIfStateSetsAnyPositionValues(StateSave state, string prefix, List<VariableSave> variablesToConsider)
        {
            return variablesToConsider.Any(item =>
                    item.Name == prefix + "X" ||
                    item.Name == prefix + "Y" ||
                    item.Name == prefix + "Width" ||
                    item.Name == prefix + "Height" ||

                    item.Name == prefix + "X Units" ||
                    item.Name == prefix + "Y Units" ||
                    item.Name == prefix + "Width Units" ||
                    item.Name == prefix + "Height Units" ||
                    item.Name == prefix + "X Origin" ||
                    item.Name == prefix + "Y Origin"

                    );
        }
        private static void FillWithInstanceDeclaration(InstanceSave instance, ElementSave container, StringBuilder stringBuilder, int tabCount = 0)
        {
            VisualApi visualApi = VisualApi.Gum;

            var defaultState = container.DefaultState;
            var isXamForms = defaultState.GetValueRecursive($"{instance.Name}.IsXamarinFormsControl") as bool?;
            if (isXamForms == true)
            {
                visualApi = VisualApi.XamarinForms;
            }

            var tabs = new String(' ', 4 * tabCount);

            string className = GetClassNameForType(instance.BaseType, visualApi);

            bool isPublic = true;
            string accessString = isPublic ? "public " : "";

            var isOverride = (defaultState.GetValueRecursive($"{instance.Name}.IsOverrideInCodeGen") as bool?) ?? false;
            if (isOverride)
            {
                accessString += "override ";
            }

            // If this is private, it cannot override anything. Therefore, we'll mark the setter as protected:
            //stringBuilder.AppendLine($"{tabs}{accessString}{className} {instance.Name} {{ get; private set; }}");
            stringBuilder.AppendLine($"{tabs}{accessString}{className} {instance.Name} {{ get; protected set; }}");
        }

        public static string GetClassNameForType(string gumType, VisualApi visualApi)
        {
            string className = null;
            var specialHandledCase = false;

            if (visualApi == VisualApi.XamarinForms)
            {
                switch (gumType)
                {
                    case "Text":
                        className = "Label";
                        specialHandledCase = true;
                        break;
                }
            }

            if (!specialHandledCase)
            {

                var strippedType = gumType;
                if (strippedType.Contains("/"))
                {
                    strippedType = strippedType.Substring(strippedType.LastIndexOf("/") + 1);
                }

                string suffix = visualApi == VisualApi.Gum ? "Runtime" : "";
                className = $"{strippedType}{suffix}";

            }
            return className;
        }

        private static string GetSuffixCodeLine(InstanceSave instance, VariableSave variable, VisualApi visualApi)
        {
            if (visualApi == VisualApi.XamarinForms)
            {
                var rootName = variable.GetRootName();

                //switch(rootName)
                //{
                // We don't do this anymore now that we are stuffing forms objects in absolute layouts
                //case "Width": return $"{instance.Name}.HorizontalOptions = LayoutOptions.Start;";
                //case "Height": return $"{instance.Name}.VerticalOptions = LayoutOptions.Start;";
                //}
            }

            return null;
        }

        public static string StringIdPrefix = "T_";
        public static string FormattedLocalizationCode = "Strings.Get(\"{0}\")";

        private static string TryGetFullXamarinFormsLineReplacement(InstanceSave instance, ElementSave container, VariableSave variable, StateSave state, CodeGenerationContext context)
        {
            var rootVariableName = variable.GetRootName();

            #region Handle all variables that have no direct translation in Xamarin forms

            if (
                rootVariableName == "Clips Children" ||
                rootVariableName == "ExposeChildrenEvents" ||
                rootVariableName == "FlipHorizontal" ||
                rootVariableName == "HasEvents" ||

                rootVariableName == "IsXamarinFormsControl" ||
                rootVariableName == "IsOverrideInCodeGen" ||
                rootVariableName == "Name" ||
                rootVariableName == "Wraps Children" ||
                rootVariableName == "X Origin" ||
                rootVariableName == "XOrigin" ||
                rootVariableName == "Y Origin" ||
                rootVariableName == "YOrigin"
                )
            {
                return " "; // Don't do anything with these variables::
            }

            #endregion

            #region Parent

            else if (rootVariableName == "Parent")
            {
                var parentName = variable.Value as string;

                var hasContent = false;
                var parentInstance = container.GetInstance(parentName);

                if (parentInstance != null)
                {
                    // traverse the inheritance chain - we don't want to go to the very base because 
                    // Glue has base types like Container for all components, and that's not what we want.
                    // Actually we should go one above the inheritance:

                    var instanceElement = ObjectFinder.Self.GetElementSave(parentInstance?.BaseType);

                    if (instanceElement != null)
                    {
                        var baseElements = ObjectFinder.Self.GetBaseElements(instanceElement);
                        string componentType = null;
                        if (baseElements.Count > 1)
                        {
                            // don't do the "Last" because that will be container, so take all but the last:
                            var baseBeforeContainer = baseElements.Take(baseElements.Count - 1).LastOrDefault();
                            componentType = baseBeforeContainer?.Name;
                        }
                        else if (baseElements.Count == 1)
                        {
                            // this inherits from Container, so just use it's own base type:
                            componentType = instanceElement.Name;
                        }
                        else
                        {
                            // All XamForms objects are components, so all must inherit from something. This should never happen...
                        }
                        hasContent = DoesTypeHaveContent(componentType);
                    }

                    // Certain types of views don't support Children.Add - they only have
                    // a single content. In the future we may want to formalize the way we
                    // handle standard XamarinForms controls, but for now we'll hardcode some
                    // checks:
                    if (hasContent)
                    {
                        return $"{parentName}.Content = {context.Instance.Name};";
                    }
                    else
                    {
                        return $"{parentName}.Children.Add({context.Instance.Name});";
                    }
                }
                // parent instance is null, so attach to "this" top level object
                else
                {
                    // Couldn't find anything, so don't return anything

                }

            }

            #endregion

            #region Font

            else if(rootVariableName == "Font")
            {
                return $"{context.CodePrefixNoTabs}.SetFont(CustomFont.{variable.Value?.ToString()?.Replace(" ", "_")});";
            }

            #endregion


            #region Children Layout

            else if (rootVariableName == "Children Layout" && variable.Value is ChildrenLayout valueAsChildrenLayout)
            {
                if (instance != null && instance?.BaseType.EndsWith("/StackLayout") == true)
                {
                    if (valueAsChildrenLayout == ChildrenLayout.LeftToRightStack)
                    {
                        return $"{context.CodePrefix}.Orientation = StackOrientation.Horizontal;";
                    }
                    else
                    {
                        return $"{context.CodePrefix}.Orientation = StackOrientation.Vertical;";
                    }
                }
                else if (instance == null && container.BaseType.EndsWith("/StackLayout"))
                {
                    if (valueAsChildrenLayout == ChildrenLayout.LeftToRightStack)
                    {
                        return $"{context.CodePrefix}.Orientation = StackOrientation.Horizontal;";
                    }
                    else
                    {
                        return $"{context.CodePrefix}.Orientation = StackOrientation.Vertical;";
                    }
                }
                else if (valueAsChildrenLayout != ChildrenLayout.Regular)
                {
                    var message = $"Error: The object {instance?.Name ?? container.Name} cannot have a layout of {valueAsChildrenLayout}.";

                    if (instance != null && instance.BaseType?.EndsWith("/SkiaGumCanvasView") == true)
                    {
                        message += $"\nTo stack objects in a Skia canvas, add a Container which has its ChildrenLayout set to {valueAsChildrenLayout}";
                    }
                    else
                    {
                        message += $"\nIt should probably inherit from StackLayout to be a top-to-bottom stack";
                    }

                    return message;
                }
                else
                {
                    // it's regular, so we just ignore it
                    return string.Empty;
                }
            }

            #endregion
            else if (GetIsShouldBeLocalized(variable, context.Element.DefaultState))
            {
                string assignment = GetLocaliedLine(instance, variable, context);

                return assignment;
            }

            return null;
        }

        public static bool DoesTypeHaveContent(string type)
        {
            return type?.EndsWith("/ScrollView") == true ||
                            type?.EndsWith("/StickyScrollView") == true ||
                            type?.EndsWith("/Frame") == true;
        }

        private static string GetLocaliedLine(InstanceSave instance, VariableSave variable, CodeGenerationContext context)
        {
            var valueAsString = variable.Value as string;
            var formattedStringIdAssignment = string.Format(FormattedLocalizationCode, valueAsString);
            var assignment = $"{context.CodePrefix}.{variable.GetRootName()} = {formattedStringIdAssignment};";
            return assignment;
        }

        private static bool GetIsShouldBeLocalized(VariableSave variable, StateSave defaultState)
        {
            var toReturn = LocalizationManager.HasDatabase &&
                // This could be exposed of exposed, so the name wouldn't be "Text"
                //variable.GetRootName() == "Text" && 
                variable.Value is string valueAsString &&
                valueAsString?.StartsWith(StringIdPrefix) == true &&
                // This could be a leftover variable
                ObjectFinder.Self.IsVariableOrphaned(variable, defaultState) == false;

            return toReturn;
        }

        private static string TryGetFullGumLineReplacement(InstanceSave instance, VariableSave variable, CodeGenerationContext context)
        {
            var rootName = variable.GetRootName();
            #region Parent

            if (rootName == "Parent")
            {
                var owner = string.IsNullOrEmpty(variable.Value as string)
                    ? "this"
                    : variable.Value;
                return $"{owner}.Children.Add({instance.Name});";
            }
            #endregion
            // ignored variables:
            else if (rootName == "IsXamarinFormsControl" ||
                rootName == "ExposeChildrenEvents" ||
                rootName == "IsOverrideInCodeGen" ||
                rootName == "HasEvents")
            {
                return " ";
            }
            else if (GetIsShouldBeLocalized(variable, context.Element.DefaultState))
            {
                string assignment = GetLocaliedLine(instance, variable, context);

                return assignment;
            }

            return null;
        }



        private static string VariableValueToXamarinFormsCodeValue(VariableSave variable, ElementSave container)
        {
            var value = variable.Value;
            var rootName = variable.GetRootName();
            var isState = variable.IsState(container, out ElementSave categoryContainer, out StateSaveCategory category);
            return VariableValueToXamarinFormsCodeValue(value, rootName, isState, categoryContainer, category);
        }

        private static string VariableValueToXamarinFormsCodeValue(VariableListSave variable, ElementSave container)
        {
            var value = variable.ValueAsIList;
            var rootName = variable.GetRootName();
            var isState = false;
            return VariableValueToXamarinFormsCodeValue(value, rootName, isState, null, null);
        }


        private static object GetGumVariableName(VariableSave variable, ElementSave container)
        {
            if (variable.IsState(container))
            {
                return variable.GetRootName().Replace(" ", "");
            }
            else
            {
                return variable.GetRootName().Replace(" ", "");
            }
        }

        private static string GetXamarinFormsVariableName(VariableSave variable)
        {
            var rootName = variable.GetRootName();

            switch (rootName)
            {
                case "Height": return "HeightRequest";
                case "Width": return "WidthRequest";
                case "X": return "PixelX";
                case "Y": return "PixelY";
                case "Visible": return "IsVisible";
                case "HorizontalAlignment": return "HorizontalTextAlignment";
                case "VerticalAlignment": return "VerticalTextAlignment";
                case "StackSpacing": return "Spacing";

                default: return rootName;
            }
        }

        private static VariableSave[] GetVariablesForValueAssignmentCode(InstanceSave instance, ElementSave currentElement)
        {
            var baseElement = Gum.Managers.ObjectFinder.Self.GetElementSave(instance.BaseType);
            if (baseElement == null)
            {
                // this could happen if the project references an object that has a missing type. Tolerate it, return an empty l ist
                return new VariableSave[0];
            }
            else
            {
                var baseDefaultState = baseElement?.DefaultState;
                RecursiveVariableFinder baseRecursiveVariableFinder = new RecursiveVariableFinder(baseDefaultState);

                var defaultState = currentElement.DefaultState;
                var variablesToConsider = defaultState.Variables
                    .Where(item =>
                    {
                        return GetIfVariableShouldBeIncludedForInstance(instance, item, baseRecursiveVariableFinder);
                    })
                    .ToArray();
                return variablesToConsider;
            }
        }

        private static bool GetIfVariableShouldBeIncludedForInstance(InstanceSave instance, VariableSave item, RecursiveVariableFinder baseRecursiveVariableFinder)
        {
            var shouldInclude =
                                    item.Value != null &&
                                    item.SetsValue &&
                                    item.SourceObject == instance?.Name;

            if (shouldInclude)
            {
                var foundVariable = baseRecursiveVariableFinder.GetVariable(item.GetRootName());
                shouldInclude = foundVariable != null;
            }

            return shouldInclude;
        }

        #region Utilities

        private static string ToTabs(int tabCount) => new string(' ', tabCount * 4);

        #endregion

    }
}
