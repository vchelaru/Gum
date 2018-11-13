using Gum.DataTypes;
using Gum.Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using GlueScreen = FlatRedBall.Glue.SaveClasses.ScreenSave;
using GlueEntity = FlatRedBall.Glue.SaveClasses.EntitySave;
using GlueElement = FlatRedBall.Glue.SaveClasses.IElement;

using System.Xml.Linq;
using ToolsUtilities;
using GluePlugin.SaveObjects;

namespace GluePlugin.Logic
{
    public class ElementDeleteLogic : Singleton<ElementDeleteLogic>
    {
        internal void HandleElementDelete(ElementSave gumElement)
        {
            var glueProject = GluePluginState.Self.GlueProject;

            ///////////////////////Early Out///////////////////////
            if (glueProject == null || GluePluginState.Self.InitializationState != InitializationState.Initialized)
            {
                return;
            }
            ////////////////////End Early Out/////////////////////


            var glueElement = GluePluginObjectFinder.Self.GetGlueElementFrom(gumElement);

            if (glueElement is GlueScreen)
            {
                glueProject.Screens.Remove(glueElement as GlueScreen);
            }
            else if (glueElement is GlueEntity)
            {
                glueProject.Entities.Remove(glueElement as GlueEntity);
            }

            var project = VisualStudioProjectSave.Load(
                GluePluginState.Self.CsprojFilePath.StandardizedCaseSensitive);

            RemoveCodeFilesFromProject(glueElement, project.XDocument.Elements());
            project.XDocument.Save(GluePluginState.Self.CsprojFilePath.StandardizedCaseSensitive);

            FileManager.XmlSerialize(glueProject, GluePluginState.Self.GlueProjectFilePath.StandardizedCaseSensitive);

        }

        private void RemoveCodeFilesFromProject(GlueElement glueElement, IEnumerable<XElement> elements)
        {

            foreach(var removalCandidate in elements.ToArray())
            {
                if(ShouldRemove(glueElement, removalCandidate))
                {
                    removalCandidate.Remove();
                }
                else
                {
                    RemoveCodeFilesFromProject(glueElement, removalCandidate.Elements());
                }
            }
        }

        private bool ShouldRemove(GlueElement glueElement, XElement removalCandidate)
        {
            var isCompile = removalCandidate.Name?.LocalName == "Compile";
            if(isCompile)
            {
                var includeAttribute = removalCandidate.Attributes().FirstOrDefault(item => item.Name.LocalName == "Include");

                if(includeAttribute != null)
                {
                    var customCodeLocation =
                        CodeCreationLogic.Self.GetCustomCodeFileLocationFor(glueElement, absolute: false);

                    var shouldRemove = includeAttribute.Value.StartsWith(glueElement.Name + ".") &&
                        includeAttribute.Value.EndsWith(".cs");

                    return shouldRemove;
                }

            }
            return false;
        }
    }
}
