using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.ToolCommands
{
    public class ProjectCommands
    {
        #region Fields

        static ProjectCommands mSelf;

        #endregion

        #region Properties

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

        #region Methods

        public ScreenSave AddScreen(string screenName)
        {
            ScreenSave screenSave = new ScreenSave();
            screenSave.Initialize( StandardElementsManager.Self.GetDefaultStateFor("Screen") );
            screenSave.Name = screenName;

            ProjectManager.Self.GumProjectSave.ScreenReferences.Add(new ElementReference { Name = screenName, ElementType = ElementType.Screen });
            ProjectManager.Self.GumProjectSave.Screens.Add(screenSave);

            return screenSave;
        }


        public ComponentSave AddComponent(string componentName)
        {
            ComponentSave componentSave = new ComponentSave();
            componentSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Container"));

            // components shouldn't set their positions to 0 by default, so if the
            // default state sets those values, we should null them out:
            var xVariable = componentSave.DefaultState.GetVariableSave("X");
            var yVariable = componentSave.DefaultState.GetVariableSave("Y");

            if (xVariable != null)
            {
                xVariable.Value = null;
            }
            if (yVariable != null)
            {
                yVariable.Value = null;
            }

            componentSave.BaseType = "Container";
            componentSave.Name = componentName;

            ProjectManager.Self.GumProjectSave.ComponentReferences.Add(new ElementReference { Name = componentName, ElementType = ElementType.Component });
            ProjectManager.Self.GumProjectSave.Components.Add(componentSave);

            return componentSave;
        }


        #endregion

        internal void RemoveElement(ElementSave element)
        {
            if (element is ScreenSave)
            {
                RemoveScreen(element as ScreenSave);
            }
            else if (element is ComponentSave)
            {
                RemoveComponent(element as ComponentSave);
            }
        }

        internal void RemoveScreen(ScreenSave asScreenSave)
        {
            GumProjectSave gps = ProjectManager.Self.GumProjectSave;

            string name = asScreenSave.Name;
            List<ElementReference> references = gps.ScreenReferences;

            RemoveElementReferencesFromList(name, references);

            gps.Screens.Remove(asScreenSave);
        }


        internal void RemoveComponent(ComponentSave asComponentSave)
        {
            GumProjectSave gps = ProjectManager.Self.GumProjectSave;

            string name = asComponentSave.Name;
            List<ElementReference> references = gps.ComponentReferences;

            RemoveElementReferencesFromList(name, references);

            gps.Components.Remove(asComponentSave);
        }


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

        internal void SaveProject()
        {
            ProjectManager.Self.SaveProject();
        }


    }
}
