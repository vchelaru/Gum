using FlatRedBall.Glue.SaveClasses;
using Gum;
using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using RenderingLibrary.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GlueEntity = FlatRedBall.Glue.SaveClasses.EntitySave;
using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;
using GlueState = FlatRedBall.Glue.SaveClasses.StateSave;
using GlueStateCategory = FlatRedBall.Glue.SaveClasses.StateSaveCategory;

using GumScreen = Gum.DataTypes.ScreenSave;
using GumElement = Gum.DataTypes.ElementSave;
using GumState = Gum.DataTypes.Variables.StateSave;
using GumStateCategory = Gum.DataTypes.Variables.StateSaveCategory;
using FlatRedBall.Content.Instructions;
using Microsoft.Xna.Framework;

namespace GluePlugin.Converters
{
    public class GlueToGumProjectConverter : Singleton<GlueToGumProjectConverter>
    {

        public GumProjectSave ToGumProjectSave(GlueProjectSave glueProject)
        {
            GumProjectSave gumProject = new GumProjectSave();

            foreach(var entitySave in glueProject.Entities)
            {
                var component = ToComponent(entitySave);
                gumProject.Components.Add(component);

                var componentReference = ToComponentReference(entitySave);
                gumProject.ComponentReferences.Add(componentReference);
            }

            foreach (var screen in glueProject.Screens)
            {
                var gumScreen = ToScreen(screen);
                gumProject.Screens.Add(gumScreen);

                var screenReference = ToScreenReference(screen);
                gumProject.ScreenReferences.Add(screenReference);
            }

            gumProject.AddNewStandardElementTypes();
            gumProject.FixStandardVariables();

            // set the finder before we do this because we'll need it to get objects:
            ObjectFinder.Self.GumProjectSave = gumProject;

            AdjustStandardsToMatchFrb(gumProject);

            GumCommands.Self.GuiCommands.PrintOutput($"Created Gum project from Glue project");

            return gumProject;
        }

        // This auto-adds all inherited objects, but we don't want to do that because
        // Glue has its own inheritance rules:
        //private void AddInheritedInstances(GumProjectSave gumProject)
        //{
        //    var sortedScreens = gumProject.Screens
        //        .OrderBy(item => GetInheritanceDepth(item))
        //        .ToArray();

        //    foreach(var screen in sortedScreens)
        //    {
        //        if(!string.IsNullOrEmpty(screen.BaseType ))
        //        {
        //            Gum.PropertyGridHelpers.SetVariableLogic.Self.ReactToPropertyValueChanged(
        //                "Base Type", null, screen, null, false);

        //        }
        //    }
        //}


        //private int GetInheritanceDepth(ElementSave gumElement, int depth = 0)
        //{
        //    if(string.IsNullOrEmpty(gumElement.BaseType))
        //    {
        //        return depth;
        //    }
        //    else
        //    {
        //        var baseElement = ObjectFinder.Self.GetElementSave(gumElement.BaseType);

        //        return GetInheritanceDepth(baseElement, depth + 1);
        //    }
        //}

        private void AdjustStandardsToMatchFrb(GumProjectSave gumProject)
        {
            foreach (var standardElement in gumProject.StandardElements)
            {
                if(standardElement.Name == "Circle")
                {
                    standardElement.DefaultState.SetValue("Width", 2.0f, "float");
                    standardElement.DefaultState.SetValue("Height", 2.0f, "float");
                    standardElement.DefaultState.SetValue("Radius", 1.0f, "float");
                }

                foreach(var state in standardElement.AllStates)
                {
                    if(standardElement.Name == "Polygon")
                    {
                        // Polygons don't really have a width/height
                        state.SetValue("X Origin", HorizontalAlignment.Left, nameof(HorizontalAlignment));
                        state.SetValue("Y Origin", VerticalAlignment.Top, nameof(VerticalAlignment));
                    }
                    else
                    {
                        state.SetValue("X Origin", HorizontalAlignment.Center, nameof(HorizontalAlignment));
                        state.SetValue("Y Origin", VerticalAlignment.Center, nameof(VerticalAlignment));
                    }



                    state.SetValue("X Units", PositionUnitType.PixelsFromCenterX, nameof(PositionUnitType));
                    state.SetValue("Y Units", PositionUnitType.PixelsFromCenterYInverted, nameof(PositionUnitType));
                }
            }
        }

