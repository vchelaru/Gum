using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Gum.RenderingLibrary;
using Gum.Converters;
using RenderingLibrary.Content;
using CommonFormsAndControls.Forms;
using ToolsUtilities;
using Microsoft.Xna.Framework.Graphics;
using RenderingLibrary.Graphics;

namespace Gum.PropertyGridHelpers
{
    public class SetVariableLogic : Singleton<SetVariableLogic>
    {
        internal void PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;

            PropertyValueChanged(changedMember, oldValue);
        }

        public void PropertyValueChanged(string changedMember, object oldValue, bool refresh = true)
        {
            var selectedStateSave = SelectedState.Self.SelectedStateSave;

            ElementSave parentElement = null;
            InstanceSave instance = null;

            if (selectedStateSave != null)
            {
                parentElement = selectedStateSave.ParentContainer;
                instance = SelectedState.Self.SelectedInstance;

                if (instance != null)
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(instance.Name + "." + changedMember);
                }
                else
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(changedMember);
                }
            }
            PropertyValueChanged(changedMember, oldValue, parentElement, instance, refresh);
        }

        public void PropertyValueChanged(string changedMember, object oldValue, ElementSave parentElement, InstanceSave instance, bool refresh)
        {
            if (parentElement != null)
            {

                ReactToChangedMember(changedMember, oldValue, parentElement, instance);

                // Need to record undo before refreshing and reselecting the UI
                Undo.UndoManager.Self.RecordUndo();

                if (refresh)
                {
                    GumCommands.Self.GuiCommands.RefreshElementTreeView(parentElement);
                }


                if (refresh)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

                    // Inefficient but let's do this for now - we can make it more efficient later
                    WireframeObjectManager.Self.RefreshAll(true);
                    SelectionManager.Self.Refresh();
                }
            }
        }

        private void ReactToChangedMember(string changedMember, object oldValue, ElementSave parentElement, InstanceSave instance)
        {
            ReactIfChangedMemberIsName(parentElement, instance, changedMember, oldValue);

            ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsFont(parentElement, instance, changedMember, oldValue);

            ReactIfChangedMemberIsCustomFont(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsUnitType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsTexture(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsTextureAddress(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsParent(parentElement, instance, changedMember, oldValue);

            PluginManager.Self.VariableSet(parentElement, instance, changedMember, oldValue);
        }

        private static void ReactIfChangedMemberIsName(ElementSave container, InstanceSave instance, string changedMember, object oldValue)
        {
            if (changedMember == "Name")
            {
                RenameManager.Self.HandleRename(container, instance, (string)oldValue);
            }
        }

        private static void ReactIfChangedMemberIsBaseType(object s, string changedMember, object oldValue)
        {
            if (changedMember == "Base Type")
            {
                ElementSave asElementSave = s as ElementSave;

                asElementSave.ReactToChangedBaseType(SelectedState.Self.SelectedInstance, oldValue.ToString());
            }
        }

        private void ReactIfChangedMemberIsFont(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            if (changedMember == "Font" || changedMember == "FontSize")
            {
                FontManager.Self.ReactToFontValueSet(instance);
            }
        }

        private void ReactIfChangedMemberIsCustomFont(ElementSave parentElement, string changedMember, object oldValue)
        {
            // FIXME: This react needs a proper if condition
            //PropertyGridManager.Self.RefreshUI(force: true);
        }

        private void ReactIfChangedMemberIsUnitType(ElementSave parentElement, string changedMember, object oldValueAsObject)
        {
            bool wasAnythingSet = false;
            string variableToSet = null;
            StateSave stateSave = SelectedState.Self.SelectedStateSave;
            float valueToSet = 0;

            if (changedMember == "X Units" || changedMember == "Y Units" || changedMember == "Width Units" || changedMember == "Height Units")
            {
                GeneralUnitType oldValue;

                if (UnitConverter.TryConvertToGeneralUnit(oldValueAsObject, out oldValue))
                {



                    IRenderableIpso currentIpso =
                        WireframeObjectManager.Self.GetSelectedRepresentation();

                    float parentWidth = ObjectFinder.Self.GumProjectSave.DefaultCanvasWidth;
                    float parentHeight = ObjectFinder.Self.GumProjectSave.DefaultCanvasHeight;

                    float fileWidth = 0;
                    float fileHeight = 0;

                    if (currentIpso != null)
                    {
                        currentIpso.GetFileWidthAndHeight(out fileWidth, out fileHeight);
                        if (currentIpso.Parent != null)
                        {
                            parentWidth = currentIpso.Parent.Width;
                            parentHeight = currentIpso.Parent.Height;
                        }
                    }


                    float outX = 0;
                    float outY = 0;

                    bool isWidthOrHeight = false;

                    
                    object unitTypeAsObject = EditingManager.GetCurrentValueForVariable(changedMember, SelectedState.Self.SelectedInstance);
                    GeneralUnitType unitType = UnitConverter.ConvertToGeneralUnit(unitTypeAsObject);


                    XOrY xOrY = XOrY.X;
                    if (changedMember == "X Units")
                    {
                        variableToSet = "X";
                        xOrY = XOrY.X;
                    }
                    else if (changedMember == "Y Units")
                    {
                        variableToSet = "Y";
                        xOrY = XOrY.Y;
                    }
                    else if (changedMember == "Width Units")
                    {
                        variableToSet = "Width";
                        isWidthOrHeight = true;
                        xOrY = XOrY.X;

                    }
                    else if (changedMember == "Height Units")
                    {
                        variableToSet = "Height";
                        isWidthOrHeight = true;
                        xOrY = XOrY.Y;
                    }



                    float valueOnObject = 0;
                    if (stateSave.TryGetValue<float>(GetQualifiedName(variableToSet), out valueOnObject))
                    {

                        var defaultUnitType = GeneralUnitType.PixelsFromSmall;

                        if (xOrY == XOrY.X)
                        {
                            UnitConverter.Self.ConvertToPixelCoordinates(
                                valueOnObject, 0, oldValue, defaultUnitType, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                            UnitConverter.Self.ConvertToUnitTypeCoordinates(
                                outX, outY, unitType, defaultUnitType, parentWidth, parentHeight, fileWidth, fileHeight, out valueToSet, out outY);
                        }
                        else
                        {
                            UnitConverter.Self.ConvertToPixelCoordinates(
                                0, valueOnObject, defaultUnitType, oldValue, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                            UnitConverter.Self.ConvertToUnitTypeCoordinates(
                                outX, outY, defaultUnitType, unitType, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out valueToSet);
                        }
                        wasAnythingSet = true;
                    }
                }
            }

            if (wasAnythingSet)
            {
                InstanceSave instanceSave = SelectedState.Self.SelectedInstance;
                if (SelectedState.Self.SelectedInstance != null)
                {
                    variableToSet = SelectedState.Self.SelectedInstance.Name + "." + variableToSet;
                }

                stateSave.SetValue(variableToSet, valueToSet, instanceSave);
            }
        }

        private void ReactIfChangedMemberIsTexture(ElementSave parentElement, string changedMember, object oldValue)
        {
            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables

            

            if (variable != null && variable.GetRootName() == "SourceFile")
            {
                string value = variable.Value as string;

                var extension = FileManager.GetExtension(value);

                var isValidExtension = extension == "gif" ||
                    extension == "jpg" ||
                    extension == "png";

                if (isValidExtension)
                {

                    if (!string.IsNullOrEmpty(value))
                    {
                        // See if this is relative to the project
                        bool isRelativeToProject = !value.StartsWith("../") && !value.StartsWith("..\\");

                        if (!isRelativeToProject)
                        {
                            TryCopyFileLocally(variable, value);
                        }
                    }


                    StateSave stateSave = SelectedState.Self.SelectedStateSave;

                    RecursiveVariableFinder rvf = new RecursiveVariableFinder(stateSave);

                    stateSave.SetValue("AnimationFrames", new List<string>());
                }
                else
                {

                    MessageBox.Show("The extension " + extension + " is not supported for textures");

                    variable.Value = oldValue;
                }
            }

        }

        private static void TryCopyFileLocally(VariableSave variable, string value)
        {
            // Ask the user what to do - make it relative?
            MultiButtonMessageBox mbmb = new
                MultiButtonMessageBox();

            mbmb.MessageText = "The file\n" + value + "\nis not relative to the project.  What would you like to do?";
            mbmb.AddButton("Reference the file in its current location", DialogResult.OK);
            mbmb.AddButton("Copy the file relative to the Gum project and reference the copy", DialogResult.Yes);

            var dialogResult = mbmb.ShowDialog();

            bool shouldCopy = false;

            string directory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);
            string targetAbsoluteFile = directory + FileManager.RemovePath(value);

            if (dialogResult == DialogResult.Yes)
            {
                shouldCopy = true;

                // If the destination already exists, we gotta ask the user what they want to do.
                if (System.IO.File.Exists(targetAbsoluteFile))
                {
                    mbmb = new MultiButtonMessageBox();
                    mbmb.MessageText = "The destination file already exists.  Would you like to overwrite it?";
                    mbmb.AddButton("Yes", DialogResult.Yes);
                    mbmb.AddButton("No, use the original file", DialogResult.No);

                    shouldCopy = mbmb.ShowDialog() == DialogResult.Yes;
                }

            }

            if (shouldCopy)
            {

                try
                {

                    string sourceAbsoluteFile =
                        directory + value;
                    sourceAbsoluteFile = FileManager.RemoveDotDotSlash(sourceAbsoluteFile);

                    System.IO.File.Copy(sourceAbsoluteFile, targetAbsoluteFile, overwrite: true);

                    variable.Value = FileManager.RemovePath(value);

                }
                catch (Exception e)
                {
                    MessageBox.Show("Error copying file:\n" + e.ToString());
                }
            }
        }

        private void ReactIfChangedMemberIsParent(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            bool isValidAssignment = true;

            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && changedMember == "Parent")
            {
                if ((variable.Value as string) == "<NONE>")
                {
                    variable.Value = null;
                }

                if(variable.Value != null)
                {
                    var newParent = parentElement.Instances.FirstOrDefault(item => item.Name == variable.Value as string);
                    var newValue = variable.Value;
                    // unset it before finding recursive children, in case there is a circular reference:
                    variable.Value = null;
                    var childrenInstances = GetRecursiveChildrenOf(parentElement, instance);

                    if(childrenInstances.Contains(newParent))
                    {
                        // uh oh, circular referenced detected, don't allow it!
                        MessageBox.Show("This parent assignment would produce a circular reference, which is not allowed.");
                        variable.Value = oldValue;
                        isValidAssignment = false;
                    }
                    else
                    {
                        // set it back:
                        variable.Value = newValue;
                    }
                }

                if(isValidAssignment)
                {
                    GumCommands.Self.GuiCommands.RefreshElementTreeView(parentElement);
                }
                else
                {
                    GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                }
            }
        }

        private List<InstanceSave> GetRecursiveChildrenOf(ElementSave parent, InstanceSave instance)
        {
            var defaultState = parent.DefaultState;
            List<InstanceSave> toReturn = new List<InstanceSave>();
            List<InstanceSave> directChildren = new List<InstanceSave>();
            foreach(var potentialChild in parent.Instances)
            {
                var foundParentVariable = defaultState.Variables
                    .FirstOrDefault(item => item.Name == $"{potentialChild.Name}.Parent" && item.Value as string == instance.Name);

                if(foundParentVariable != null)
                {
                    directChildren.Add(potentialChild);
                }
            }

            toReturn.AddRange(directChildren);

            foreach(var child in directChildren)
            {
                var childrenOfChild = GetRecursiveChildrenOf(parent, child);
                toReturn.AddRange(childrenOfChild);
            }

            return toReturn;
        }

        private void ReactIfChangedMemberIsTextureAddress(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "Texture Address")
            {
                RecursiveVariableFinder rvf;

                var instance = SelectedState.Self.SelectedInstance;
                if (instance != null)
                {
                    rvf = new RecursiveVariableFinder(SelectedState.Self.SelectedInstance, parentElement);
                }
                else
                {
                    rvf = new RecursiveVariableFinder(parentElement.DefaultState);
                }

                var textureAddress = rvf.GetValue<TextureAddress>("Texture Address");

                if (textureAddress == TextureAddress.Custom)
                {
                    string sourceFile = rvf.GetValue<string>("SourceFile");

                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        string absolute = ProjectManager.Self.MakeAbsoluteIfNecessary(sourceFile);

                        if (System.IO.File.Exists(absolute))
                        {
                            var texture = LoaderManager.Self.LoadContent<Texture2D>(absolute);

                            if (texture != null && instance != null)
                            {
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Top", 0);
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Left", 0);
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Width", texture.Width);
                                parentElement.DefaultState.SetValue(instance.Name + ".Texture Height", texture.Height);
                            }
                        }
                    }
                }
                if (textureAddress == TextureAddress.DimensionsBased)
                {
                    // if the values are 0, then we should set them to 1:
                    float widthScale = rvf.GetValue<float>("Texture Width Scale");
                    float heightScale = rvf.GetValue<float>("Texture Height Scale");

                    if (widthScale == 0)
                    {
                        if (instance != null)
                        {
                            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Texture Width Scale", 1.0f);
                        }
                        else
                        {
                            SelectedState.Self.SelectedStateSave.SetValue("Texture Width Scale", 1.0f);
                        }
                    }

                    if (heightScale == 0)
                    {
                        if (instance != null)
                        {
                            SelectedState.Self.SelectedStateSave.SetValue(instance.Name + ".Texture Height Scale", 1.0f);
                        }
                        else
                        {
                            SelectedState.Self.SelectedStateSave.SetValue("Texture Height Scale", 1.0f);
                        }
                    }
                }
            }
        }

        string GetQualifiedName(string variableName)
        {
            if (SelectedState.Self.SelectedInstance != null)
            {
                return SelectedState.Self.SelectedInstance.Name + "." + variableName;
            }
            else
            {
                return variableName;
            }
        }

    }
}
