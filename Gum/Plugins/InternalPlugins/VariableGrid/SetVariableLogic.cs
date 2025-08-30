using Gum.DataTypes;
using Gum.DataTypes.Variables;
using Gum.Managers;
using Gum.Plugins;
using Gum.ToolStates;
using Gum.Wireframe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Gum.RenderingLibrary;
using Gum.Converters;
using RenderingLibrary.Content;
using CommonFormsAndControls.Forms;
using ToolsUtilities;
using RenderingLibrary.Graphics;
using Gum.Logic;
using GumRuntime;
using Gum.Plugins.InternalPlugins.VariableGrid;
using Gum.Services;
using Gum.Commands;
using Gum.Graphics;
using Gum.ToolCommands;
using Gum.Undo;

namespace Gum.PropertyGridHelpers
{
    public class SetVariableLogic
    {
        private readonly VariableReferenceLogic _variableReferenceLogic;
        private readonly CircularReferenceManager _circularReferenceManager;
        private readonly FontManager _fontManager;
        private readonly IFileCommands _fileCommands;
        private readonly ISelectedState _selectedState;
        private readonly INameVerifier _nameVerifier;
        private readonly IRenameLogic _renameLogic;
        private readonly IElementCommands _elementCommands;
        private readonly IUndoManager _undoManager;
        private readonly WireframeCommands _wireframeCommands;
        private readonly IGuiCommands _guiCommands;
        private readonly IVariableInCategoryPropagationLogic _variableInCategoryPropagationLogic;

        public SetVariableLogic(ISelectedState selectedState, 
            INameVerifier nameVerifier, 
            IRenameLogic renameLogic, 
            IElementCommands elementCommands, 
            IUndoManager undoManager,
            WireframeCommands wireframeCommands,
            VariableReferenceLogic variableReferenceLogic,
            IGuiCommands guiCommands,
            FontManager fontManager,
            IFileCommands fileCommands,
            CircularReferenceManager circularReferenceManager,
            IVariableInCategoryPropagationLogic variableInCategoryPropagationLogic)
        {
            _selectedState = selectedState;
            _nameVerifier = nameVerifier;
            _renameLogic = renameLogic;
            _elementCommands = elementCommands;
            _undoManager = undoManager;
            _wireframeCommands = wireframeCommands;
            _variableReferenceLogic = variableReferenceLogic;
            _guiCommands = guiCommands;
            _fontManager = fontManager;
            _fileCommands = fileCommands;
            _circularReferenceManager = circularReferenceManager;
            _variableInCategoryPropagationLogic = variableInCategoryPropagationLogic;
        }

        public bool AttemptToPersistPositionsOnUnitChanges { get; set; } = true;



        // added instance property so we can change values even if a tree view is selected
        public GeneralResponse PropertyValueChanged(string unqualifiedMemberName, object oldValue, InstanceSave instance, StateSave stateContainingVariable, bool refresh = true, bool recordUndo = true,
            bool trySave = true)
        {
            IInstanceContainer instanceContainer = null;

            if (stateContainingVariable != null)
            {
                instanceContainer = stateContainingVariable.ParentContainer;


                if (instance != null)
                {
                    _selectedState.SelectedVariableSave = stateContainingVariable.GetVariableSave(instance.Name + "." + unqualifiedMemberName);
                }
                else
                {
                    _selectedState.SelectedVariableSave = stateContainingVariable.GetVariableSave(unqualifiedMemberName);
                }
            }

            if (instance != null && instanceContainer == null)
            {
                // This can happen if the user hasn't selected a state
                //  (if the user opens the app and drag+drops immediately before selecting anything)
                // --or--
                // the user hasn't selected a behavior.
                // case we should look to the instance and get its container:
                instanceContainer = 
                    (IInstanceContainer)ObjectFinder.Self.GetElementContainerOf(instance) ?? 
                    ObjectFinder.Self.GetBehaviorContainerOf(instance);
            }
            if(stateContainingVariable == null && instanceContainer is ElementSave containerElement)
            {
                stateContainingVariable = containerElement.DefaultState;
            }

            var response = ReactToPropertyValueChanged(unqualifiedMemberName, oldValue, instanceContainer, instance, stateContainingVariable, refresh, recordUndo: recordUndo, trySave: trySave);
            return response;
        }