        private GumScreen ToScreen(GlueScreen glueScreen)
        {
            var gumScreen = new GumScreen();
            gumScreen.States.Add(new Gum.DataTypes.Variables.StateSave() { Name = "Default" });
            gumScreen.Name = glueScreen.Name.Substring(
                "Screens\\".Length);

            if(!string.IsNullOrEmpty(glueScreen.BaseScreen))
            {
                // This sets the base type, but doesn't populate the instances.
                // Once the entire project is converted over, we'll loop through all 
                // screens that have base types and have them act as if the value was 
                // just set, causing the inheritance plugin to handle it.
                var baseScreen = glueScreen.BaseScreen.Substring(
                    "Screens\\".Length);

                gumScreen.BaseType = baseScreen;
            }

            AddInstances(glueScreen, gumScreen);

            AddStates(glueScreen, gumScreen);

            return gumScreen;
        }

        private ElementReference ToScreenReference(GlueScreen screen)
        {
            var elementReference = new ElementReference();
            elementReference.ElementType = ElementType.Screen;
            elementReference.Name = screen.Name.Substring(
                "Screens\\".Length);
            return elementReference;
        }

        private ComponentSave ToComponent(EntitySave entitySave)
        {
            ComponentSave component = new ComponentSave();
            component.BaseType = "Container";
            component.States.Add(new Gum.DataTypes.Variables.StateSave() { Name = "Default" });

            component.Name = entitySave.Name.Substring(
                "Entities\\".Length);

            component.DefaultState.SetValue("Width", 
                0.0f, "float");
            component.DefaultState.SetValue("Height", 
                0.0f, "float");

            component.DefaultState.SetValue("Width Units", 
                DimensionUnitType.RelativeToChildren, nameof(DimensionUnitType));
            component.DefaultState.SetValue("Height Units", 
                DimensionUnitType.RelativeToChildren, nameof(DimensionUnitType));

            AddInstances(entitySave, component);

            AddStates(entitySave, component);

            return component;
        }

        private void AddStates(GlueElement glueElement, GumElement gumElement)
        {
            foreach(var glueState in glueElement.States)
            {
                var gumState = ToGumState(glueState, glueElement);

                gumElement.States.Add(gumState);
            }

            foreach(var glueStateCategory in glueElement.StateCategoryList)
            {
                var gumStateCategory = ToGumStateCategory(glueStateCategory, glueElement);

                gumElement.Categories.Add(gumStateCategory);
            }
        }

        private GumState ToGumState(FlatRedBall.Glue.SaveClasses.StateSave glueState, GlueElement glueElement)
        {
            var gumState = new GumState();
            gumState.Name = glueState.Name;

            VariableGroupDictionary variableGroups = new VariableGroupDictionary();

            foreach (var glueVariable in glueState.InstructionSaves)
            {
                AddGumVariables(glueVariable, null, glueElement, gumState.Variables, gumState.VariableLists, variableGroups, isInState:true);
            }

            ApplyVariableGroups(variableGroups, glueElement, gumState.Variables);


            // everything should set value
            foreach (var gumVariable in gumState.Variables)
            {
                gumVariable.SetsValue = true;
            }


            return gumState;
        }

        private void ApplyVariableGroups(VariableGroupDictionary variableGroups, GlueElement glueElement, 
            List<VariableSave> gumVariables)
        {
            if(variableGroups.HasCategory(GlueToGumTextureCoordinateConversionLogic.TextureCoordinatesCategory))
            {
                GlueToGumTextureCoordinateConversionLogic.Self.ApplyTextureCoordinatesVariables(variableGroups, gumVariables, glueElement);
            }
        }

        private GumStateCategory ToGumStateCategory(GlueStateCategory glueStateCategory, GlueElement glueElement)
        {
            var gumStateCategory = new GumStateCategory();
            gumStateCategory.Name = glueStateCategory.Name;

            foreach(var glueState in glueStateCategory.States)
            {
                var gumState = ToGumState(glueState, glueElement);
                gumStateCategory.States.Add(gumState);
            }

            return gumStateCategory;
        }

        private ElementReference ToComponentReference(EntitySave entitySave)
        {
            var elementReference = new ElementReference();
            elementReference.ElementType = ElementType.Component;
            elementReference.Name = entitySave.Name.Substring(
                "Entities\\".Length);

            return elementReference;
        }

