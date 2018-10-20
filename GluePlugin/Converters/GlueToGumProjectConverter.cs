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
using GumScreen = Gum.DataTypes.ScreenSave;

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


            AdjustStandardsToMatchFrb(gumProject);

            GumCommands.Self.GuiCommands.PrintOutput($"Created Gum project from Glue project");

            return gumProject;
        }

        private void AdjustStandardsToMatchFrb(GumProjectSave gumProject)
        {
            foreach (var standardElement in gumProject.StandardElements)
            {
                if(standardElement.Name == "Circle")
                {
                    standardElement.DefaultState.SetValue("Width", 32.0f, "float");
                    standardElement.DefaultState.SetValue("Height", 32.0f, "float");
                    standardElement.DefaultState.SetValue("Radius", 32.0f, "float");
                }




                foreach(var state in standardElement.AllStates)
                {
                    state.SetValue("X Origin", HorizontalAlignment.Center, nameof(HorizontalAlignment));
                    state.SetValue("Y Origin", VerticalAlignment.Center, nameof(VerticalAlignment));

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

            AddInstances(glueScreen, gumScreen);

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

            return component;
        }

        private ElementReference ToComponentReference(EntitySave entitySave)
        {
            var elementReference = new ElementReference();
            elementReference.ElementType = ElementType.Component;
            elementReference.Name = entitySave.Name.Substring(
                "Entities\\".Length);

            return elementReference;
        }

        private void AddInstances(IElement glueElement, ElementSave gumElement)
        {
            foreach (var namedObject in glueElement.NamedObjects)
            {
                var instance = ToInstanceSave(namedObject);

                var variableList = GetVariableSaves(namedObject, glueElement);
                gumElement.DefaultState.Variables.AddRange(variableList);

                gumElement.Instances.Add(instance);

                if (namedObject.IsList)
                {
                    foreach (var containedObject in namedObject.ContainedObjects)
                    {
                        var containedInstance = ToInstanceSave(containedObject);

                        // todo - add it and make it a child
                    }
                }
            }
        }

        private List<VariableSave> GetVariableSaves(NamedObjectSave namedObject, IElement glueElement)
        {
            List<VariableSave> gumVariables = new List<VariableSave>();
            foreach(var glueVariable in namedObject.InstructionSaves)
            {
                AddGumVariables(glueVariable, namedObject, glueElement, gumVariables);
            }

            // everything should set value
            foreach(var gumVariable in gumVariables)
            {
                gumVariable.SetsValue = true;
            }

            return gumVariables;
        }

        private void AddGumVariables(CustomVariableInNamedObject glueVariable, NamedObjectSave namedObject, IElement glueElement, List<VariableSave> gumVariables)
        {

            // Let's be explicit instead of expecting the names to match up:
            switch(glueVariable.Member)
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
            return instanceSave;
        }

        private string GetGumBaseType(NamedObjectSave namedObject)
        {
            string gumType = null;

            switch(namedObject.SourceType)
            {
                case SourceType.FlatRedBallType:
                    switch(namedObject.SourceClassType)
                    {
                        case "Circle":
                            gumType = "Circle";
                            break;
                        case "AxisAlignedRectangle":
                            gumType = "Rectangle";
                            break;
                        case "Sprite":
                            gumType = "Sprite";
                            break;
                    }
                    break;
                case SourceType.Entity:
                    if(namedObject.SourceClassType?.StartsWith("Entities\\") == true)
                    {
                        gumType = namedObject.SourceClassType.Substring("Entities\\".Length);
                    }
                    break;
            }

            if(gumType == null)
            {
                gumType = "Container";
            }

            return gumType;
        }

    }
}
