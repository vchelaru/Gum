using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.RenderingLibrary;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Matrix = System.Numerics.Matrix4x4;
using Vector2 = System.Numerics.Vector2;


namespace Gum.Managers;

#region Enums

public enum TextureAddress
{
    EntireTexture,
    Custom,
    DimensionsBased
}

public enum ChildrenLayout
{
    Regular,
    TopToBottomStack,
    LeftToRightStack,
    AutoGridHorizontal,
    AutoGridVertical

}

#endregion

public class StandardElementsManager
{
    #region Enums

    public enum DimensionVariableAction
    {
        ExcludeFileOptions,
        AllowFileOptions,
        DefaultToPercentageOfFile
    }

    #endregion

    #region Fields

    static StateSave? arcState;
    static StateSave? filledCircleState;
    static StateSave? lineState;
    static StateSave? roundedRectangleState;
    static StateSave? canvasState;
    static StateSave? svgState;
    static StateSave? lottieAnimationState;

    public const string ScreenBoundsName = "<SCREEN BOUNDS>";

    Dictionary<string, StateSave> mDefaults;

    static StandardElementsManager mSelf;

    // Standard types kept in mDefaults so legacy projects that already contain them still load
    // with correct default variable values, but which are no longer seeded into newly created
    // projects. The v3 Rectangle carries the full fill/stroke/gradient/dropshadow surface, making
    // ColoredRectangle redundant for new work (#2965 phase 2).
    readonly HashSet<string> _deprecatedStandardTypeNames;

    #endregion

    #region Constructor

    public StandardElementsManager()
    {
        _deprecatedStandardTypeNames = new HashSet<string> { "ColoredRectangle" };
    }

    #endregion

    #region Properties

    public IEnumerable<string> DefaultTypes
    {
        get
        {
            foreach (var kvp in mDefaults)
            {
                yield return kvp.Key;
            }
        }
    }

    /// <summary>
    /// The standard element type names that should be seeded into newly created projects and
    /// auto-added to existing projects on load. Excludes "Screen" (created on demand, never a
    /// standard element) and deprecated types retained only for backward-compatible loading.
    /// </summary>
    public IEnumerable<string> SeedableStandardTypes =>
        mDefaults.Keys.Where(name => name != "Screen" && !_deprecatedStandardTypeNames.Contains(name));

    public static StandardElementsManager Self
    {
        get
        {
            if (mSelf == null)
            {
                mSelf = new StandardElementsManager();
            }
            return mSelf;
        }
    }

    public string DefaultType => "Container";

    public Dictionary<string, StateSave> DefaultStates => mDefaults;

    #endregion

    bool hasInitialized = false;
    readonly object initLock = new object();

    public void Initialize()
    {
        if (hasInitialized) return;
        lock (initLock)
        {
            if (hasInitialized) return;
            RefreshDefaults();
            hasInitialized = true;
        }
    }

