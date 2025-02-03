﻿using System.Collections.Generic;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Behaviors;
using Gum.ToolStates;
using ToolsUtilities;
using CommonFormsAndControls;
using System.Windows.Forms;
using System.Windows.Controls;
using Gum.DataTypes.Variables;
using Gum.Plugins;

namespace Gum.ToolCommands
{
    public class ProjectCommands
    {
        #region Fields

        static ProjectCommands mSelf;

        #endregion

        #region Properties

        public ElementCommands ElementCommands => ElementCommands.Self;

        public static ProjectCommands Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectCommands();
                }
                return mSelf;
            }
        }

        #endregion

        #region Screens
        /// <summary>
        /// Creates a new Screen using the argument as the name. This saves the newly created screen to disk and saves the project.
        /// </summary>
        /// <param name="screenName"></param>
        /// <returns></returns>
        public ScreenSave AddScreen(string screenName)
        {
            ScreenSave screenSave = new ScreenSave();
            screenSave.Name = screenName;

            AddScreen(screenSave);

            return screenSave;
        }

        public void AddScreen(ScreenSave screenSave)
        {
            screenSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));
            StandardElementsManagerGumTool.Self.FixCustomTypeConverters(screenSave);
            ProjectManager.Self.GumProjectSave.ScreenReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
            ProjectManager.Self.GumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            ProjectManager.Self.GumProjectSave.Screens.Add(screenSave);
            ProjectManager.Self.GumProjectSave.Screens.Sort((first, second) => first.Name.CompareTo(second.Name));


            GumCommands.Self.FileCommands.TryAutoSaveProject();
            GumCommands.Self.FileCommands.TryAutoSaveElement(screenSave);

            Plugins.PluginManager.Self.ElementAdd(screenSave);
        }

        #endregion

        #region Element (Screen/Component/Standard)

        internal void RemoveElement(ElementSave element)
        {
            GumProjectSave gps = ProjectManager.Self.GumProjectSave;
            string name = element.Name;
            var removed = false;
            if (element is ScreenSave asScreenSave)
            {
                RemoveElementReferencesFromList(name, gps.ScreenReferences);
                gps.Screens.Remove(asScreenSave);
                removed = true;
            }
            else if (element is ComponentSave asComponentSave)
            {
                RemoveElementReferencesFromList(name, gps.ComponentReferences);
                gps.Components.Remove(asComponentSave);
                removed = true;

            }

            if(removed)
            {
                Plugins.PluginManager.Self.ElementDelete(element);
                GumCommands.Self.FileCommands.TryAutoSaveProject();
            }
        }

        #endregion

        private static void RemoveElementReferencesFromList(string name, List<ElementReference> references)
        {
            for (int i = 0; i < references.Count; i++)
            {
                ElementReference reference = references[i];

                if (reference.Name == name)
                {
                    references.RemoveAt(i);
                    break;
                }
            }
        }

        #region Component

        public void AskToAddComponent()
        {
            if (ObjectFinder.Self.GumProjectSave == null || string.IsNullOrEmpty(ProjectManager.Self.GumProjectSave.FullFileName))
            {
                MessageBox.Show("You must first save the project before adding a new component");
            }
            else
            {
                TextInputWindow tiw = new TextInputWindow();
                tiw.Message = "Enter new Component name:";
                tiw.Title = "Add Component";

                if (tiw.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    string name = tiw.Result;
                    TreeNode nodeToAddTo = ElementTreeViewManager.Self.SelectedNode;

                    while (nodeToAddTo != null && nodeToAddTo.Tag is ComponentSave && nodeToAddTo.Parent != null)
                    {
                        nodeToAddTo = nodeToAddTo.Parent;
                    }

                    if (nodeToAddTo == null || !nodeToAddTo.IsPartOfComponentsFolderStructure())
                    {
                        nodeToAddTo = ElementTreeViewManager.Self.RootComponentsTreeNode;
                    }

                    FilePath path = nodeToAddTo.GetFullFilePath();

                    string relativeToComponents = FileManager.MakeRelative(path.StandardizedCaseSensitive,
                        FileLocations.Self.ComponentsFolder, preserveCase:true);

                    AddComponent(name, relativeToComponents);
                }
            }
        }

        public ComponentSave AddComponent(string componentName, string folder)
        {
            string whyNotValid;

            folder = folder?.Replace('\\', '/');

            if (!NameVerifier.Self.IsElementNameValid(componentName, folder, null, out whyNotValid))
            {
                MessageBox.Show(whyNotValid);
                return null;
            }
            else
            {
                ComponentSave componentSave = new ComponentSave();
                PrepareNewComponentSave(componentSave, folder + componentName);

                AddComponent(componentSave);
                return componentSave;
            }
        }

        public void AddComponent(ComponentSave componentSave)
        {
            var gumProject = ProjectState.Self.GumProjectSave;
            gumProject.ComponentReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
            gumProject.ComponentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            gumProject.Components.Add(componentSave);
            gumProject.Components.Sort((first, second) => first.Name.CompareTo(second.Name));


            GumCommands.Self.FileCommands.TryAutoSaveProject();
            GumCommands.Self.FileCommands.TryAutoSaveElement(componentSave);
            Plugins.PluginManager.Self.ElementAdd(componentSave);

            SelectedState.Self.SelectedComponent = componentSave;
        }

        private void PrepareNewComponentSave(ComponentSave componentSave, string componentName)
        {
            componentSave.Name = componentName;

            componentSave.BaseType = "Container";

            componentSave.InitializeDefaultAndComponentVariables();
            StandardElementsManagerGumTool.Self.FixCustomTypeConverters(componentSave);


            // components shouldn't set their positions to 0 by default, so if the
            // default state sets those values, we should null them out:
            var xVariable = componentSave.DefaultState.GetVariableSave("X");
            var yVariable = componentSave.DefaultState.GetVariableSave("Y");

            if (xVariable != null)
            {
                xVariable.Value = null;
                xVariable.SetsValue = false;
            }
            if (yVariable != null)
            {
                yVariable.Value = null;
                yVariable.SetsValue = false;
            }

            var hasEventsVariable = componentSave.DefaultState.GetVariableSave("HasEvents");
            if (hasEventsVariable != null)
            {
                hasEventsVariable.Value = true;
            }
        }


        #endregion
    }
}