        private void AddInstances(GlueElement glueElement, ElementSave gumElement)
        {
            foreach (var namedObject in glueElement.NamedObjects)
            {
                var instance = ToInstanceSave(namedObject);

                List<VariableSave> variableList = null;
                List<VariableListSave> variableListList = null;
                GetVariableSaves(namedObject, null, glueElement, out variableList, out variableListList);
                gumElement.DefaultState.Variables.AddRange(variableList);
                gumElement.DefaultState.VariableLists.AddRange(variableListList);

                gumElement.Instances.Add(instance);

                // Do we want to "trust" the .glux? 
                // ShapeCollection instances can now
                // contain shapes, so this if-check is
                // wrong. I could add an explicit check
                // for list or ShapeCollection, but maybe
                // I'll just comment out the if-check for now.
                //if (namedObject.IsList)
                {
                    foreach (var containedObject in namedObject.ContainedObjects)
                    {
                        var containedInstance = ToInstanceSave(containedObject);
                        gumElement.Instances.Add(containedInstance);
                        List<VariableSave> containedVariableList;
                        List<VariableListSave> containedVariableListList;
                        GetVariableSaves(containedObject, namedObject, glueElement, out containedVariableList, out containedVariableListList);
                        gumElement.DefaultState.Variables.AddRange(containedVariableList);
                    }
                }
            }
        }

        private void GetVariableSaves(NamedObjectSave namedObject, NamedObjectSave parentNamedObject, GlueElement glueElement,
            out List<VariableSave> gumVariables, out List<VariableListSave> gumVariableLists)
        {
            VariableGroupDictionary variableGroups = new VariableGroupDictionary();

            gumVariables = new List<VariableSave>();
            gumVariableLists = new List<VariableListSave>();
            foreach(var glueVariable in namedObject.InstructionSaves)
            {
                AddGumVariables(glueVariable, namedObject, glueElement, gumVariables, gumVariableLists, variableGroups);
            }

            ApplyVariableGroups(variableGroups, glueElement, gumVariables);

            if (namedObject.SourceType == SourceType.FlatRedBallType && 
                namedObject.SourceClassType == "PositionedObjectList<T>" &&
                namedObject.SourceClassGenericType != null)
            {
                AddVariablesForPositionedObjectList(namedObject, gumVariables);
            }

            if (namedObject.SourceType == SourceType.FlatRedBallType &&
                namedObject.SourceClassType == "ShapeCollection")
            {
                AddVariablesForShapeCollection(namedObject, gumVariables);
            }

            if (parentNamedObject != null)
            {
                var parentVariable = new VariableSave();
                parentVariable.Value = parentNamedObject.InstanceName;
                parentVariable.SetsValue = true;
                parentVariable.Type = "string";
                parentVariable.Name = $"{namedObject.InstanceName}.Parent";
                gumVariables.Add(parentVariable);

            }


            // everything should set value
            foreach (var gumVariable in gumVariables)
            {
                gumVariable.SetsValue = true;
            }

        }

        private static void AddVariablesForShapeCollection(NamedObjectSave namedObject, List<VariableSave> gumVariables)
        {
            AddVariablesForContainer(namedObject, gumVariables);
        }

        private static void AddVariablesForPositionedObjectList(NamedObjectSave namedObject, List<VariableSave> gumVariables)
        {
            var type = ConvertEntityToGumComponent(namedObject.SourceClassGenericType);

            if (string.IsNullOrEmpty(type))
            {
                type = ConvertFlatRedBallPrimitiveTypeToGumPrimitive(namedObject.SourceClassGenericType);
            }

            AddVariablesForContainer(namedObject, gumVariables);

            if (!string.IsNullOrEmpty(type))
            {
                var variableSave = new VariableSave();
                variableSave.Value = type;
                variableSave.SetsValue = true;
                variableSave.Type = "string";
                variableSave.Name = $"{namedObject.InstanceName}.Contained Type";
                gumVariables.Add(variableSave);
            }
        }