    public void RefreshDefaults()
    {
        mDefaults = new Dictionary<string, StateSave>();

        // Eventually this would get read from somewhere like an XML file
        // or a CSV file, but for
        // now we'll just use hard values.


        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     Text                                                           //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";

            AddPositioningVariables(stateSave, includeBaseline: true);

            AddDimensionsVariables(stateSave, 100, 50, DimensionVariableAction.ExcludeFileOptions);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "Hello", Name = "Text", Category = "Text" });

            // Okay so here's some info on this value.
            // It would be nice to be able to select whether 
            // a Text should or should not be localized in Gum.
            // The problem is that when we do the localization, we
            // only have access to the GraphicalUiElement, not the InstanceSave.
            // Sure, we could get the reference, but what about at runtime in a game?
            // We ultimately want this value to be saved in the GraphicalUiElement so it
            // can be applied at runtime too. This is a pain to do, since it would require
            // changes to SkiaGum and possibly to FRB plugins. It's a bigger project, so until
            // then, we'll leave this out and put it back in when it's time.
            //stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Apply Localization", Category = "Text" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "HorizontalAlignment", Value = HorizontalAlignment.Left, Name = "HorizontalAlignment", Category = "Text" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "VerticalAlignment", Value = VerticalAlignment.Top, Name = "VerticalAlignment", Category = "Text" });

            var maxLettersToShowVariable = new VariableSave
            {
                SetsValue = true,
                Type = "int?",
                Value = null,
                Name = "MaxLettersToShow",
                Category = "Text",
            };

            maxLettersToShowVariable.PropertiesToSetOnDisplayer["NullCheckboxText"] = "All";

            stateSave.Variables.Add(maxLettersToShowVariable);

            var maxNumberOfLinesVariable = new VariableSave { SetsValue = true, Type = "int?", Value = null, Name = "MaxNumberOfLines", Category = "Text" };
            maxNumberOfLinesVariable.PropertiesToSetOnDisplayer["NullCheckboxText"] = "All";
            stateSave.Variables.Add(maxNumberOfLinesVariable);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = nameof(TextOverflowVerticalMode), Value = TextOverflowVerticalMode.SpillOver, Name = nameof(TextOverflowVerticalMode), Category = "Text" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = nameof(TextOverflowHorizontalMode), Value = TextOverflowHorizontalMode.TruncateWord, Name = nameof(TextOverflowHorizontalMode), Category = "Text" });

            var lineHeightMultiplierVariable =
                new VariableSave { SetsValue = true, Type = "float", Value = 1.0f, Name = "LineHeightMultiplier", Category = "Text" };
            // should this go in a plugin?
            lineHeightMultiplierVariable.PropertiesToSetOnDisplayer["LabelDragChangeMultiplier"] = .02m;
            lineHeightMultiplierVariable.PropertiesToSetOnDisplayer["LabelDragValueRounding"] = .01m;
            
            stateSave.Variables.Add(lineHeightMultiplierVariable);

            // font:
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "UseCustomFont", Category = "Font" });

            var fontVariable = new VariableSave { SetsValue = true, Type = "string", Value = "Arial", Name = "Font", IsFont = true, Category = "Font" };
            fontVariable.PropertiesToSetOnDisplayer["IsEditable"] = true;
            stateSave.Variables.Add(fontVariable);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 18, Name = "FontSize", Category = "Font" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "OutlineThickness", Category = "Font" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IsItalic", Category = "Font" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IsBold", Category = "Font" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "UseFontSmoothing", Category = "Font" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "CustomFontFile", Category = "Font", IsFile = true });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 1.0f, Name = "FontScale", Category = "Font" });

            AddRotationVariable(stateSave);

            AddEventVariables(stateSave);

            AddStateVariable(stateSave);

            AddVariableReferenceList(stateSave);

            AddColorVariables(stateSave, includeAlpha: true);

            ApplySortValuesFromOrderInState(stateSave);

            mDefaults.Add("Text", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }




        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     Sprite                                                         //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";
            AddPositioningVariables(stateSave);
            AddDimensionsVariables(stateSave, 100, 100, DimensionVariableAction.DefaultToPercentageOfFile);
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true, Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = null, Name = "RenderTargetTextureSource", Category = "Source", MinimumGumxVersion = V3StandardSurfaceVersion,
                DetailText = "Displays the render target of another Container whose Is Render Target is checked. Takes precedence over SourceFile when set." });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Category = "Animation", Name = "Animate" });

            var currentChainNameVariable = new VariableSave { SetsValue = true, Type = "string", Value = null, Category = "Animation", Name = "CurrentChainName" };
            currentChainNameVariable.PropertiesToSetOnDisplayer["IsEditable"] = true;
            stateSave.Variables.Add(currentChainNameVariable);


            AddRotationVariable(stateSave);
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Category = "Flip and Rotation", Name = "FlipHorizontal" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Category = "Flip and Rotation", Name = "FlipVertical" });


            //stateSave.Variables.Add(new VariableSave { Type = "bool", Value = false, Name = "Custom Texture Coordinates", Category="Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "TextureAddress", Value = Gum.Managers.TextureAddress.EntireTexture, Name = "TextureAddress", Category = "Source" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureLeft", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureTop", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureWidth", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureHeight", Category = "Source" });


            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 1.0f, Name = "TextureWidthScale", Category = "Source",
                DetailText="Multiplies the size of the displayed image. e.g. a value of 2 makes the image show twice as wide"});

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 1.0f, Name = "TextureHeightScale", Category = "Source",
                DetailText = "Multiplies the size of the displayed image. e.g. a value of 2 makes the image show twice as tall"});

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "Wrap", Category = "Source" });

            AddColorVariables(stateSave);
            stateSave.Variables.Add(CreateBlendVariable());

            AddEventVariables(stateSave);

            AddStateVariable(stateSave);

            AddVariableReferenceList(stateSave);

            ApplySortValuesFromOrderInState(stateSave);


            mDefaults.Add("Sprite", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                   Container                                                        //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";


            AddPositioningVariables(stateSave);

            AddDimensionsVariables(stateSave, 150, 150, DimensionVariableAction.ExcludeFileOptions);

            stateSave.Variables.Add(new VariableSave
            {
                SetsValue = true,
                Category = "Children",
                Type = "string",
                Value = null,
                Name = "ContainedType"
            });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Category = "Children", Type = "ChildrenLayout", Value = ChildrenLayout.Regular, Name = "ChildrenLayout" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Category = "Children", Type = "float", Value = 0.0f, Name = "StackSpacing" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Category = "Children", Type = "bool", Value = false, Name = "WrapsChildren" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Category = "Children", Type = "int", Value = 4, Name = "AutoGridHorizontalCells" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Category = "Children", Type = "int", Value = 4, Name = "AutoGridVerticalCells" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IsRenderTarget", Category = "Rendering" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceShaderFile", IsFile = true, Category = "Rendering", MinimumGumxVersion = V3StandardSurfaceVersion,
                DetailText = "A .fx post-process shader applied to this container's contents when it is drawn to the screen." });

            var alphaValue = CreateAlphaVariable();
            stateSave.Variables.Add(alphaValue);


            var blendVariable = CreateBlendVariable();
            stateSave.Variables.Add(blendVariable);


            AddClipsChildren(stateSave);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });

            AddRotationVariable(stateSave);
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Category = "Flip and Rotation", Name = "FlipHorizontal" });

            AddVariableReferenceList(stateSave);

            AddEventVariables(stateSave, defaultHasEvents: true);


            ApplySortValuesFromOrderInState(stateSave);

            mDefaults.Add("Container", stateSave);


            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                               ColoredRectangle                                                     //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";

            AddPositioningVariables(stateSave);

            AddDimensionsVariables(stateSave, 50, 50, DimensionVariableAction.ExcludeFileOptions);

            AddRotationVariable(stateSave);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });

            AddColorVariables(stateSave, true);

            stateSave.Variables.Add(CreateBlendVariable());

            AddEventVariables(stateSave);

            AddStateVariable(stateSave);

            ApplySortValuesFromOrderInState(stateSave);
            
            AddVariableReferenceList(stateSave);

            mDefaults.Add("ColoredRectangle", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }




        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                               Circle                                                               //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";

            AddPositioningVariables(stateSave);

            // Issue #2947 — Circle now sizes via Width/Height like every other visual instead of
            // a one-off Radius variable. The rendered radius is min(Width, Height)/2, so a square
            // box (the default 32x32, matching the old Radius=16 diameter) draws a circle and a
            // non-square box draws an ellipse. Existing Radius values are migrated to
            // Width = Height = Radius * 2 on load (GumProjectSaveExtensionMethods.MigrateCircleRadiusToWidthHeight).
            AddDimensionsVariables(stateSave, 32, 32, DimensionVariableAction.ExcludeFileOptions);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });

            // v3 (#2929 / #2931): AddColorVariables is intentionally NOT called on the plain
            // Circle / Rectangle defaults. The legacy Color / Red / Green / Blue / Alpha route
            // to stroke under #2938 (see SetProperty_Color_RoutesToStroke_NotFill), so
            // surfacing them alongside StrokeRed/Green/Blue/Alpha would be redundant and
            // confusing. The runtime keeps the [Obsolete] aliases so older projects still load.
            // Gradient / dropshadow / blend route through the same SetProperty path as on
            // ColoredCircle, picked up by CircleRuntime's existing pass-through. IsFilled
            // exposure must precede UseGradient so users can scope a gradient to the outline
            // only by flipping IsFilled = false (the fill slot's gradient is gated on _isFilled
            // in SkiaShapeRuntime.RefreshSlotGradients).
            AddFillAndStrokeVariables(stateSave, category: "Rendering", minimumGumxVersion: V3StandardSurfaceVersion);
            // Issue #3009 — Circle drives the gradient start from the active body color, so no
            // standalone Color1 (Red1/Green1/Blue1/Alpha1).
            AddGradientVariables(stateSave, includeStartColor: false, minimumGumxVersion: V3StandardSurfaceVersion);
            AddDropshadowVariables(stateSave, minimumGumxVersion: V3StandardSurfaceVersion);
            AddBlendVariable(stateSave, minimumGumxVersion: V3StandardSurfaceVersion);

            // Although rotating a circle about its center does nothing we add rotation because you can rotate it about a different origin
            AddRotationVariable(stateSave);


            AddEventVariables(stateSave);

            AddStateVariable(stateSave);

            ApplySortValuesFromOrderInState(stateSave);

            AddVariableReferenceList(stateSave);

            mDefaults.Add("Circle", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                               Rectangle                                                            //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";

            AddPositioningVariables(stateSave);

            // 50x50 matches RectangleRuntime's constructor so a Rectangle created in code is the
            // same size as one created in-tool from this standard (reconcile mirror of #2947's
            // Circle 32x32).
            AddDimensionsVariables(stateSave, 50, 50, DimensionVariableAction.ExcludeFileOptions);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });

            // v3 (#2929 / #2931): mirror of the Circle block above — see comment there for why
            // AddColorVariables is intentionally omitted.
            AddFillAndStrokeVariables(stateSave, category: "Rendering", minimumGumxVersion: V3StandardSurfaceVersion);
            // CornerRadius absorbs the legacy RoundedRectangle's rounded-corner surface so that
            // standard can be retired; default 0 keeps the historical hard-cornered visual. Only
            // Rectangle gets this (a Circle has no corners). Gated to v3 in ShapeVariableVersionGate.
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0.0f, Name = "CornerRadius", Category = "Rendering", MinimumGumxVersion = V3StandardSurfaceVersion });
            // Issue #3009 — Rectangle drives the gradient start from the active body color, so no
            // standalone Color1 (Red1/Green1/Blue1/Alpha1).
            AddGradientVariables(stateSave, includeStartColor: false, minimumGumxVersion: V3StandardSurfaceVersion);
            AddDropshadowVariables(stateSave, minimumGumxVersion: V3StandardSurfaceVersion);
            AddBlendVariable(stateSave, minimumGumxVersion: V3StandardSurfaceVersion);

            AddRotationVariable(stateSave);


            AddEventVariables(stateSave);

            AddStateVariable(stateSave);

            ApplySortValuesFromOrderInState(stateSave);

            AddVariableReferenceList(stateSave);

            mDefaults.Add("Rectangle", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     Polygon                                                        //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            var stateSave = new StateSave();
            stateSave.Name = "Default";

            // January 1, 2026
            // Modifying this to
            // include origin variables
            // because they can be set using
            // dock/anchor anyway, so we might
            // as well have them available in the
            // UI
            //AddPositioningVariables(stateSave, addOriginVariables: false);
            AddPositioningVariables(stateSave, addOriginVariables: true);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });
            AddColorVariables(stateSave, true);

            AddRotationVariable(stateSave);

            var pointsVariable = new VariableListSave<Vector2>()
            { Name = "Points", Category = "Points" , Type = "Vector2"};

            pointsVariable.Value.Add(new Vector2(0, 0));
            pointsVariable.Value.Add(new Vector2(32, 0));
            pointsVariable.Value.Add(new Vector2(32, 32));
            pointsVariable.Value.Add(new Vector2(0, 32));
            // close it:
            pointsVariable.Value.Add(new Vector2(0, 0));

            stateSave.VariableLists.Add(pointsVariable);

            AddStateVariable(stateSave);

            AddVariableReferenceList(stateSave);

            ApplySortValuesFromOrderInState(stateSave);

            mDefaults.Add("Polygon", stateSave);
        }


        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                    NineSlice                                                       //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";
            AddPositioningVariables(stateSave);
            AddDimensionsVariables(stateSave, 64, 64, DimensionVariableAction.AllowFileOptions);

            var borderScaleVariable =
                new VariableSave { SetsValue = true, Type = "float", Value = 1f, Name = "BorderScale", Category = "Dimensions" };
            borderScaleVariable.PropertiesToSetOnDisplayer["LabelDragChangeMultiplier"] = .02m;
            borderScaleVariable.PropertiesToSetOnDisplayer["LabelDragValueRounding"] = .01m;

            stateSave.Variables.Add(borderScaleVariable);


            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true, Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Category = "Animation", Name = "Animate" });

            var currentChainNameVariable = new VariableSave { SetsValue = true, Type = "string", Value = null, Category = "Animation", Name = "CurrentChainName" };
            currentChainNameVariable.PropertiesToSetOnDisplayer["IsEditable"] = true;
            stateSave.Variables.Add(currentChainNameVariable);


            AddColorVariables(stateSave);
            stateSave.Variables.Add(CreateBlendVariable());

            var ninesliceTextureAddressVariable =
                new VariableSave { SetsValue = true, Type = "TextureAddress", Value = Gum.Managers.TextureAddress.EntireTexture, Name = "TextureAddress", Category = "Source" };
            ninesliceTextureAddressVariable.ExcludedValuesForEnum.Add(TextureAddress.DimensionsBased);
            stateSave.Variables.Add(ninesliceTextureAddressVariable);

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureLeft", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureTop", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureWidth", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "TextureHeight", Category = "Source" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float?", Value = null, Name = "CustomFrameTextureCoordinateWidth", Category = "Source" });

            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IsTilingMiddleSections", Category = "Source", MinimumGumxVersion = V3StandardSurfaceVersion });

            AddVariableReferenceList(stateSave);

            AddEventVariables(stateSave);
            // For NineSlice we want it to expose its children, but it should not have events itself, as that would break old projects:
            stateSave.Variables.Find(item => item.Name == "ExposeChildrenEvents")!.Value = true;

            AddStateVariable(stateSave);

            AddRotationVariable(stateSave);


            ApplySortValuesFromOrderInState(stateSave);

            mDefaults.Add("NineSlice", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     Component                                                      //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";


            ApplySortValuesFromOrderInState(stateSave);

            AddStateVariable(stateSave);

            // Not sure if component needs this - does it get values from container?
            //AddEventVariables(stateSave);

            mDefaults.Add("Component", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }


        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                    Screen                                                          //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            var stateSave = new StateSave();
            stateSave.Name = "Default";



            ApplySortValuesFromOrderInState(stateSave);

            mDefaults.Add("Screen", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        }

        
        // We shouldn't do this because states above may explicitly not want to set values - like the variable for state
        //foreach (var defaultState in mDefaults.Values)
        //{
        //    foreach (var variable in defaultState.Variables)
        //    {
        //        variable.SetsValue = true;
        //    }
        //}
    }

    private VariableSave CreateBlendVariable()
    {
        return new VariableSave { SetsValue = true, Type = "Blend", Value = Gum.RenderingLibrary.Blend.Normal, Name = "Blend", Category = "Rendering" };
    }

    public static void AddClipsChildren(StateSave stateSave)
    {
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Category = "Children", Type = "bool", Value = false, Name = "ClipsChildren" });
    }

    private void AddRotationVariable(StateSave stateSave)
    {
        var variable = new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" };
        stateSave.Variables.Add(variable);
    }

    public static void AddVariableReferenceList(StateSave stateSave)
    {
        var variableListSave = new VariableListSave<string>
        {
            Type = "string",
            Value = new List<string>(),
            Category = "References",
            Name = "VariableReferences"
        };
        stateSave.VariableLists.Add(variableListSave);
    }

    public static void AddEventVariables(StateSave stateSave, bool defaultHasEvents = false)
    {
        var hasEventsVariable =
            new VariableSave
            {
                SetsValue = true,
                Type = "bool",
                Value = defaultHasEvents,
                Name = "HasEvents",
                Category = "Behavior",
                CanOnlyBeSetInDefaultState = true,
                // We used to exclude them from instances, but there are plenty of situations where we want to hide events on an instance. It's similar to InputTransparent in XamForms
                //ExcludeFromInstances = true
            };


        stateSave.Variables.Add(hasEventsVariable);
        stateSave.Variables.Add(
            new VariableSave
            { 
                SetsValue = true, 
                Type = "bool", 
                Value = defaultHasEvents, 
                Name = "ExposeChildrenEvents", 
                Category = "Behavior", 
                CanOnlyBeSetInDefaultState = true,
                // We used to exclude ExposeChildrenEvents from instances, but there are plenty of situations where we want to modify this value on an instance-by-instance basis. It's similar to InputTransparent in Maui
                //ExcludeFromInstances = true 
            });
    }

    private static void AddStateVariable(StateSave stateSave)
    {
        stateSave.Variables.Add(new VariableSave
        {
            // Don't want it to set the value...
            SetsValue = false, 
            Type = "State",
            Value = "Default",
            Name = "State",
            Category = "States and Visibility"
        });
    }

    private void ApplySortValuesFromOrderInState(StateSave stateSave)
    {
        for (int i = 0; i < stateSave.Variables.Count; i++)
        {
            stateSave.Variables[i].DesiredOrder = i;
        }
    }

    public static void AddColorVariables(StateSave stateSave, bool includeAlpha = true)
    {
        if (includeAlpha)
        {
            VariableSave alphaValue = CreateAlphaVariable(); 
            stateSave.Variables.Add(alphaValue);
        }
        var redValue = new VariableSave
        {
            SetsValue = true,
            Type = "int",
            Value = 255,
            Name = "Red",
            Category = "Rendering",
        };
        stateSave.Variables.Add(redValue);

        var greenValue = new VariableSave
        {
            SetsValue = true,
            Type = "int",
            Value = 255,
            Name = "Green",
            Category = "Rendering",
        };
        stateSave.Variables.Add(greenValue);

        var blueValue = new VariableSave
        {
            SetsValue = true,
            Type = "int",
            Value = 255,
            Name = "Blue",
            Category = "Rendering",
        };
        stateSave.Variables.Add(blueValue);

    }

    private static VariableSave CreateAlphaVariable()
    {
        var alphaValue = new VariableSave
        {
            SetsValue = true,
            Type = "int",
            Value = 255,
            Name = "Alpha",
            Category = "Rendering",
        };

        return alphaValue;
    }

    public static void AddDimensionsVariables(StateSave stateSave, float defaultWidth, float defaultHeight, DimensionVariableAction dimensionVariableAction)
    {
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = defaultWidth, Name = "Width", Category = "Dimensions",
            ToolTipText = "The width of this object in units determined by Width Units." });

        var defaultValue = DimensionUnitType.Absolute;

        if(dimensionVariableAction == DimensionVariableAction.DefaultToPercentageOfFile)
        {
            defaultValue = DimensionUnitType.PercentageOfSourceFile;
        }

        VariableSave variableSave = new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = defaultValue, Name = "WidthUnits", Category = "Dimensions",
            ToolTipText = "Determines how the Width value is interpreted (e.g. absolute pixels, percentage of parent, relative to children)." };
        if (dimensionVariableAction == DimensionVariableAction.ExcludeFileOptions)
        {
            variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
            variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.MaintainFileAspectRatio);
        }
        stateSave.Variables.Add(variableSave);

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float?", Value = null, Name = "MinWidth", Category = "Dimensions" });
        var maxWidthVariable = new VariableSave { SetsValue = true, Type = "float?", Value = null, Name = "MaxWidth", Category = "Dimensions" };
        stateSave.Variables.Add(maxWidthVariable);



        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = defaultHeight, Name = "Height", Category = "Dimensions",
            ToolTipText = "The height of this object in units determined by Height Units." });

        variableSave = new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = defaultValue, Name = "HeightUnits", Category = "Dimensions",
            ToolTipText = "Determines how the Height value is interpreted (e.g. absolute pixels, percentage of parent, relative to children)." };
        if (dimensionVariableAction == DimensionVariableAction.ExcludeFileOptions)

        {
            variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
            variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.MaintainFileAspectRatio);
        }
        stateSave.Variables.Add(variableSave);

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float?", Value = null, Name = "MinHeight", Category = "Dimensions" });
        var maxHeightVariable = new VariableSave { SetsValue = true, Type = "float?", Value = null, Name = "MaxHeight", Category = "Dimensions" };
        stateSave.Variables.Add(maxHeightVariable);

    }

    public static void AddPositioningVariables(StateSave stateSave, bool addOriginVariables = true, bool includeBaseline = false)
    {
        List<object> xUnitsExclusions = new List<object>();
        xUnitsExclusions.Add(PositionUnitType.PixelsFromTop);
        xUnitsExclusions.Add(PositionUnitType.PercentageHeight);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromBottom);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterY);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterYInverted);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromBaseline);

        List<object> yUnitsExclusions = new List<object>();
        yUnitsExclusions.Add(PositionUnitType.PixelsFromLeft);
        yUnitsExclusions.Add(PositionUnitType.PixelsFromCenterX);
        yUnitsExclusions.Add(PositionUnitType.PercentageWidth);
        yUnitsExclusions.Add(PositionUnitType.PixelsFromRight);


        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0.0f, Name = "X", Category = "Position",
            ToolTipText = "The horizontal position of this object's origin in units determined by X Units." });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "XUnits", Category = "Position", ExcludedValuesForEnum = xUnitsExclusions,
            ToolTipText = "Determines how the X value is interpreted (e.g. pixels from left edge, percentage of parent width, pixels from center)." });

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0.0f, Name = "Y", Category = "Position",
            ToolTipText = "The vertical position of this object's origin in units determined by Y Units." });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "YUnits", Category = "Position", ExcludedValuesForEnum = yUnitsExclusions,
            ToolTipText = "Determines how the Y value is interpreted (e.g. pixels from top edge, percentage of parent height, pixels from center)." });

        if(addOriginVariables)
        {
            stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = nameof(HorizontalAlignment), Value = HorizontalAlignment.Left, Name = "XOrigin", Category = "Position" });

            var verticalAlignmentVariable =
                new VariableSave { SetsValue = true, Type = nameof(VerticalAlignment), Value = VerticalAlignment.Top, Name = "YOrigin", Category = "Position" };
            if(includeBaseline == false)
            {
                verticalAlignmentVariable.ExcludedValuesForEnum.Add(VerticalAlignment.TextBaseline);
            }
            stateSave.Variables.Add(verticalAlignmentVariable);
        }

        // Removed December 16, 2024
        // This duplicates functionality
        // that you can get from adding containers
        // to a screen. It's not documente, hasn't been
        // tested, and probably doesn't work in some environments
        // like MonoGame
        //stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = null, Name = "Guide", Category = "Position" });
        AddParentVariables(stateSave);
    }


    private static void AddParentVariables(StateSave stateSave)
    {
        VariableSave variableSave = new VariableSave();
        variableSave.SetsValue = true;
        variableSave.Type = "string";
        variableSave.Name = "Parent";
        variableSave.Category = "Parent";
        variableSave.CanOnlyBeSetInDefaultState = true;


        stateSave.Variables.Add(variableSave);

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IgnoredByParentSize", Category = "Parent" });
    }

    public Func<string, StateSave?>? CustomGetDefaultState;

    public StateSave? TryGetDefaultStateFor(string type, bool throwExceptionOnMissing = true)
    {
        if(mDefaults == null)
        {
            return null;
        }
        if (string.IsNullOrEmpty(type))
        {
            return null;
        }
        if (mDefaults.ContainsKey(type))
        {
            return mDefaults[type];

        }
        else
        {

            StateSave customState = CustomGetDefaultState?.Invoke(type);
            // Vic says - not sure if this is still used. If so, we need to create a 
            // CustomGetDefaultState that returns a state as shown below.
            //#if SKIA
            //                // In Skia we will assume that any type that comes through has a default state:
            //                customState = new StateSave();
            //                AddPositioningVariables(customState, addOriginVariables: true);
            //                mDefaults[type] = customState;
            //#endif

            if (customState == null && throwExceptionOnMissing)
            {
                var message = $"Could not get the default state for type {type} in either the default or through plugins";

                if(type == "Arc")
                {
                    message += "\nIf using MonoGame/KNI, FNA, did you remember to initialize the shapes library?";
                }

                throw new InvalidOperationException(message);
            }
            else
            {
                return customState;
            }
        }
    }

    public StateSave? GetDefaultStateFor(string type, bool throwExceptionOnMissing = true)
    {
        if (mDefaults == null)
        {
            throw new Exception("You must first call Initialize on StandardElementsManager before calling this function");
        }
        return TryGetDefaultStateFor(type, throwExceptionOnMissing);
    }

    public bool IsDefaultType(string type)
    {
        return mDefaults.ContainsKey(type);
    }

    public void PopulateProjectWithDefaultStandards(GumProjectSave gumProjectSave)
    {
        if (mDefaults == null)
        {
            throw new Exception("You must first call Initialize on this StandardElementsManager");
        }


        foreach (string type in SeedableStandardTypes)
        {
            AddStandardElementSaveInstance(gumProjectSave, type);
        }
    }

    public StandardElementSave AddStandardElementSaveInstance(GumProjectSave gumProjectSave, string type)
    {
        if (!mDefaults.TryGetValue(type, out StateSave defaultState))
        {
            // Plugin-contributed standards (the Skia shapes Arc/Canvas/Line/Svg/LottieAnimation and
            // the legacy ColoredCircle/RoundedRectangle) are never placed in mDefaults, so they can't
            // be rebuilt here. Throw a clear, typed error instead of letting the indexer surface a
            // cryptic KeyNotFoundException -- callers must filter to built-in types first (#3373).
            throw new ArgumentException(
                $"'{type}' is not a built-in standard element type, so it can't be created from the " +
                $"default states. Plugin-contributed standards must be added by their owning plugin.",
                nameof(type));
        }

        StandardElementSave elementSave = new StandardElementSave();
        elementSave.Initialize(defaultState);
        elementSave.Name = type;

        
        gumProjectSave.StandardElementReferences.Add( new ElementReference { Name = type, ElementType = ElementType.Standard});
        gumProjectSave.StandardElements.Add( elementSave);

        return elementSave;
    }

    #region Colored Circle State
    public static StateSave GetColoredCircleState()
    {
        if (filledCircleState == null)
        {
            filledCircleState = new StateSave();
            filledCircleState.Name = "Default";
            AddVisibleVariable(filledCircleState);

            StandardElementsManager.AddPositioningVariables(filledCircleState);
            StandardElementsManager.AddDimensionsVariables(filledCircleState, 64, 64,
                StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
            StandardElementsManager.AddColorVariables(filledCircleState);

            AddGradientVariables(filledCircleState);

            AddDropshadowVariables(filledCircleState, minimumGumxVersion: V3StandardSurfaceVersion);


            AddStrokeAndFilledVariables(filledCircleState);

            AddBlendVariable(filledCircleState);

            filledCircleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation", SetsValue = true });

            AddVariableReferenceList(filledCircleState);
            StandardElementsManager.AddEventVariables(filledCircleState);
        }

        return filledCircleState;
    }
    #endregion

    #region Rounded Rectangle State

    public static StateSave GetRoundedRectangleState()
    {
        if (roundedRectangleState == null)
        {
            roundedRectangleState = new StateSave();
            roundedRectangleState.Name = "Default";
            AddVisibleVariable(roundedRectangleState);

            roundedRectangleState.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 5f, Name = "CornerRadius", Category = "Dimensions" });
            roundedRectangleState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation" });

            StandardElementsManager.AddPositioningVariables(roundedRectangleState);
            StandardElementsManager.AddDimensionsVariables(roundedRectangleState, 64, 64,
                StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
            StandardElementsManager.AddColorVariables(roundedRectangleState);

            AddGradientVariables(roundedRectangleState);

            AddDropshadowVariables(roundedRectangleState, minimumGumxVersion: V3StandardSurfaceVersion);

            AddStrokeAndFilledVariables(roundedRectangleState);

            AddBlendVariable(roundedRectangleState);


            AddVariableReferenceList(roundedRectangleState);

            StandardElementsManager.AddClipsChildren(roundedRectangleState);
            StandardElementsManager.AddEventVariables(roundedRectangleState);
        }

        return roundedRectangleState;
    }

    #endregion

    #region Arc State

    public static StateSave GetArcState()
    {
        if (arcState == null)
        {
            arcState = new StateSave();
            arcState.Name = "Default";
            arcState.Variables.Add(new VariableSave { Type = "float", Value = 10f, Category = "Arc", Name = "Thickness", SetsValue = true });

            var startAngle = new VariableSave { Type = "float", Value = 0f, Category = "Arc", Name = "StartAngle", SetsValue = true };
#if GUM
            StandardElementsManagerGumTool.MakeDegreesAngle(startAngle);
#endif
            arcState.Variables.Add(startAngle);

            var sweepAngle = new VariableSave { Type = "float", Value = 90f, Category = "Arc", Name = "SweepAngle", SetsValue = true };
#if GUM
            StandardElementsManagerGumTool.MakeDegreesAngle(sweepAngle);
#endif
            arcState.Variables.Add(sweepAngle);

            arcState.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Arc", Name = "IsEndRounded", SetsValue = true });

            AddVisibleVariable(arcState);

            StandardElementsManager.AddPositioningVariables(arcState);
            StandardElementsManager.AddDimensionsVariables(arcState, 64, 64,
                StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
            StandardElementsManager.AddColorVariables(arcState);
            StandardElementsManager.AddEventVariables(arcState);
            //StandardElementsManager.


            AddBlendVariable(arcState);

            AddGradientVariables(arcState);

            AddDropshadowVariables(arcState, minimumGumxVersion: V3StandardSurfaceVersion);

            AddVariableReferenceList(arcState);
        }

        return arcState;
    }

    #endregion

    #region Headless extended-type registration

    private static bool _extendedDefaultsRegistered;

    // INTERIM: bridges shape (Arc/ColoredCircle/RoundedRectangle/Line) and Skia
    // (Canvas/Svg/LottieAnimation) standard types into headless consumers
    // (Gum.ProjectServices, gumcli) that don't load the WPF Skia plugin or a runtime.
    // Remove once these are promoted to first-class entries in RefreshDefaults() and the
    // matching switches in MainSkiaPlugin.HandleGetDefaultStateForType,
    // AposShapeRuntime.HandleCustomGetDefaultState, and the Skia SystemManagers go away.
    public void RegisterExtendedDefaultStates()
    {
        if (_extendedDefaultsRegistered) return;
        _extendedDefaultsRegistered = true;

        var existing = CustomGetDefaultState;
        CustomGetDefaultState = type => existing?.Invoke(type) ?? GetExtendedDefaultState(type);
    }

    private static StateSave? GetExtendedDefaultState(string type) => type switch
    {
        "Arc"              => GetArcState(),
        "ColoredCircle"    => GetColoredCircleState(),
        "RoundedRectangle" => GetRoundedRectangleState(),
        "Line"             => GetLineState(),
        "Canvas"           => GetCanvasState(),
        "Svg"              => GetSvgState(),
        "LottieAnimation"  => GetLottieAnimationState(),
        _ => null,
    };

    #endregion

    #region Canvas State

    public static StateSave GetCanvasState()
    {
        if (canvasState == null)
        {
            canvasState = new StateSave();
            canvasState.Name = "Default";

            AddVisibleVariable(canvasState);
            AddClipsChildren(canvasState);
            AddPositioningVariables(canvasState);
            AddDimensionsVariables(canvasState, 64, 64, DimensionVariableAction.ExcludeFileOptions);
            AddVariableReferenceList(canvasState);
            AddEventVariables(canvasState);
        }

        return canvasState;
    }

    #endregion

    #region Svg State

    public static StateSave GetSvgState()
    {
        if (svgState == null)
        {
            svgState = new StateSave();
            svgState.Name = "Default";

            AddVisibleVariable(svgState);
            AddPositioningVariables(svgState);
            AddDimensionsVariables(svgState, 100, 100, DimensionVariableAction.AllowFileOptions);
            AddColorVariables(svgState);

            foreach (var variableSave in svgState.Variables.Where(item => item.Type == typeof(DimensionUnitType).Name))
            {
                variableSave.Value = DimensionUnitType.Absolute;
                variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
            }

            svgState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true, Category = "Source" });

            AddBlendVariable(svgState);

            svgState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation", SetsValue = true });

            AddVariableReferenceList(svgState);
            AddEventVariables(svgState);
        }
        return svgState;
    }

    #endregion

    #region Lottie Animation State

    public static StateSave GetLottieAnimationState()
    {
        if (lottieAnimationState == null)
        {
            lottieAnimationState = new StateSave();
            lottieAnimationState.Name = "Default";

            AddVisibleVariable(lottieAnimationState);
            AddPositioningVariables(lottieAnimationState);
            AddDimensionsVariables(lottieAnimationState, 100, 100, DimensionVariableAction.AllowFileOptions);

            lottieAnimationState.Variables.Add(new VariableSave { SetsValue = true, Type = "string", Value = "", Name = "SourceFile", IsFile = true, Category = "Source" });

            AddBlendVariable(lottieAnimationState);
            AddVariableReferenceList(lottieAnimationState);
            AddEventVariables(lottieAnimationState);
        }
        return lottieAnimationState;
    }

    #endregion

    #region Line State

    public static StateSave GetLineState()
    {
        if (lineState == null)
        {
            lineState = new StateSave();
            lineState.Name = "Default";

            lineState.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Line", Name = "IsRounded", SetsValue = true });

            AddVisibleVariable(lineState);

            StandardElementsManager.AddPositioningVariables(lineState);
            StandardElementsManager.AddDimensionsVariables(lineState, 64, 0,
                StandardElementsManager.DimensionVariableAction.ExcludeFileOptions);
            StandardElementsManager.AddColorVariables(lineState);

            lineState.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Category = "Flip and Rotation", Name = "Rotation", SetsValue = true });
            lineState.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 2.0f, Name = "StrokeWidth", Category = "Stroke and Fill" });

            AddGradientVariables(lineState);

            AddDropshadowVariables(lineState, minimumGumxVersion: V3StandardSurfaceVersion);

            AddBlendVariable(lineState);

            AddVariableReferenceList(lineState);
            StandardElementsManager.AddEventVariables(lineState);
        }

        return lineState;
    }

    #endregion

    public static void AddVisibleVariable(StateSave state)
    {
        state.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "Visible", Category = "States and Visibility" });
    }

    // Variables that are part of the v3 native standard surface (the #2929/#2931/#2950 shape
    // expansion on Circle/Rectangle, plus the later RenderTargetTextureSource / SourceShaderFile /
    // IsTilingMiddleSections additions) carry this as their MinimumGumxVersion. The load-time
    // back-fill (GumProjectSaveExtensionMethods.Initialize) skips any variable whose
    // MinimumGumxVersion exceeds the project's version, so a pre-v3 project (e.g. an older FRB1
    // project pinned to an older Gum runtime) is left byte-stable instead of being injected with
    // standard variables its runtime can't compile. See FlatRedBall issue #1881.
    private const int V3StandardSurfaceVersion = (int)GumProjectSave.GumxVersions.ShapeVariableExpansion;

    // Stamps MinimumGumxVersion on the variables a default-state builder just appended (those at
    // index >= startIndex) so the load-time back-fill can gate them by project version. A 0 version
    // is a no-op (the default), keeping the legacy-shape call sites untouched.
    private static void TagMinimumGumxVersion(StateSave state, int startIndex, int minimumGumxVersion)
    {
        if (minimumGumxVersion == 0)
        {
            return;
        }
        for (int i = startIndex; i < state.Variables.Count; i++)
        {
            state.Variables[i].MinimumGumxVersion = minimumGumxVersion;
        }
    }

    /// <summary>
    /// Appends the gradient variables (UseGradient, GradientType, the gradient endpoint positions
    /// and units, the radial inner/outer radius, and the Color2 second stop) to a standard
    /// element's default state. Shared by the plain Circle/Rectangle and the legacy Skia shapes.
    /// </summary>
    /// <param name="state">The default state to append the gradient variables to.</param>
    /// <param name="includeStartColor">
    /// When true (the default, for Arc and the legacy ColoredCircle/RoundedRectangle/Line shapes)
    /// the standalone gradient start color channels (Red1/Green1/Blue1/Alpha1) are added. Issue
    /// #3009 — the two-slot Circle/Rectangle drive the gradient start from the active body color
    /// (FillColor/StrokeColor) instead of a standalone Color1, so they pass false and these
    /// channels are omitted entirely. Color2 (the standalone second stop) is always added.
    /// </param>
    /// <param name="minimumGumxVersion">
    /// When non-zero, stamps each appended variable's <see cref="VariableSave.MinimumGumxVersion"/>
    /// so the load-time back-fill gates them for older projects. Legacy-shape call sites pass 0
    /// (gradients predate v3 there); the plain Circle/Rectangle pass the v3 surface version.
    /// </param>
    public static void AddGradientVariables(StateSave state, bool includeStartColor = true, int minimumGumxVersion = 0)
    {
        int startIndex = state.Variables.Count;

        List<object> xUnitsExclusions = new List<object>();
        xUnitsExclusions.Add(PositionUnitType.PixelsFromTop);
        xUnitsExclusions.Add(PositionUnitType.PercentageHeight);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromBottom);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterY);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterYInverted);
        xUnitsExclusions.Add(PositionUnitType.PixelsFromBaseline);

        List<object> yUnitsExclusions = new List<object>();
        yUnitsExclusions.Add(PositionUnitType.PixelsFromLeft);
        yUnitsExclusions.Add(PositionUnitType.PixelsFromCenterX);
        yUnitsExclusions.Add(PositionUnitType.PercentageWidth);
        yUnitsExclusions.Add(PositionUnitType.PixelsFromRight);


        state.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Rendering", Name = "UseGradient", SetsValue = true });

        state.Variables.Add(new VariableSave
        {
            SetsValue = true,
            Type = typeof(GradientType).Name,
            Value = GradientType.Linear,
            Name = "GradientType",
            Category = "Rendering",
            CustomTypeConverter = new EnumConverter(typeof(GradientType))
        });


        state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Category = "Rendering", Name = "GradientX1" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "GradientX1Units", Category = "Rendering", ExcludedValuesForEnum = xUnitsExclusions });


        state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Category = "Rendering", Name = "GradientY1" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "GradientY1Units", Category = "Rendering", ExcludedValuesForEnum = yUnitsExclusions });

        // Issue #3009 — the standalone gradient start (Color1) only exists on Arc and the legacy
        // shapes; Circle/Rectangle drive the start from the active body color, so they omit it.
        if (includeStartColor)
        {
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Alpha1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Red1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Green1", Category = "Rendering" });
            state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Blue1", Category = "Rendering" });
        }

        state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100f, Category = "Rendering", Name = "GradientX2" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "GradientX2Units", Category = "Rendering", ExcludedValuesForEnum = xUnitsExclusions });

        state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100f, Category = "Rendering", Name = "GradientY2" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "GradientY2Units", Category = "Rendering", ExcludedValuesForEnum = yUnitsExclusions });

        state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 50f, Category = "Rendering", Name = "GradientInnerRadius" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "GradientInnerRadiusUnits", Category = "Rendering" });


        state.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 100f, Category = "Rendering", Name = "GradientOuterRadius" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "GradientOuterRadiusUnits", Category = "Rendering" });

        state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Alpha2", Category = "Rendering" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Red2", Category = "Rendering" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "Green2", Category = "Rendering" });
        state.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "Blue2", Category = "Rendering" });

        TagMinimumGumxVersion(state, startIndex, minimumGumxVersion);
    }


    /// <summary>
    /// Appends the dropshadow surface (HasDropshadow, offsets, blur, and color channels) to a
    /// standard element's default state. Shared by the plain Circle/Rectangle and the legacy Skia
    /// shapes (Arc/ColoredCircle/RoundedRectangle/Line).
    /// </summary>
    /// <param name="stateSave">The default state to append the dropshadow variables to.</param>
    /// <param name="minimumGumxVersion">
    /// When non-zero, stamps each appended variable's <see cref="VariableSave.MinimumGumxVersion"/>
    /// so the load-time back-fill gates them for older projects. All six current callers pass
    /// <see cref="V3StandardSurfaceVersion"/>: dropshadow itself predates v3 on the legacy shapes,
    /// but #2950 renamed their per-axis DropshadowBlurX/DropshadowBlurY into this single scalar
    /// DropshadowBlur for every caller of this helper, not just Circle/Rectangle. FRB1's generated
    /// runtime for the legacy shapes (frozen outside this repo -- see
    /// SkiaGum.Renderables.RenderableArc and friends) predates that rename and never gained the
    /// scalar name, so back-filling "DropshadowBlur" into an old FRB1 project broke the regenerated
    /// runtime with CS0246. If you add a new variable to this method (or any other shared default-
    /// state helper used by these four legacy call sites), gate it explicitly -- there is no build
    /// in this repo that exercises FRB1's frozen consumer to catch a regression here.
    /// </param>
    public static void AddDropshadowVariables(StateSave stateSave, int minimumGumxVersion = 0)
    {
        int startIndex = stateSave.Variables.Count;

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "HasDropshadow", Category = "Dropshadow" });

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0f, Name = "DropshadowOffsetX", Category = "Dropshadow" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3f, Name = "DropshadowOffsetY", Category = "Dropshadow" });

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 3f, Name = "DropshadowBlur", Category = "Dropshadow" });

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "DropshadowAlpha", Category = "Dropshadow" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowRed", Category = "Dropshadow" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowGreen", Category = "Dropshadow" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 0, Name = "DropshadowBlue", Category = "Dropshadow" });

        TagMinimumGumxVersion(stateSave, startIndex, minimumGumxVersion);
    }

    public static void AddStrokeAndFilledVariables(StateSave stateSave)
    {
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = true, Name = "IsFilled", Category = "Stroke and Fill" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 2.0f, Name = "StrokeWidth", Category = "Stroke and Fill" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0.0f, Name = "StrokeDashLength", Category = "Stroke and Fill" });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 0.0f, Name = "StrokeGapLength", Category = "Stroke and Fill" });

    }

    // v3 (#2931): fill + stroke variables for plain Circle / Rectangle, which expose fill and
    // stroke as independent surfaces (unlike legacy ColoredCircle / RoundedRectangle / Arc,
    // which share a single Color). Emitted in three logical sections so the grid reads as a
    // sequence of self-contained groups — stroke (always visible, no enabling bool because
    // StrokeWidth = 0 is the implicit gate), then fill (gated by IsFilled), and AddGradient-
    // Variables continues the pattern after this helper returns (gated by UseGradient).
    //
    // Tool defaults intentionally diverge from the runtime ctor (IsFilled = true + transparent
    // fill, which preserves stroke-only visuals for code-only constructions): IsFilled = false
    // + opaque white fill, so the checkbox honestly says "no fill" and flipping it lights up a
    // visible fill instead of being a no-op against alpha 0.
    public static void AddFillAndStrokeVariables(StateSave stateSave, string category = "Rendering", int minimumGumxVersion = 0)
    {
        int startIndex = stateSave.Variables.Count;

        // Stroke section. Dashed strokes aren't supported on plain Circle / Rectangle, so unlike
        // the legacy AddStrokeAndFilledVariables there is no StrokeDashLength / StrokeGapLength here.
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "float", Value = 2.0f, Name = "StrokeWidth", Category = category });

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "StrokeAlpha", Category = category });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "StrokeRed", Category = category });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "StrokeGreen", Category = category });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "StrokeBlue", Category = category });

        // Fill section
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "bool", Value = false, Name = "IsFilled", Category = category });

        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "FillAlpha", Category = category });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "FillRed", Category = category });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "FillGreen", Category = category });
        stateSave.Variables.Add(new VariableSave { SetsValue = true, Type = "int", Value = 255, Name = "FillBlue", Category = category });

        TagMinimumGumxVersion(stateSave, startIndex, minimumGumxVersion);
    }

    public static void AddBlendVariable(StateSave stateSave, int minimumGumxVersion = 0)
    {
        var blendariable = new VariableSave { SetsValue = true, Type = "Blend", Value = Gum.RenderingLibrary.Blend.Normal, Name = "Blend", Category = "Rendering", MinimumGumxVersion = minimumGumxVersion };

        stateSave.Variables.Add(blendariable);
    }

}
