using FlatRedBall.Glue.SaveClasses;
using GluePlugin.Logic;
using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

using GumScreen = Gum.DataTypes.ScreenSave;


namespace GluePlugin.Converters
{
    public class GumToGlueConverter : Singleton<GumToGlueConverter>
    {
        public string ConvertVariableName(string variableName, InstanceSave instance)
        {
            var convertedName = variableName;

            // values for any type:
            switch(variableName)
            {
                case "Rotation": convertedName = "RotationZ"; break;
            }

            if (instance.BaseType == "Sprite")
            {
                switch (variableName)
                {
                    case "Height": convertedName = "TextureScale"; break;
                    case "Width": convertedName = "TextureScale"; break;

                    // eventually this might be .achx or something else, but for now always texture
                    case "SourceFile": convertedName = "Texture"; break;
                    case "Texture Left": convertedName = "LeftTexturePixel"; break;
                    case "Texture Right": convertedName = "RightTexturePixel"; break;
                    case "Texture Top": convertedName = "TopTexturePixel"; break;
                    case "Texture Bottom": convertedName = "BottomTexturePixel"; break;
                }

            }


            return convertedName;
        }

        internal IElement ConvertElement(ElementSave element)
        {
            if (element is GumScreen)
            {
                var glueScreen = new FlatRedBall.Glue.SaveClasses.ScreenSave();
                glueScreen.Name = $"Screens\\{element.Name}";

                return glueScreen;
            }
            else if (element is ComponentSave)
            {
                var glueEntity = new FlatRedBall.Glue.SaveClasses.EntitySave();
                glueEntity.Name = $"Entities\\{element.Name}";

                // components should have some variables by default:
                glueEntity.CustomVariables.Add(new CustomVariable()
                {
                    Name = "X",
                    Type = "float",
                });

                glueEntity.CustomVariables.Add(new CustomVariable()
                {
                    Name = "Y",
                    Type = "float",
                });

                glueEntity.CustomVariables.Add(new CustomVariable()
                {
                    Name = "RotationZ",
                    Type = "float",
                });

                return glueEntity;
            }
            else
            {
                throw new NotImplementedException();
            }
        }

        public object ConvertVariableValue(string gumVariableName, object variableValue, InstanceSave instance)
        {
            var convertedValue = variableValue;

            // conversions regardless of type:
            if(gumVariableName == "Rotation")
            {
                if(variableValue is float)
                {
                    convertedValue = (float)(2 * Math.PI * ((float)variableValue) / 360);
                }
            }

            if (instance.BaseType == "Sprite")
            {
                if (gumVariableName == "Height")
                {
                    if (variableValue is float)
                    {
                        convertedValue = ((float)variableValue) / 100.0f;
                    }
                }

                else if (gumVariableName == "Width")
                {
                    if (variableValue is float)
                    {
                        convertedValue = ((float)variableValue) / 100.0f;
                    }
                }
                else if (gumVariableName == "SourceFile")
                {
                    if (variableValue is string)
                    {
                        var filePath = new FilePath(variableValue as string);

                        var strippedName = filePath.CaseSensitiveNoPathNoExtension;

                        // for now assume it's in the same entity/screen, but eventually need to make it smarter
                        convertedValue = strippedName;
                    }
                }
            }

            return convertedValue;
        }

        public string ConvertType(string gumVariableName, object variableValue, InstanceSave instance)
        {
            string typeToReturn = null;

            /// any type
            if(gumVariableName == "X")
            {
                typeToReturn = "float";
            }
            else if(gumVariableName == "Y")
            {
                typeToReturn = "float";
            }
            else if(gumVariableName == "Rotation")
            {
                typeToReturn = "float";
            }

            if (instance.BaseType == "Sprite")
            {
                if (gumVariableName == "SourceFile" && variableValue is string)
                {
                    typeToReturn = "Microsoft.Xna.Framework.Graphics.Texture2D";
                }

                else if (gumVariableName == "Texture Left" ||
                    gumVariableName == "Texture Right" ||
                    gumVariableName == "Texture Top" ||
                    gumVariableName == "Texture Bottom")
                {
                    typeToReturn = "float";
                }


            }

            return typeToReturn;
        }

        public NamedObjectSave ConvertInstance(InstanceSave instance)
        {
            var newNamedObjectSave = new NamedObjectSave();
            newNamedObjectSave.InstanceName = instance.Name;

            if (!string.IsNullOrEmpty(instance.BaseType))
            {
                var baseElement = ObjectFinder.Self.GetElementSave(instance.BaseType);

                if (baseElement is StandardElementSave)
                {
                    switch (instance.BaseType)
                    {
                        case "Sprite":
                            newNamedObjectSave.SourceType = SourceType.FlatRedBallType;
                            newNamedObjectSave.SourceClassType = "Sprite";
                            break;
                        case "Circle":
                            newNamedObjectSave.SourceType = SourceType.FlatRedBallType;
                            newNamedObjectSave.SourceClassType = "Circle";
                            break;
                        case "Rectangle":
                            newNamedObjectSave.SourceType = SourceType.FlatRedBallType;
                            newNamedObjectSave.SourceClassType = "AxisAlignedRectangle";
                            break;
                    }
                }
                else if (baseElement is ComponentSave)
                {
                    newNamedObjectSave.SourceType = SourceType.Entity;
                    newNamedObjectSave.SourceClassType = $"Entities\\{baseElement.Name}";
                }
            }

            return newNamedObjectSave;
        }

        internal bool ApplyGumVariableCustom(InstanceSave gumInstance, ElementSave gumElement, string glueVariableName, object glueValue)
        {
            var handled = false;
            if (glueVariableName == "Parent")
            {
                var parentValue = (string)glueValue;

                handled = true;

                var glueElement = GluePluginObjectFinder.Self.GetGlueElementFrom(gumElement);
                var namedObject = GluePluginObjectFinder.Self.GetNamedObjectSave(gumInstance, glueElement);

                if (parentValue == null)
                {
                    var isInList = glueElement.NamedObjects.Contains(namedObject) == false;

                    if (isInList)
                    {
                        var list = glueElement.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(namedObject));

                        if (list != null)
                        {
                            list.ContainedObjects.Remove(namedObject);
                        }
                        glueElement.NamedObjects.Add(namedObject);
                    }
                }
                else
                {
                    // first remove it from wherever it's referenced...
                    var wasRemoved = false;

                    var isInList = glueElement.NamedObjects.Contains(namedObject) == false;

                    if (!isInList)
                    {
                        glueElement.NamedObjects.Remove(namedObject);
                        wasRemoved = true;
                    }
                    else
                    {
                        var list = glueElement.NamedObjects.FirstOrDefault(item => item.ContainedObjects.Contains(namedObject));

                        if (list != null && list.InstanceName != parentValue)
                        {
                            list.ContainedObjects.Remove(namedObject);
                            wasRemoved = true;
                        }
                    }

                    if (wasRemoved)
                    {
                        var newList = glueElement.NamedObjects.FirstOrDefault(item => item.InstanceName == parentValue);

                        if (newList != null)
                        {
                            newList.ContainedObjects.Add(namedObject);
                        }
                    }

                }
            }

            return handled;
        }
    }
}
