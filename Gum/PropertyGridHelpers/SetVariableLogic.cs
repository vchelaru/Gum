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
            object selectedObject = SelectedState.Self.SelectedStateSave;

            // We used to suppress
            // saving - not sure why.
            //bool saveProject = true;

            if (selectedObject is StateSave)
            {
                ElementSave parentElement = ((StateSave)selectedObject).ParentContainer;
                InstanceSave instance = SelectedState.Self.SelectedInstance;

                if (instance != null)
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(instance.Name + "." + changedMember);
                }
                else
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(changedMember);
                }
                // Why do we do this before reacting to names?  I think we want to do it after
                //ElementTreeViewManager.Self.RefreshUI();
                ReactToChangedMember(changedMember, oldValue, parentElement, instance);



                // This used to be above the React methods but
                // we probably want to referesh the UI after everything
                // else has changed, don't we?
                // I think this code makes things REALLY slow - we only want to refresh one of the tree nodes:
                //ElementTreeViewManager.Self.RefreshUI();

                if (refresh)
                {
                    GumCommands.Self.GuiCommands.RefreshElementTreeView(SelectedState.Self.SelectedElement);
                }
            }

            if (refresh)
            {
                // Save the change
                if (SelectedState.Self.SelectedElement != null)
                {
                    GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();
                }


                // Inefficient but let's do this for now - we can make it more efficient later
                WireframeObjectManager.Self.RefreshAll(true);
                SelectionManager.Self.Refresh();
            }
        }

        private void ReactToChangedMember(string changedMember, object oldValue, ElementSave parentElement, InstanceSave instance)
        {
            ReactIfChangedMemberIsName(parentElement, instance, changedMember, oldValue);

            ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsFont(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsCustomFont(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsUnitType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsTexture(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsTextureAddress(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsParent(parentElement, changedMember, oldValue);

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

        private void ReactIfChangedMemberIsFont(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "Font" || changedMember == "FontSize")
            {
                FontManager.Self.ReactToFontValueSet();
            }
        }

        private void ReactIfChangedMemberIsCustomFont(ElementSave parentElement, string changedMember, object oldValue)
        {
            // FIXME: This react needs a proper if condition
            //PropertyGridManager.Self.RefreshUI(force: true);
        }

        private void ReactIfChangedMemberIsUnitType(ElementSave parentElement, string changedMember, object oldValue)
        {
            StateSave stateSave = SelectedState.Self.SelectedStateSave;

            IPositionedSizedObject currentIpso =
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
            float valueToSet = 0;
            string variableToSet = null;

            bool isWidthOrHeight = false;

            bool wasAnythingSet = false;

            if (changedMember == "X Units" || changedMember == "Y Units" || changedMember == "Width Units" || changedMember == "Height Units")
            {
                object unitType = EditingManager.GetCurrentValueForVariable(changedMember, SelectedState.Self.SelectedInstance);
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
                if(stateSave.TryGetValue<float>(GetQualifiedName(variableToSet), out valueOnObject))
                {

                    if (xOrY == XOrY.X)
                    {
                        UnitConverter.Self.ConvertToPixelCoordinates(
                            valueOnObject, 0, oldValue, null, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                        UnitConverter.Self.ConvertToUnitTypeCoordinates(
                            outX, outY, unitType, null, parentWidth, parentHeight, fileWidth, fileHeight, out valueToSet, out outY);
                    }
                    else
                    {
                        UnitConverter.Self.ConvertToPixelCoordinates(
                            0, valueOnObject, null, oldValue, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                        UnitConverter.Self.ConvertToUnitTypeCoordinates(
                            outX, outY, null, unitType, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out valueToSet);
                    }
                    wasAnythingSet = true;
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

                if(!string.IsNullOrEmpty(value))
                {
                    // See if this is relative to the project
                    bool isRelativeToProject = !value.StartsWith("../") && !value.StartsWith("..\\");

                    if (!isRelativeToProject)
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

                        if(dialogResult == DialogResult.Yes)
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

                                System.IO.File.Copy(sourceAbsoluteFile, targetAbsoluteFile, overwrite:true);

                                variable.Value = FileManager.RemovePath(value);

                            }
                            catch (Exception e)
                            {
                                MessageBox.Show("Error copying file:\n" + e.ToString());
                            }
                        }
                    }



                }


                StateSave stateSave = SelectedState.Self.SelectedStateSave;

                RecursiveVariableFinder rvf = new RecursiveVariableFinder(stateSave);

                stateSave.SetValue("AnimationFrames", new List<string>());

            }

        }

        private void ReactIfChangedMemberIsParent(ElementSave parentElement, string changedMember, object oldValue)
        {
            VariableSave variable = SelectedState.Self.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && changedMember == "Parent")
            {
                if ((variable.Value as string) == "<NONE>")
                {
                    variable.Value = null;
                }

                GumCommands.Self.GuiCommands.RefreshElementTreeView(parentElement);
            }
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
                            var texture = LoaderManager.Self.Load(absolute, null);

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