        /// <summary>
        /// Reacts to a variable having been set.
        /// </summary>
        /// <param name="unqualifiedMember">The variable name without the prefix instance name.</param>
        /// <param name="oldValue"></param>
        /// <param name="parentElement"></param>
        /// <param name="instance"></param>
        /// <param name="refresh"></param>
        public GeneralResponse ReactToPropertyValueChanged(string unqualifiedMember, object oldValue, IInstanceContainer instanceContainer,
            InstanceSave instance, StateSave stateSave, bool refresh, bool recordUndo = true, bool trySave = true)
        {
            GeneralResponse response = GeneralResponse.SuccessfulResponse;
            ObjectFinder.Self.EnableCache();
            try
            {
                // This code calls plugin methods and may generate code. We want to generate code
                // after the variable references are assigned. Moving this line of code down:
                //ReactToChangedMember(unqualifiedMember, oldValue, parentElement, instance, stateSave);
                // Update - ReactToChangedMember expands variable reference names like "Color" and it fills
                // in implied assignments such as "Reference.X" to "X = Reference.X"
                // It must be called first before applying references
                // Update December 23 2024
                // So does that mean that we want to call this code before we update variable references, but
                // we can still raise the plugin event after? If so, I'm going to move the plugin manager call
                // out of ReactToChangedMember and call it here.
                response = ReactToChangedMember(unqualifiedMember, oldValue, instanceContainer, instance, stateSave);
                var parentElement = instanceContainer as ElementSave;

                bool didSetDeepReference = false;

                if (parentElement != null)
                {
                    string qualifiedName = unqualifiedMember;
                    if (instance != null)
                    {
                        qualifiedName = $"{instance.Name}.{unqualifiedMember}";
                    }

                    _variableReferenceLogic.DoVariableReferenceReaction(parentElement, instance, unqualifiedMember, stateSave, qualifiedName, trySave);

                    _variableInCategoryPropagationLogic.PropagateVariablesInCategory(qualifiedName, parentElement,
                        // This code used to not specify the category, so it defaulted to the selected category.
                        // I'm maintaining this behavior but I'm not sure if it's what should happen - maybe we should
                        // serach for the owner category of the state?
                        _selectedState.SelectedStateCategorySave);

                    // Need to record undo before refreshing and reselecting the UI
                    if (recordUndo)
                    {
                        _undoManager.RecordUndo();
                    }

                    if (refresh)
                    {
                        RefreshInResponseToVariableChange(unqualifiedMember, oldValue, parentElement, instance, qualifiedName);

                    }
                }

                // see comment by ReactToChangedMember about why we make this call here
                // Also this should happen after we update the wireframe so that plugins like
                // the texture window which depend on the wireframe will have the correct values
                PluginManager.Self.VariableSet(parentElement, instance, unqualifiedMember, oldValue);

                // This used to only check if values have changed. However, this can cause problems
                // because an intermediary value may change the value, then it gets a full commit. On
                // the full commit it doesn't save, so we need to save if this is true. 
                // Update July 22, 2025
                // Plugins may make modifications
                // to the element, so save *after*
                // calling PluginManager.Self.VariableSet
                if (trySave)
                {
                    _fileCommands.TryAutoSaveElement(parentElement);
                }
            }
            finally
            {
                ObjectFinder.Self.DisableCache();
            }

            return response;
        }


        void RefreshInResponseToVariableChange(string unqualifiedMember, object oldValue, ElementSave parentElement,
            InstanceSave instance, string qualifiedName)
        {

            var needsToRefreshEntireElement = ExclusionsPlugin.VariablesRequiringRefresh.Contains(unqualifiedMember);

            if (needsToRefreshEntireElement)
            {
                _guiCommands.RefreshElementTreeView(parentElement);
                _guiCommands.RefreshVariables(force: true);
            }
        }

