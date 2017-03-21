using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using Gum.DataTypes.Behaviors;

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
            screenSave.Name = screenName;

            AddScreen(screenSave);

            return screenSave;
        }

        public void AddScreen(ScreenSave screenSave)
        {
            screenSave.Initialize(StandardElementsManager.Self.GetDefaultStateFor("Screen"));

            ProjectManager.Self.GumProjectSave.ScreenReferences.Add(new ElementReference { Name = screenSave.Name, ElementType = ElementType.Screen });
            ProjectManager.Self.GumProjectSave.ScreenReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            ProjectManager.Self.GumProjectSave.Screens.Add(screenSave);

        }

        public ComponentSave AddComponent(string componentName)
        {
            ComponentSave componentSave = new ComponentSave();
            componentSave.Name = componentName;



            AddComponent(componentSave);




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

            return componentSave;
        }

        public void AddComponent(ComponentSave componentSave)
        {
            componentSave.BaseType = "Container";

            ProjectManager.Self.GumProjectSave.ComponentReferences.Add(new ElementReference { Name = componentSave.Name, ElementType = ElementType.Component });
            ProjectManager.Self.GumProjectSave.ComponentReferences.Sort((first, second) => first.Name.CompareTo(second.Name));
            ProjectManager.Self.GumProjectSave.Components.Add(componentSave);

            componentSave.InitializeDefaultAndComponentVariables();

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

        internal void RemoveBehavior(BehaviorSave behavior)
        {
            GumProjectSave gps = ProjectManager.Self.GumProjectSave;
            string name = behavior.Name;
            List<BehaviorReference> references = gps.BehaviorReferences;

            references.RemoveAll(item => item.Name == behavior.Name);

            gps.Behaviors.Remove(behavior);
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
    }
}
