using Gum.DataTypes.Variables;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

using GumElement = Gum.DataTypes.ElementSave;
using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;

using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GumScreen = Gum.DataTypes.ScreenSave;

namespace GluePlugin.Converters
{
    public class GlueToGumTextureCoordinateConversionLogic : Singleton<GlueToGumTextureCoordinateConversionLogic>
    {
        public const string TextureCoordinatesCategory = "TextureCoordinates";

        public void ApplyTextureCoordinatesVariables(VariableGroupDictionary variableGroups, List<VariableSave> gumVariables, GlueElement glueElement)
        {
            var variableGroup = variableGroups.GetVariablesInCategory(TextureCoordinatesCategory);

            var rightTexturePixelVar = variableGroup.FirstOrDefault(item => item.RootName == "RightTexturePixel");
            var leftTexturePixelVar = variableGroup.FirstOrDefault(item => item.RootName == "LeftTexturePixel");
            var bottomTexturePixelVar = variableGroup.FirstOrDefault(item => item.RootName == "BottomTexturePixel");
            var topTexturePixelVar = variableGroup.FirstOrDefault(item => item.RootName == "TopTexturePixel");
            var textureVariable = variableGroup.FirstOrDefault(item => item.RootName == "Texture");

            var namedObject = variableGroup.First().NamedObjectSave;

            var setsAny = rightTexturePixelVar != null ||
                leftTexturePixelVar != null ||
                bottomTexturePixelVar != null ||
                topTexturePixelVar != null;


            if (setsAny == false)
            {
                ApplyTextureAddress(gumVariables, namedObject, Gum.Managers.TextureAddress.EntireTexture);
            }
            else
            {
                // Sets some, but not all, so read the texture to fill the value
                // This sucks, because the non-set values are either 0 or 1 depending on 
                // which side they're on, but if they're 1, we can't have Gum mimic that behavior
                // Eventually we might, if it becomes an issue, but for now we'll explicitly set the
                // values.
                ApplyTextureAddress(gumVariables, namedObject, Gum.Managers.TextureAddress.Custom);

                float left;
                float top;
                if(leftTexturePixelVar != null)
                {
                    left = (float)leftTexturePixelVar.InstructionSave.Value;
                }
                else
                {
                    left = 0;
                }

                ApplyTextureLeft(gumVariables, left, namedObject);

                if(topTexturePixelVar != null)
                {
                    top = (float)topTexturePixelVar.InstructionSave.Value;
                }
                else
                {
                    top = 0;
                }
                ApplyTextureTop(gumVariables, top, namedObject);

                float right;
                float bottom;

                BitmapDecoder decoder = null;

                string fileName = null;
                if(textureVariable != null)
                {
                    fileName = $"{GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive}../Content/{glueElement.Name}/{(string)textureVariable.InstructionSave.Value}.png";
                    fileName = ToolsUtilities.FileManager.RemoveDotDotSlash(fileName);
                }


                if (rightTexturePixelVar?.InstructionSave.Value is float)
                {
                    right = (float)rightTexturePixelVar.InstructionSave.Value;
                }
                else
                {
                    decoder = TryLoadDecoder(fileName);
                    // todo - get the texture value:
                    right = decoder?.Frames[0].PixelWidth ?? 256;
                }
                ApplyTextureWidth(gumVariables, right - left, namedObject);

                if(bottomTexturePixelVar?.InstructionSave.Value is float)
                {
                    bottom = (float)bottomTexturePixelVar.InstructionSave.Value;
                }
                else
                {
                    if(decoder == null)
                    {
                        decoder = TryLoadDecoder(fileName);
                    }
                    // todo - get the value from the entire texture:
                    bottom = decoder?.Frames[0].PixelHeight ?? 256;
                }
                ApplyTextureHeight(gumVariables, bottom - top, namedObject);
            }
        }

        private static BitmapDecoder TryLoadDecoder(string fileName)
        {
            BitmapDecoder decoder = null;
            if (!string.IsNullOrEmpty(fileName))
            {
                using (var imageStream = System.IO.File.OpenRead(fileName))
                {
                    decoder = BitmapDecoder.Create(imageStream, BitmapCreateOptions.IgnoreColorProfile, BitmapCacheOption.None);
                }
            }

            return decoder;
        }

        private static void ApplyTextureAddress(List<VariableSave> gumVariables, FlatRedBall.Glue.SaveClasses.NamedObjectSave namedObject, TextureAddress textureAddress)
        {
            VariableSave variableSave = new VariableSave();
            variableSave.Name = $"{namedObject.InstanceName}.Texture Address";
            variableSave.Type = nameof(Gum.Managers.TextureAddress);
            variableSave.Value = textureAddress;
            gumVariables.Add(variableSave);
        }

        private static void ApplyTextureLeft(List<VariableSave> gumVariables, float value, FlatRedBall.Glue.SaveClasses.NamedObjectSave namedObject)
        {
            VariableSave variableSave = new VariableSave();
            variableSave.Name = $"{namedObject.InstanceName}.Texture Left";
            variableSave.Type = "int";
            variableSave.Value = RenderingLibrary.Math.MathFunctions.RoundToInt(value);
            gumVariables.Add(variableSave);
        }

        private static void ApplyTextureTop(List<VariableSave> gumVariables, float value, FlatRedBall.Glue.SaveClasses.NamedObjectSave namedObject)
        {
            VariableSave variableSave = new VariableSave();
            variableSave.Name = $"{namedObject.InstanceName}.Texture Top";
            variableSave.Type = "int";
            variableSave.Value = RenderingLibrary.Math.MathFunctions.RoundToInt(value);
            gumVariables.Add(variableSave);
        }

        private static void ApplyTextureWidth(List<VariableSave> gumVariables, float width, FlatRedBall.Glue.SaveClasses.NamedObjectSave namedObject)
        {
            VariableSave variableSave;

            variableSave = new VariableSave();
            variableSave.Name = $"{namedObject.InstanceName}.Texture Width";
            variableSave.Type = "int";
            variableSave.Value = RenderingLibrary.Math.MathFunctions.RoundToInt(width);
            gumVariables.Add(variableSave);
        }

        private static void ApplyTextureHeight(List<VariableSave> gumVariables, float height, FlatRedBall.Glue.SaveClasses.NamedObjectSave namedObject)
        {
            VariableSave variableSave;
            
            variableSave = new VariableSave();
            variableSave.Name = $"{namedObject.InstanceName}.Texture Height";
            variableSave.Type = "int";
            variableSave.Value = RenderingLibrary.Math.MathFunctions.RoundToInt(height);
            gumVariables.Add(variableSave);
        }
    }
}