        private GeneralResponse ReactToChangedMember(string rootVariableName, object oldValue, IInstanceContainer instanceContainer, InstanceSave instance, StateSave stateSave)
        {
            var response = ReactIfChangedMemberIsName(instanceContainer, instance, rootVariableName, oldValue);

            // Handled in a plugin
            //ReactIfChangedMemberIsBaseType(parentElement, changedMember, oldValue);

            // todo - should this use current state?
            var changedMemberWithPrefix = rootVariableName;
            if (instance != null)
            {
                changedMemberWithPrefix = instance.Name + "." + rootVariableName;
            }

            var parentElement = instanceContainer as ElementSave;
            if (parentElement != null)
            {
                var rfv = new RecursiveVariableFinder(stateSave);
                var value = rfv.GetValue(changedMemberWithPrefix);
                List<ElementWithState> elementStack = new List<ElementWithState>();
                elementStack.Add(new ElementWithState(parentElement) { StateName = stateSave?.Name, InstanceName = instance?.Name });
                ReactIfChangedMemberIsFont(elementStack, instance, rootVariableName, oldValue, value);

                ReactIfChangedMemberIsCustomFont(parentElement, rootVariableName, oldValue);

                ReactIfChangedMemberIsUnitType(parentElement, rootVariableName, oldValue);

                ReactIfChangedMemberIsSourceFile(parentElement, instance, rootVariableName, oldValue);

                ReactIfChangedMemberIsTextureAddress(parentElement, rootVariableName, oldValue);

                ReactIfChangedMemberIsParent(parentElement, instance, rootVariableName, oldValue);

                ReactIfChangedMemberIsDefaultChildContainer(parentElement, instance, rootVariableName, oldValue);

                _variableReferenceLogic.ReactIfChangedMemberIsVariableReference(parentElement, instance, stateSave, rootVariableName, oldValue);
            }
            ReactIfChangedBaseType(instanceContainer, instance, stateSave, rootVariableName, oldValue);

            return response;
        }

        private void ReactIfChangedBaseType(IInstanceContainer instanceContainer, InstanceSave instance, StateSave stateSave, string rootVariableName, object oldValue)
        {

            if (rootVariableName == "BaseType")
            {
                VariableSave variable = _selectedState.SelectedVariableSave;

                if (instance != null)
                {
                    var parentElement = instanceContainer as ElementSave;

                    if(_circularReferenceManager.CanTypeBeAddedToElement(parentElement, instance.BaseType) == false)
                    {
                        MessageBox.Show("This assignment would create a circular reference, which is not allowed.");
                        //stateSave.SetValue("BaseType", oldValue, instance);
                        instance.BaseType = (string)oldValue;
                        _guiCommands.PrintOutput($"BaseType assignment on {instance.Name} is not allowed - reverting to previous value");
                        _guiCommands.RefreshVariables(force: true);
                    }

                    if(instanceContainer != null)
                    {
                        _fileCommands.TryAutoSaveObject(instanceContainer);
                    }
                }
            }
        }

        private void ReactIfChangedMemberIsDefaultChildContainer(ElementSave parentElement, InstanceSave instance, string rootVariableName, object oldValue)
        {
            VariableSave variable = _selectedState.SelectedVariableSave;

            if (variable != null && rootVariableName == nameof(ComponentSave.DefaultChildContainer))
            {
                if ((variable.Value as string) == "<NONE>")
                {
                    variable.Value = null;
                }

            }
        }

        private GeneralResponse ReactIfChangedMemberIsName(IInstanceContainer instanceContainer, InstanceSave instance, string changedMember, object oldValue)
        {
            var toReturn = OptionallyAttemptedGeneralResponse.SuccessfulWithoutAttempt;

            if (changedMember == "Name")
            {
                var innerResponse = _renameLogic.HandleRename(instanceContainer, instance, (string)oldValue, NameChangeAction.Rename);
                toReturn.SetFrom(innerResponse);
            }
            return toReturn;
        }

