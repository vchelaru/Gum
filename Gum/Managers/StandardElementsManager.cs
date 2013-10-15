using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using RenderingLibrary.Graphics;
using Gum.Plugins;
using Gum.RenderingLibrary;
using Gum.PropertyGridHelpers.Converters;
using Microsoft.Xna.Framework;

namespace Gum.Managers
{
    #region Enums

    public enum GeneralUnitType
    {
        PixelsFromSmall,
        PixelsFromLarge,
        PixelsFromMiddle,
        Percentage,
        PercentageOfFile
    }

    public enum PositionUnitType
    {
        PixelsFromLeft,
        PixelsFromTop,
        PercentageWidth,
        PercentageHeight,
        PixelsFromRight,
        PixelsFromBottom,
        PixelsFromCenterX,
        PixelsFromCenterY

    }



    #endregion

    public class StandardElementsManager
    {
        #region Fields

        Dictionary<string, StateSave> mDefaults;

        static StandardElementsManager mSelf;

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

        public string DefaultType
        {
            get
            {
                return "Container";
            }
        }

        public Dictionary<string, StateSave> DefaultStates
        {
            get
            {
                return mDefaults;
            }
        }

        #endregion



        public void Initialize()
        {
            mDefaults = new Dictionary<string, StateSave>();

            // Eventually this would get read from somewhere like an XML file
            // or a CSV file, but for
            // now we'll just use hard values.
            


            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     TEXT                                                           //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            StateSave stateSave = new StateSave();
            stateSave.Name = "Default";
            AddPositioningVariables(stateSave);
            AddDimensionsVariables(stateSave, 100, 50, false);
            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = true, Name = "Visible" });
            stateSave.Variables.Add(new VariableSave { Type = "string", Value = "Hello", Name = "Text", Category = "Text" });
            stateSave.Variables.Add(new VariableSave { Type = "VerticalAlignment", Value = VerticalAlignment.Top, Name = "VerticalAlignment", Category = "Text" });
            stateSave.Variables.Add(new VariableSave { Type = "HorizontalAlignment", Value = HorizontalAlignment.Left, Name = "HorizontalAlignment", Category = "Text" });
            stateSave.Variables.Add(new VariableSave { Type = "string", Value = "Arial", Name = "Font", IsFont = true, Category = "Font" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 18, Name = "FontSize", Category = "Font" });


            stateSave.Variables.Add(new VariableSave { Type = "string", Value = "Default", Name = "State", CustomTypeConverter = new AvailableStatesConverter() });

            AddColorVariables(stateSave, includeAlpha:true);