        private static void AddVariablesForContainer(NamedObjectSave namedObject, List<VariableSave> gumVariables)
        {
            var widthUnits = new VariableSave();
            widthUnits.Value = DimensionUnitType.RelativeToContainer;
            widthUnits.SetsValue = true;
            widthUnits.Type = nameof(DimensionUnitType);
            widthUnits.Name = $"{namedObject.InstanceName}.Width Units";
            gumVariables.Add(widthUnits);

            var width = new VariableSave();
            width.Value = 0.0f;
            width.SetsValue = true;
            width.Type = "float";
            width.Name = $"{namedObject.InstanceName}.Width";
            gumVariables.Add(width);

            var heightUnits = new VariableSave();
            heightUnits.Value = DimensionUnitType.RelativeToContainer;
            heightUnits.SetsValue = true;
            heightUnits.Type = nameof(DimensionUnitType);
            heightUnits.Name = $"{namedObject.InstanceName}.Height Units";
            gumVariables.Add(heightUnits);

            var height = new VariableSave();
            height.Value = 0.0f;
            height.SetsValue = true;
            height.Type = "float";
            height.Name = $"{namedObject.InstanceName}.Height";
            gumVariables.Add(height);

            var x = new VariableSave();
            x.Value = 0.0f;
            x.SetsValue = true;
            x.Type = "float";
            x.Name = $"{namedObject.InstanceName}.X";
            gumVariables.Add(x);

            var y = new VariableSave();
            y.Value = 0.0f;
            y.SetsValue = true;
            y.Type = "float";
            y.Name = $"{namedObject.InstanceName}.Y";
            gumVariables.Add(y);
        }

        private void AddGumVariables(InstructionSave glueVariable, NamedObjectSave namedObject,
            GlueElement glueElement, 
            List<VariableSave> gumVariables, 
            List<VariableListSave> gumVariableLists,
            VariableGroupDictionary variableGroups, bool isInState = false)
        {
            string glueVariableName = glueVariable.Member;

            if(isInState)
            {
                // It's a state variable so it references a different variable that we have to find:
                var variableOnElement = glueElement.GetCustomVariable(glueVariable.Member);

                if(variableOnElement != null)
                {
                    glueVariableName = variableOnElement.SourceObjectProperty;
                }


                if(!string.IsNullOrEmpty( variableOnElement.SourceObject ))
                {
                    namedObject = glueElement.AllNamedObjects
                        .FirstOrDefault(item => item.InstanceName == variableOnElement.SourceObject);
                }
            }

            // Let's be explicit instead of expecting the names to match up:
            switch(glueVariableName)
            {
                case "Height":
                    {
                        var variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.Height";
                        variableSave.Type = "float";
                        variableSave.Value = (float)glueVariable.Value;
                        gumVariables.Add(variableSave);
                    }
                    break;
                case "Points":
                    {
                        var variableListSave = new VariableListSave<Vector2>();
                        variableListSave.Name = $"{namedObject.InstanceName}.Points";

                        var pointsPositiveYUp = glueVariable.Value as List<Vector2>;



                        if(pointsPositiveYUp != null)
                        {
                            foreach(var frbPoint in pointsPositiveYUp)
                            {
                                var newPoint = frbPoint;
                                newPoint.Y *= -1;

                                variableListSave.ValueAsIList.Add(newPoint);
                            }
                        }
                        gumVariableLists.Add(variableListSave);
                    }
                    break;
                case "Radius":
                    {
                        if(namedObject.SourceType == SourceType.FlatRedBallType && namedObject.SourceClassType == "Circle")
                        {
                            var variableSave = new VariableSave();
                            variableSave.Name = $"{namedObject.InstanceName}.Width";
                            variableSave.Type = "float";
                            variableSave.Value = (float)glueVariable.Value * 2.0f;
                            variableSave.IsHiddenInPropertyGrid = true;
                            gumVariables.Add(variableSave);

                            variableSave = new VariableSave();
                            variableSave.Name = $"{namedObject.InstanceName}.Height";
                            variableSave.Type = "float";
                            variableSave.Value = (float)glueVariable.Value * 2.0f;
                            variableSave.IsHiddenInPropertyGrid = true;
                            gumVariables.Add(variableSave);


                            variableSave = new VariableSave();
                            variableSave.Name = $"{namedObject.InstanceName}.Radius";
                            variableSave.Type = "float";
                            variableSave.Value = (float)glueVariable.Value;
                            gumVariables.Add(variableSave);

                        }
                    }
                    break;

                case "RotationZ":
                    {
                        var variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.Rotation";
                        variableSave.Type = "float";
                        var valueRadians = (float)glueVariable.Value;
                        var degrees = 360 * (valueRadians / (2 * Math.PI));
                        variableSave.Value = (float)degrees;
                        gumVariables.Add(variableSave);
                    }

                    break;

                case "RightTexturePixel":
                case "LeftTexturePixel":
                case "BottomTexturePixel":
                case "TopTexturePixel":
                    variableGroups.AddVariable(glueVariable, namedObject, 
                        glueVariableName, GlueToGumTextureCoordinateConversionLogic.TextureCoordinatesCategory);
                    break;
                case "Texture":
                    {
                        var variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.SourceFile";
                        variableSave.Type = "string";

                        //var referencedFileName = (string)glueVariable.Value;
                        //var nos = glueElement.GetReferencedFileSave(referencedFileName);

                        //if(nos == null)
                        //{
                        //    // todo - need to look in global content;
                        //}

                        // assume the content location is in a monogame DGL location, and the
                        // file is a PNG. Eventually we can make this more intelligent
                        var fileName = $"../Content/{glueElement.Name}/{(string)glueVariable.Value}.png";

                        variableSave.Value = fileName;
                        variableSave.IsFile = true;
                        gumVariables.Add(variableSave);

                        variableGroups.AddVariable(glueVariable, namedObject, glueVariableName,
                            GlueToGumTextureCoordinateConversionLogic.TextureCoordinatesCategory);
                    }
                    break;
                case "TextureScale":
                    {
                        var variableSave = new VariableSave();

                        variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.Width";
                        variableSave.Type = "float";
                        variableSave.Value = (float)glueVariable.Value * 100;
                        gumVariables.Add(variableSave);


                        variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.Height";
                        variableSave.Type = "float";
                        variableSave.Value = (float)glueVariable.Value * 100;
                        gumVariables.Add(variableSave);

                        // todo width units?
                    }
                    break;
                case "X":
                    {
                        VariableSave variableSave = null;
                        variableSave = new VariableSave();
                        variableSave.Type = "float";
                        variableSave.Name = $"{namedObject.InstanceName}.X";
                        variableSave.Value = (float)glueVariable.Value;
                        gumVariables.Add(variableSave);

                    }
                    break;
                case "Width":
                    {

                        var variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.Width";
                        variableSave.Type = "float";
                        variableSave.Value = (float)glueVariable.Value;
                        gumVariables.Add(variableSave);
                    }
                    break;
                case "Y":
                    {
                        var variableSave = new VariableSave();
                        variableSave.Name = $"{namedObject.InstanceName}.Y";
                        variableSave.Type = "float";
                        variableSave.Value = (float)glueVariable.Value;
                        gumVariables.Add(variableSave);
                    }
                    break;
                default:
                    int m = 3;
                    break;
            }
        }

