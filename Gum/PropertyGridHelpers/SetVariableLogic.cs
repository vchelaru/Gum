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
using Gum.Logic;

namespace Gum.PropertyGridHelpers
{
    public class SetVariableLogic : Singleton<SetVariableLogic>
    {

        public bool AttemptToPersistPositionsOnUnitChanges { get; set; } = true;

        static HashSet<string> PropertiesSupportingIncrementalChange = new HashSet<string>
        {
            "Animate",
            "Alpha",
            "Blue",
            "CurrentChainName",
            "FlipHorizontal",
            "FontSize",
            "Green",
            "Height",
            "Height Units",

            "MaxLettersToShow",
            "Red",
            "Rotation",
            "Text",
            "Texture Address",
            "Width",
            "Width Units",
            "X",
            "X Origin",
            "X Units",
            "Y",
            "Y Origin",
            "Y Units",
        };

        internal void PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            string changedMember = e.ChangedItem.PropertyDescriptor.Name;
            object oldValue = e.OldValue;

            PropertyValueChanged(changedMember, oldValue, SelectedState.Self.SelectedInstance);
        }

        // added instance property so we can change values even if a tree view is selected
        public void PropertyValueChanged(string unqualifiedMemberName, object oldValue, InstanceSave instance, bool refresh = true)
        {
            var selectedStateSave = SelectedState.Self.SelectedStateSave;

            ElementSave parentElement = null;

            if (selectedStateSave != null)
            {
                parentElement = selectedStateSave.ParentContainer;

                if (instance != null)
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(instance.Name + "." + unqualifiedMemberName);
                }
                else
                {
                    SelectedState.Self.SelectedVariableSave = SelectedState.Self.SelectedStateSave.GetVariableSave(unqualifiedMemberName);
                }
            }
            ReactToPropertyValueChanged(unqualifiedMemberName, oldValue, parentElement, instance, refresh);
        }

        /// <summary>
        /// Reacts to a variable having been set.
        /// </summary>
        /// <param name="unqualifiedMember">The variable name without the prefix instance name.</param>
        /// <param name="oldValue"></param>
        /// <param name="parentElement"></param>
        /// <param name="instance"></param>
        /// <param name="refresh"></param>
        public void ReactToPropertyValueChanged(string unqualifiedMember, object oldValue, ElementSave parentElement, InstanceSave instance, bool refresh)
        {
            if (parentElement != null)
            {

                ReactToChangedMember(unqualifiedMember, oldValue, parentElement, instance);

                string qualifiedName = unqualifiedMember;
                if(instance != null)
                {
                    qualifiedName = $"{instance.Name}.{unqualifiedMember}";
                }
                VariableInCategoryPropagationLogic.Self.PropagateVariablesInCategory(qualifiedName);

                // Need to record undo before refreshing and reselecting the UI
                Undo.UndoManager.Self.RecordUndo();

                if (refresh)
                {
                    // These properties may require some changes to the grid, so we refresh the tree view
                    // and entire grid.
                    // There's lots of work that can/should be done here:
                    // 1. We should have the plugins that handle excluding variables also
                    //    report whether a variable requires refreshing
                    // 2. We could only refresh the grid for some variables like UseCustomFont
                    // 3. We could have only certain variable refresh themselves instead of the entire 
                    //    grid.
                    var needsToRefreshEntireElement = 
                        unqualifiedMember == "Parent" || 
                        unqualifiedMember == "Name" ||
                        unqualifiedMember == "UseCustomFont" ||
                        unqualifiedMember == "Texture Address" ||
                        unqualifiedMember == "Base Type"
                        ;
                    if(needsToRefreshEntireElement)
                    {
                        GumCommands.Self.GuiCommands.RefreshElementTreeView(parentElement);
                        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                    }
                }


                if (refresh)
                {
                    var value = SelectedState.Self.SelectedStateSave.GetValue(qualifiedName);

                    var areSame = value == null && oldValue == null;
                    if (!areSame && value != null)
                    {
                        areSame = value.Equals(oldValue);
                    }

                    // If the values are the same they may have been set to be the same by a plugin that
                    // didn't allow the assignment, so don't go through the work of saving and refreshing
                    if (!areSame)
                    {
                        GumCommands.Self.FileCommands.TryAutoSaveCurrentElement();

                        // Inefficient but let's do this for now - we can make it more efficient later
                        // November 19, 2019
                        // While this is inefficient
                        // at runtime, it is *really*
                        // inefficient for debugging. If
                        // a set value fails, we have to trace
                        // the entire variable assignment and that
                        // can take forever. Therefore, we're going to
                        // migrate towards setting the individual values
                        // here. This can expand over time to just exclude
                        // the RefreshAll call completely....but I don't know
                        // if that will cause problems now, so instead I'm going
                        // to do it one by one:
                        var handledByDirectSet = false;
                        if (PropertiesSupportingIncrementalChange.Contains(unqualifiedMember) && 
                            (instance != null || SelectedState.Self.SelectedComponent != null || SelectedState.Self.SelectedStandardElement != null))
                        {
                            // this assumes that the object having its variable set is the selected instance. If we're setting
                            // an exposed variable, this is not the case - the object having its variable set is actually the instance.
                            //GraphicalUiElement gue = WireframeObjectManager.Self.GetSelectedRepresentation();
                            GraphicalUiElement gue = null;
                            if (instance != null)
                            {
                                gue = WireframeObjectManager.Self.GetRepresentation(instance);
                            }
                            else
                            {
                                gue = WireframeObjectManager.Self.GetSelectedRepresentation();
                            }

                            if (gue != null)
                            {
                                gue.SetProperty(unqualifiedMember, value);
                                handledByDirectSet = true;
                            }
                            if(unqualifiedMember == "Text" && LocalizationManager.HasDatabase)
                            {
                                WireframeObjectManager.Self.ApplyLocalization(gue, value as string);
                            }
                        }
                        
                        if(!handledByDirectSet)
                        {
                            WireframeObjectManager.Self.RefreshAll(true, forceReloadTextures:false);
                        }


                        SelectionManager.Self.Refresh();
                    }
                }
            }
        }



        private void ReactToChangedMember(string changedMember, object oldValue, ElementSave parentElement, InstanceSave instance)
        {
            ReactIfChangedMemberIsName(parentElement, instance, changedMember, oldValue);

            // Handled in a plugin
            //ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsFont(parentElement, instance, changedMember, oldValue);

            ReactIfChangedMemberIsCustomFont(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsUnitType(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsSourceFile(parentElement, instance, changedMember, oldValue);

            ReactIfChangedMemberIsTextureAddress(parentElement, changedMember, oldValue);

            ReactIfChangedMemberIsParent(parentElement, instance, changedMember, oldValue);

            PluginManager.Self.VariableSet(parentElement, instance, changedMember, oldValue);
        }



        private static void ReactIfChangedMemberIsName(ElementSave container, InstanceSave instance, string changedMember, object oldValue)
        {
            if (changedMember == "Name")
            {
                RenameLogic.HandleRename(container, instance, (string)oldValue, NameChangeAction.Rename);
            }
        }

        private void ReactIfChangedMemberIsFont(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            if (changedMember == "Font" || changedMember == "FontSize" || changedMember == "OutlineThickness" || changedMember == "UseFontSmoothing")
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

            var wereUnitValuesChanged =
                changedMember == "X Units" || changedMember == "Y Units" || changedMember == "Width Units" || changedMember == "Height Units";

            var shouldAttemptValueChange = wereUnitValuesChanged && ProjectManager.Self.GumProjectSave?.ConvertVariablesOnUnitTypeChange == true;

            if (shouldAttemptValueChange)
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

                    float thisWidth = 0;
                    float thisHeight = 0;

                    if (currentIpso != null)
                    {
                        currentIpso.GetFileWidthAndHeightOrDefault(out fileWidth, out fileHeight);
                        if (currentIpso.Parent != null)
                        {
                            parentWidth = currentIpso.Parent.Width;
                            parentHeight = currentIpso.Parent.Height;
                        }
                        thisWidth = currentIpso.Width;
                        thisHeight = currentIpso.Height;
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
                    if (AttemptToPersistPositionsOnUnitChanges && stateSave.TryGetValue<float>(GetQualifiedName(variableToSet), out valueOnObject))
                    {

                        var defaultUnitType = GeneralUnitType.PixelsFromSmall;

                        if (xOrY == XOrY.X)
                        {
                            UnitConverter.Self.ConvertToPixelCoordinates(
                                valueOnObject, 0, oldValue, defaultUnitType, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                            UnitConverter.Self.ConvertToUnitTypeCoordinates(
                                outX, outY, unitType, defaultUnitType, thisWidth, thisHeight, parentWidth, parentHeight, fileWidth, fileHeight, out valueToSet, out outY);
                        }
                        else
                        {
                            UnitConverter.Self.ConvertToPixelCoordinates(
                                0, valueOnObject, defaultUnitType, oldValue, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out outY);

                            UnitConverter.Self.ConvertToUnitTypeCoordinates(
                                outX, outY, defaultUnitType, unitType, thisWidth, thisHeight, parentWidth, parentHeight, fileWidth, fileHeight, out outX, out valueToSet);
                        }
                        wasAnythingSet = true;
                    }
                }
            }

            if (wasAnythingSet && AttemptToPersistPositionsOnUnitChanges && !float.IsPositiveInfinity(valueToSet))
            {
                InstanceSave instanceSave = SelectedState.Self.SelectedInstance;

                string unqualifiedVariableToSet = variableToSet;
                if (SelectedState.Self.SelectedInstance != null)
                {
                    variableToSet = SelectedState.Self.SelectedInstance.Name + "." + variableToSet;
                }

                stateSave.SetValue(variableToSet, valueToSet, instanceSave);

                // Force update everything on the spot. We know we can just set this value instead of forcing a full refresh:
                var gue = WireframeObjectManager.Self.GetSelectedRepresentation();

                if (gue != null)
                {
                    gue.SetProperty(unqualifiedVariableToSet, valueToSet);
                }
                GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);


            }
        }

        private void ReactIfChangedMemberIsSourceFile(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            ////////////Early Out /////////////////////////////
            string variableFullName;

            var instancePrefix = instance != null ? $"{instance.Name}." : "";

            variableFullName = $"{instancePrefix}{changedMember}";

            VariableSave variable = SelectedState.Self.SelectedStateSave?.GetVariableSave(variableFullName);

            bool isSourcefile = variable?.GetRootName() == "SourceFile";
            
            if(!isSourcefile)
            {
                return;
            }

            string errorMessage = GetWhySourcefileIsInvalid(variable.Value as string);

            if(!string.IsNullOrEmpty(errorMessage))
            {
                MessageBox.Show(errorMessage);

                variable.Value = oldValue;
            }
            else
            {
                string value;

                value = variable.Value as string;
                StateSave stateSave = SelectedState.Self.SelectedStateSave;

                if (!string.IsNullOrEmpty(value))
                {
                    var filePath = new FilePath(ProjectState.Self.ProjectDirectory + value);

                    // See if this is relative to the project
                    var shouldAskToCopy = !FileManager.IsRelativeTo(
                        filePath.FullPath,
                        ProjectState.Self.ProjectDirectory);

                    if (shouldAskToCopy && 
                        !string.IsNullOrEmpty(ProjectState.Self.GumProjectSave?.ParentProjectRoot) &&
                         FileManager.IsRelativeTo(filePath.FullPath, ProjectState.Self.ProjectDirectory + ProjectState.Self.GumProjectSave.ParentProjectRoot))
                    {
                        shouldAskToCopy = false;
                    }

                    if (shouldAskToCopy)
                    {
                        bool shouldCopy = AskIfShouldCopy(variable, value);
                        if(shouldCopy)
                        {
                            PerformCopy(variable, value);
                        }
                    }

                    if(filePath.Extension == "achx")
                    {
                        stateSave.SetValue($"{instancePrefix}Texture Address", Gum.Managers.TextureAddress.Custom);
                        GumCommands.Self.GuiCommands.RefreshPropertyGrid(force: true);
                    }
                }


                stateSave.SetValue($"{instancePrefix}AnimationFrames", new List<string>());
            }
        }

        private string GetWhySourcefileIsInvalid(string value)
        {
            string whyInvalid = null;

            var extension = FileManager.GetExtension(value);

            bool isValidExtension = extension == "gif" ||
                extension == "jpg" ||
                extension == "png" ||
                extension == "svg" || // todo - this should be plugin controlled
                extension == "achx";
            if(!isValidExtension)
            {
                whyInvalid = "The extension " + extension + " is not supported for textures";
            }

            if(string.IsNullOrEmpty(whyInvalid))
            {
                var gumProject = ProjectState.Self.GumProjectSave;
                if(gumProject.RestrictFileNamesForAndroid)
                {
                    var strippedName = 
                        FileManager.RemovePath(FileManager.RemoveExtension(value));
                    NameVerifier.Self.IsNameValidAndroidFile(strippedName, out whyInvalid);
                }
            }

            return whyInvalid;
        }

        private static bool AskIfShouldCopy(VariableSave variable, string value)
        {
            // Ask the user what to do - make it relative?
            MultiButtonMessageBox mbmb = new
                MultiButtonMessageBox();

            mbmb.StartPosition = FormStartPosition.Manual;

            mbmb.Location = new System.Drawing.Point(MainWindow.MousePosition.X - mbmb.Width / 2,
                 MainWindow.MousePosition.Y - mbmb.Height / 2);

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

            return shouldCopy;
        }

        private static void PerformCopy(VariableSave variable, string value)
        {
            string directory = FileManager.GetDirectory(ProjectManager.Self.GumProjectSave.FullFileName);
            string targetAbsoluteFile = directory + FileManager.RemovePath(value);
            try
            {

                string sourceAbsoluteFile = value;
                if(FileManager.IsRelative(sourceAbsoluteFile))
                {
                    sourceAbsoluteFile = directory + value;
                }
                sourceAbsoluteFile = FileManager.RemoveDotDotSlash(sourceAbsoluteFile);

                System.IO.File.Copy(sourceAbsoluteFile, targetAbsoluteFile, overwrite: true);

                variable.Value = FileManager.RemovePath(value);

            }
            catch (Exception e)
            {
                MessageBox.Show("Error copying file:\n" + e.ToString());
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
                            if(absolute.ToLowerInvariant().EndsWith(".achx"))
                            {
                                // I think this is already loaded here, because when the GUE has
                                // its ACXH set, the texture and texture coordinate values are set
                                // immediately...
                                // But I'm not 100% certain.
                                // update: okay, so it turns out what this does is it sets values on the Element itself
                                // so those values get saved to disk and displayed in the property grid. I could update the
                                // property grid here, but doing so would possibly immediately make the values be out-of-date
                                // because the animation chain can change the coordinates constantly as it animates. I'm not sure
                                // what to do here...
                            }
                            else
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
