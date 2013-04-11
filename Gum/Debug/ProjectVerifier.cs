using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Gum.DataTypes;
using Gum.Managers;
using RenderingLibrary;
using Gum.ToolStates;
using Gum.Wireframe;
using RenderingLibrary.Graphics;

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
                throw new Exception("Could not find Screen " + screenSave + " in the project");
            }
        }


        public void AssertIsPartOfProject(ComponentSave componentSave)
        {
            if (!ObjectFinder.Self.GumProjectSave.Components.Contains(componentSave))
            {
                throw new Exception("Could not find Component " + componentSave + " in the project");
            }
        }


        public void AssertSelectedIpsosArePartOfRenderer()
        {
            foreach (IPositionedSizedObject ipso in SelectionManager.Self.SelectedIpsos)
            {
                if (!Renderer.Self.Layers[0].Renderables.Contains(ipso as IRenderable))
                {
                    throw new Exception("There are IPSOs that are part of the selected IPSOs which aren't being rendered");
                }
            }
        }

        public void AssertIsPartOfRenderer(IPositionedSizedObject ipso)
        {
            if (ipso != null)
            {
                if (!Renderer.Self.Layers[0].Renderables.Contains(ipso as IRenderable))
                {
                    throw new Exception("Argument is not part of Renderer");
                }
            }
        }

    }
}