        private void ReactIfChangedMemberIsFont(List<ElementWithState> elementStack, InstanceSave instance, string changedMember, object oldValue, object newValue)
        {
            var handledByInner = false;
            var instanceElement = instance != null ? ObjectFinder.Self.GetElementSave(instance) : null;
            if (instanceElement != null)
            {
                var variable = instanceElement.DefaultState.Variables.FirstOrDefault(item => item.ExposedAsName == changedMember);

                if (variable != null)
                {
                    var innerInstance = instanceElement.GetInstance(variable.SourceObject);

                    elementStack.Add(new ElementWithState(instanceElement) { InstanceName = variable.SourceObject });

                    ReactIfChangedMemberIsFont(elementStack, innerInstance, variable.GetRootName(), oldValue, newValue);
                    handledByInner = true;
                }
            }

            if (!handledByInner)
            {
                if (changedMember == "Font" || changedMember == "FontSize" || changedMember == "OutlineThickness" || changedMember == "UseFontSmoothing" ||
                    changedMember == "IsItalic" || changedMember == "IsBold")
                {
                    // This will be null if the user is editing the Text StandardElement
                    if (instanceElement != null)
                    {
                        elementStack.Add(new ElementWithState(instanceElement));
                    }
                    var rfv = new RecursiveVariableFinder(elementStack);


                    var forcedValues = new StateSave();

                    void TryAddForced(string variableName)
                    {
                        var value = rfv.GetValueByBottomName(variableName);
                        if (value != null)
                        {
                            forcedValues.SetValue(variableName, value);
                        }
                    }

                    TryAddForced("Font");
                    TryAddForced("FontSize");
                    TryAddForced("OutlineThickness");
                    TryAddForced("UseFontSmoothing");
                    TryAddForced("IsItalic");
                    TryAddForced("IsBold");

                    StateSave stateSave = _selectedState.SelectedStateSave;

                    // If the user has a category selected but no state in the category, then use the default:
                    if (stateSave == null && _selectedState.SelectedStateCategorySave != null)
                    {
                        stateSave = _selectedState.SelectedElement.DefaultState;
                    }


                    _fontManager.ReactToFontValueSet(instance, GumState.Self.ProjectState.GumProjectSave, stateSave, forcedValues);
                }
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
            StateSave stateSave = _selectedState.SelectedStateSave;
            float valueToSet = 0;

            var wereUnitValuesChanged =
                changedMember == "XUnits" || changedMember == "YUnits" || changedMember == "WidthUnits" || changedMember == "HeightUnits";

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
                    
                    object unitTypeAsObject = _elementCommands.GetCurrentValueForVariable(changedMember, _selectedState.SelectedInstance);
                    GeneralUnitType unitType = UnitConverter.ConvertToGeneralUnit(unitTypeAsObject);


                    XOrY xOrY = XOrY.X;
                    if (changedMember == "XUnits")
                    {
                        variableToSet = "X";
                        xOrY = XOrY.X;
                    }
                    else if (changedMember == "YUnits")
                    {
                        variableToSet = "Y";
                        xOrY = XOrY.Y;
                    }
                    else if (changedMember == "WidthUnits")
                    {
                        variableToSet = "Width";
                        isWidthOrHeight = true;
                        xOrY = XOrY.X;

                    }
                    else if (changedMember == "HeightUnits")
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
                InstanceSave instanceSave = _selectedState.SelectedInstance;

                string unqualifiedVariableToSet = variableToSet;
                if (_selectedState.SelectedInstance != null)
                {
                    variableToSet = _selectedState.SelectedInstance.Name + "." + variableToSet;
                }

                stateSave.SetValue(variableToSet, valueToSet, instanceSave);

                // Force update everything on the spot. We know we can just set this value instead of forcing a full refresh:
                var gue = WireframeObjectManager.Self.GetSelectedRepresentation();

                if (gue != null)
                {
                    gue.SetProperty(unqualifiedVariableToSet, valueToSet);
                }
                _guiCommands.RefreshVariables(force: true);


            }
        }