            PluginManager.Self.ModifyDefaultStandardState("Text", stateSave);
            mDefaults.Add("Text", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////






            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     Sprite                                                         //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            stateSave = new StateSave();
            stateSave.Name = "Default";
            AddPositioningVariables(stateSave);
            AddDimensionsVariables(stateSave, 0, 0, true);
            stateSave.Variables.Add(new VariableSave { Type = "string", Value = "", Name = "SourceFile", IsFile = true});
            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = true, Name = "Visible" });
            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Animation", Name = "Animate" });

            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Flip", Name = "FlipHorizontal" });
            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = false, Category = "Flip", Name = "FlipVertical" });

            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = false, Name = "Custom Texture Coordinates", Category="Source" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 0, Name = "Texture Top", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 0, Name = "Texture Left", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 0, Name = "Texture Width", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 0, Name = "Texture Height", Category = "Source" });
            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = false, Name = "Wrap", Category = "Source" });

            AddColorVariables(stateSave);

            stateSave.Variables.Add(new VariableSave { Type = "Blend", Value = Blend.Normal, Name = "Blend", Category = "Rendering" });

            List<string> list = new List<string>();
            stateSave.VariableLists.Add(new VariableListSave<string> { Type = "string", Value = list, Category = "Animation", Name = "AnimationFrames"});
            PluginManager.Self.ModifyDefaultStandardState("Sprite", stateSave);
            mDefaults.Add("Sprite", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////








            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                   Container                                                        //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            stateSave = new StateSave();
            stateSave.Name = "Default";

            AddPositioningVariables(stateSave);

            AddDimensionsVariables(stateSave, 150, 150, false);

            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = true, Name = "Visible" });
            PluginManager.Self.ModifyDefaultStandardState("Container", stateSave);
            mDefaults.Add("Container", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////







            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                               ColoredRectangle                                                     //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            stateSave = new StateSave();
            stateSave.Name = "Default";

            AddPositioningVariables(stateSave);

            AddDimensionsVariables(stateSave, 50, 50, false);

            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = true, Name = "Visible" });
            AddColorVariables(stateSave, true);
            PluginManager.Self.ModifyDefaultStandardState("ColoredRectangle", stateSave);
            mDefaults.Add("ColoredRectangle", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                    NineSlice                                                         //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            stateSave = new StateSave();
            stateSave.Name = "Default";
            AddPositioningVariables(stateSave);
            AddDimensionsVariables(stateSave, 64, 64, false);
            stateSave.Variables.Add(new VariableSave { Type = "string", Value = "", Name = "SourceFile", IsFile = true });
            stateSave.Variables.Add(new VariableSave { Type = "bool", Value = true, Name = "Visible" });

            AddColorVariables(stateSave);
            stateSave.Variables.Add(new VariableSave { Type = "Blend", Value = Blend.Normal, Name = "Blend", Category = "Rendering" });

            PluginManager.Self.ModifyDefaultStandardState("NineSlice", stateSave);
            mDefaults.Add("NineSlice", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////



            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                     Component                                                      //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            stateSave = new StateSave();
            stateSave.Name = "Default";
            PluginManager.Self.ModifyDefaultStandardState("Component", stateSave);

            stateSave.Variables.Add(new VariableSave { Type = "string", Value = "Default", Name = "State", CustomTypeConverter = new AvailableStatesConverter()});


            mDefaults.Add("Component", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            //                                                    Screen                                                          //
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            stateSave = new StateSave();
            stateSave.Name = "Default";
            PluginManager.Self.ModifyDefaultStandardState("Screen", stateSave);

            mDefaults.Add("Screen", stateSave);
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




        }

        private static void AddColorVariables(StateSave stateSave, bool includeAlpha = true)
        {
            if (includeAlpha)
            {
                stateSave.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Alpha", Category="Rendering" });
            }
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Red", Category = "Rendering" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Green", Category = "Rendering" });
            stateSave.Variables.Add(new VariableSave { Type = "int", Value = 255, Name = "Blue", Category = "Rendering" });
        }

        private static void AddDimensionsVariables(StateSave stateSave, float defaultWidth, float defaultHeight, bool allowFromFile)
        {


            stateSave.Variables.Add(new VariableSave { Type = "float", Value = defaultWidth, Name = "Width", Category = "Dimensions" });

            VariableSave variableSave =
                new VariableSave { Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "Width Units", Category = "Dimensions" };
            if (!allowFromFile)
            {
                variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
            }
            stateSave.Variables.Add(variableSave);



            stateSave.Variables.Add(new VariableSave { Type = "float", Value = defaultHeight, Name = "Height", Category = "Dimensions" });
            variableSave =
                new VariableSave { Type = typeof(DimensionUnitType).Name, Value = DimensionUnitType.Absolute, Name = "Height Units", Category = "Dimensions" };
            if (!allowFromFile)
            {
                variableSave.ExcludedValuesForEnum.Add(DimensionUnitType.PercentageOfSourceFile);
            }
            stateSave.Variables.Add(variableSave);
        }

        private static void AddPositioningVariables(StateSave stateSave)
        {
            List<object> xUnitsExclusions = new List<object>();
            xUnitsExclusions.Add(PositionUnitType.PixelsFromTop);
            xUnitsExclusions.Add(PositionUnitType.PercentageHeight);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromBottom);
            xUnitsExclusions.Add(PositionUnitType.PixelsFromCenterY);

            List<object> yUnitsExclusions = new List<object>();
            yUnitsExclusions.Add(PositionUnitType.PixelsFromLeft);
            yUnitsExclusions.Add(PositionUnitType.PixelsFromCenterX);
            yUnitsExclusions.Add(PositionUnitType.PercentageWidth);
            yUnitsExclusions.Add(PositionUnitType.PixelsFromRight);


            stateSave.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Name = "X", Category = "Position" });
            stateSave.Variables.Add(new VariableSave { Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromLeft, Name = "X Units", Category = "Position", ExcludedValuesForEnum = xUnitsExclusions });

            stateSave.Variables.Add(new VariableSave { Type = "float", Value = 0.0f, Name = "Y", Category = "Position" });
            stateSave.Variables.Add(new VariableSave { Type = typeof(PositionUnitType).Name, Value = PositionUnitType.PixelsFromTop, Name = "Y Units", Category = "Position", ExcludedValuesForEnum = yUnitsExclusions });

            stateSave.Variables.Add(new VariableSave { Type = "HorizontalAlignment", Value = HorizontalAlignment.Left, Name = "X Origin", Category = "Position" });
            stateSave.Variables.Add(new VariableSave { Type = "VerticalAlignment", Value = VerticalAlignment.Top, Name = "Y Origin", Category = "Position" });

            stateSave.Variables.Add(new VariableSave { Type = "string", Value = null, Name = "Guide", Category = "Position" });

            AddParentVariables(stateSave);
        }

        private static void AddParentVariables(StateSave variables)
        {
            VariableSave variableSave = new VariableSave();
            variableSave.Type = "string";
            variableSave.Name = "Parent";
            variableSave.CanOnlyBeSetInDefaultState = true;
            variableSave.CustomTypeConverter = new AvailableInstancesConverter();
            variables.Variables.Add(variableSave);
        }

        public StateSave GetDefaultStateFor(string type)
        {
            return mDefaults[type];
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


            foreach (KeyValuePair<string, StateSave> kvp in mDefaults)
            {

                string type = kvp.Key;

                if (type != "Screen")
                {
                    AddStandardElementSaveInstance(gumProjectSave, type);
                }
            }
        }

        public void AddStandardElementSaveInstance(GumProjectSave gumProjectSave, string type)
        {
            StandardElementSave elementSave = new StandardElementSave();
            elementSave.Initialize(mDefaults[type]);
            elementSave.Name = type;

            
            gumProjectSave.StandardElementReferences.Add( new ElementReference { Name = type, ElementType = ElementType.Standard});
            gumProjectSave.StandardElements.Add( elementSave);
        }

    }
}