        private InstanceSave ToInstanceSave(NamedObjectSave namedObject)
        {
            InstanceSave instanceSave = new InstanceSave();
            instanceSave.Name = namedObject.InstanceName;
            instanceSave.BaseType = GetGumBaseType(namedObject);
            instanceSave.DefinedByBase = namedObject.DefinedByBase;

            if(namedObject.SourceType == SourceType.FlatRedBallType && 

                (namedObject.SourceClassType == "PositionedObjectList<T>" ||
                 namedObject.SourceClassType == "ShapeCollection"
                ))
            {
                // so user can't select and move it around accidentally
                instanceSave.Locked = true;
            }


            return instanceSave;
        }

        private string GetGumBaseType(NamedObjectSave namedObject)
        {
            string gumType = null;

            switch(namedObject.SourceType)
            {
                case SourceType.FlatRedBallType:
                    gumType = ConvertFlatRedBallPrimitiveTypeToGumPrimitive(namedObject.SourceClassType);
                    break;
                case SourceType.Entity:
                    gumType = ConvertEntityToGumComponent(namedObject.SourceClassType);
                    break;
            }

            if(gumType == null)
            {
                gumType = "Container";
            }

            return gumType;
        }

        private static string ConvertEntityToGumComponent(string glueType)
        {
            string gumType = null;
            if (glueType?.StartsWith("Entities\\") == true)
            {
                gumType = glueType.Substring("Entities\\".Length);
            }

            return gumType;
        }

        private static string ConvertFlatRedBallPrimitiveTypeToGumPrimitive(string glueType)
        {
            string gumType = null;
            switch (glueType)
            {
                case "Circle":
                    gumType = "Circle";
                    break;
                case "AxisAlignedRectangle":
                    gumType = "Rectangle";
                    break;
                case "Sprite":
                case "FlatRedBall.Sprite":
                    gumType = "Sprite";
                    break;
                case "PositionedObjectList":
                case "ShapeCollection":
                    gumType = "Container";
                    break;
                case "Polygon":
                case "FlatRedBall.Math.Geometry.Polygon":
                    gumType = "Polygon";
                    break;
            }

            return gumType;
        }
    }
}