        private void ReactIfChangedMemberIsSourceFile(ElementSave parentElement, InstanceSave instance, string changedMember, object oldValue)
        {
            ////////////Early Out /////////////////////////////

            string variableFullName;

            var instancePrefix = instance != null ? $"{instance.Name}." : "";

            variableFullName = $"{instancePrefix}{changedMember}";

            VariableSave variable = _selectedState.SelectedStateSave?.GetVariableSave(variableFullName);

            bool isSourcefile = variable?.GetRootName() == "SourceFile";

            if (!isSourcefile || string.IsNullOrWhiteSpace(variable.Value as string))
            {
                return;
            }

            ////////////End Early Out/////////////////////////

            string errorMessage = GetWhySourcefileIsInvalid(variable.Value as string, parentElement, instance, changedMember);

            if (!string.IsNullOrEmpty(errorMessage))
            {
                MessageBox.Show(errorMessage);

                variable.Value = oldValue;
            }
            else
            {
                string value;

                value = variable.Value as string;
                StateSave stateSave = _selectedState.SelectedStateSave;

                if (!string.IsNullOrEmpty(value))
                {
                    var filePath = new FilePath(ProjectState.Self.ProjectDirectory + value);

                    // See if this is relative to the project
                    var shouldAskToCopy = !FileManager.IsRelativeTo(
                        filePath.FullPath,
                        ProjectState.Self.ProjectDirectory) && !FileManager.IsUrl(variable.Value as string);

                    if (shouldAskToCopy &&
                        !string.IsNullOrEmpty(ProjectState.Self.GumProjectSave?.ParentProjectRoot) &&
                         FileManager.IsRelativeTo(filePath.FullPath, ProjectState.Self.ProjectDirectory + ProjectState.Self.GumProjectSave.ParentProjectRoot))
                    {
                        shouldAskToCopy = false;
                    }

                    var cancel = false;

                    if (shouldAskToCopy)
                    {
                        var shouldCopy = AskIfShouldCopy(variable, value);
                        if (shouldCopy == true)
                        {
                            PerformCopy(variable, value);
                        }
                        else if (shouldCopy == null)
                        {
                            cancel = true;
                        }
                    }

                    if (cancel)
                    {
                        variable.Value = oldValue;
                        _guiCommands.RefreshVariableValues();
                    }

                    if (!cancel && filePath.Extension == "achx")
                    {
                        stateSave.SetValue($"{instancePrefix}TextureAddress", Gum.Managers.TextureAddress.Custom);
                        _guiCommands.RefreshVariables(force: true);
                    }
                }

                // August 16, 2025
                // Why do we set this
                // value? It adds a variable
                // with a null type, and it's
                // not needed for .achx animations.
                // Were we previously trying to reset
                // this value if it was explicitly set?
                // I'm going to remove it but leave this
                // comment here to see if removing this causes
                // any problems.
                // For reference: https://github.com/vchelaru/Gum/issues/1289
                //stateSave.SetValue($"{instancePrefix}AnimationFrames", new List<string>());
            }
        }

        private string GetWhySourcefileIsInvalid(string value, ElementSave parentElement, InstanceSave instance, string changedMember)
        {

            ////////////////early out//////////////////////
            var isUrl = FileManager.IsUrl(value);
            if (isUrl)
            {
                // extension can be anything...
                return null;
            }
            //////////////end early out///////////////////

            string whyInvalid = null;

            var extension = FileManager.GetExtension(value);
            bool isValidExtension = extension == "gif" ||
                extension == "jpg" ||
                extension == "png" ||
                extension == "bmp" ||
                extension == "achx";

            if (!isValidExtension)
            {
                var fromPluginManager = PluginManager.Self.GetIfExtensionIsValid(extension, parentElement, instance, changedMember);
                if (fromPluginManager == true)
                {
                    isValidExtension = true;
                }
            }

            if (!isValidExtension)
            {
                whyInvalid = "The extension " + extension + " is not supported for textures";
            }

            if (string.IsNullOrEmpty(whyInvalid))
            {
                var gumProject = ProjectState.Self.GumProjectSave;
                if (gumProject.RestrictFileNamesForAndroid)
                {
                    var strippedName =
                        FileManager.RemovePath(FileManager.RemoveExtension(value));
                    _nameVerifier.IsNameValidAndroidFile(strippedName, out whyInvalid);
                }
            }

            return whyInvalid;
        }

