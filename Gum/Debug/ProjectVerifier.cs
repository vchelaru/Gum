using System;
using Gum.DataTypes;
using Gum.Managers;

namespace Gum.Debug
{
    /// <summary>
    /// Class responsible for reporting problems in the state
    /// of the project, such as objects referencing instances that
    /// are not part of the current gum project.  This can be used to
    /// identify issues occuring due to undo systems, object creation, or 
    /// any other system that may modify references.
    /// </summary>
    public class ProjectVerifier
    {
        static ProjectVerifier mSelf;


        public static ProjectVerifier Self
        {
            get
            {
                if (mSelf == null)
                {
                    mSelf = new ProjectVerifier();
                }
                return mSelf;
            }
        }



        public void AssertIsPartOfProject(ElementSave elementSave)
        {
            if (elementSave is ScreenSave)
            {
                AssertIsPartOfProject(elementSave as ScreenSave);
            }
            else if (elementSave is ComponentSave)
            {
                AssertIsPartOfProject(elementSave as ComponentSave);
            }
        }

        public void AssertIsPartOfProject(ScreenSave screenSave)
        {
            if (!ObjectFinder.Self.GumProjectSave.Screens.Contains(screenSave))
            {
                var screenName = screenSave == null ? "<null screen>" : screenSave.ToString();
                throw new Exception($"Could not find Screen {screenName} in the project {ObjectFinder.Self.GumProjectSave.FullFileName}");
            }
        }


        public void AssertIsPartOfProject(ComponentSave componentSave)
        {
            if (!ObjectFinder.Self.GumProjectSave.Components.Contains(componentSave))
            {
                throw new Exception("Could not find Component " + componentSave + " in the project");
            }
        }
    }
}