        private static bool? AskIfShouldCopy(VariableSave variable, string value)
        {
            // Ask the user what to do - make it relative?
            MultiButtonMessageBox mbmb = new
                MultiButtonMessageBox();

            mbmb.StartPosition = FormStartPosition.CenterParent;

            mbmb.MessageText = "The file\n" + value + "\nis not relative to the project.  What would you like to do?";
            mbmb.AddButton("Reference the file in its current location", DialogResult.OK);
            mbmb.AddButton("Copy the file relative to the Gum project and reference the copy", DialogResult.Yes);

            var dialogResult = mbmb.ShowDialog();

            bool? shouldCopy = false;

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

                    var overwriteResult = mbmb.ShowDialog();

                    if (overwriteResult == DialogResult.Yes)
                    {
                        shouldCopy = true;
                    }
                    else if (overwriteResult == DialogResult.No)
                    {
                        shouldCopy = false;
                    }
                    else
                    {
                        shouldCopy = null;
                    }
                }
            }
            else if (dialogResult == DialogResult.OK)
            {
                shouldCopy = false;
            }
            else
            {
                shouldCopy = null;
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
                if (FileManager.IsRelative(sourceAbsoluteFile))
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

            VariableSave variable = _selectedState.SelectedVariableSave;
            // Eventually need to handle tunneled variables
            if (variable != null && changedMember == "Parent")
            {
                if ((variable.Value as string) == "<NONE>")
                {
                    variable.Value = null;
                }

                if (variable.Value != null)
                {
                    var newParent = parentElement.Instances.FirstOrDefault(item => item.Name == variable.Value as string);
                    var newValue = variable.Value;
                    // unset it before finding recursive children, in case there is a circular reference:
                    variable.Value = null;
                    var childrenInstances = GetRecursiveChildrenOf(parentElement, instance);

                    if (childrenInstances.Contains(newParent))
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

                if (isValidAssignment)
                {
                    _guiCommands.RefreshElementTreeView(parentElement);
                }
                else
                {
                    _guiCommands.RefreshVariables(force: true);
                }
            }
        }

        static char[] equalsArray = new char[] { '=' };


        private List<InstanceSave> GetRecursiveChildrenOf(ElementSave parent, InstanceSave instance)
        {
            var defaultState = parent.DefaultState;
            List<InstanceSave> toReturn = new List<InstanceSave>();
            List<InstanceSave> directChildren = new List<InstanceSave>();
            foreach (var potentialChild in parent.Instances)
            {
                var foundParentVariable = defaultState.Variables
                    .FirstOrDefault(item => item.Name == $"{potentialChild.Name}.Parent" && item.Value as string == instance.Name);

                if (foundParentVariable != null)
                {
                    directChildren.Add(potentialChild);
                }
            }

            toReturn.AddRange(directChildren);

            foreach (var child in directChildren)
            {
                var childrenOfChild = GetRecursiveChildrenOf(parent, child);
                toReturn.AddRange(childrenOfChild);
            }

            return toReturn;
        }

        private void ReactIfChangedMemberIsTextureAddress(ElementSave parentElement, string changedMember, object oldValue)
        {
            if (changedMember == "TextureAddress")
            {
                RecursiveVariableFinder rvf;

                var instance = _selectedState.SelectedInstance;
                if (instance != null)
                {
                    rvf = new RecursiveVariableFinder(_selectedState.SelectedInstance, parentElement);
                }
                else
                {
                    rvf = new RecursiveVariableFinder(parentElement.DefaultState);
                }

                var textureAddress = rvf.GetValue<TextureAddress>("TextureAddress");

                if (textureAddress == TextureAddress.Custom)
                {
                    string sourceFile = rvf.GetValue<string>("SourceFile");

                    if (!string.IsNullOrEmpty(sourceFile))
                    {
                        string absolute = ProjectManager.Self.MakeAbsoluteIfNecessary(sourceFile);

                        if (System.IO.File.Exists(absolute))
                        {
                            if (absolute.ToLowerInvariant().EndsWith(".achx"))
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
                                var size = ImageHeader.GetDimensions(absolute);

                                if (size != null && instance != null)
                                {
                                    parentElement.DefaultState.SetValue(instance.Name + ".TextureTop", 0, "int");
                                    parentElement.DefaultState.SetValue(instance.Name + ".TextureLeft", 0, "int");
                                    parentElement.DefaultState.SetValue(instance.Name + ".TextureWidth", size.Value.Width, "int");
                                    parentElement.DefaultState.SetValue(instance.Name + ".TextureHeight", size.Value.Height, "int");

                                    _wireframeCommands.Refresh();
                                }
                            }
                        }
                    }
                }
            }
        }

        string GetQualifiedName(string variableName)
        {
            if (_selectedState.SelectedInstance != null)
            {
                return _selectedState.SelectedInstance.Name + "." + variableName;
            }
            else
            {
                return variableName;
            }
        }

    }
}
