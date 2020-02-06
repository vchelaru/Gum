using Gum;
using Gum.DataTypes;
using Gum.ToolStates;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ToolsUtilities;

namespace SvgPlugin.Managers
{
    static class SvgStandardAdder
    {
        internal static void AddSvgStandard()
        {
            var targetFile = ProjectState.Self.ProjectDirectory + "Standards/Svg.gutx";
            FileManager.SaveEmbeddedResource(
                typeof(SvgStandardAdder).Assembly, 
                "SvgPlugin.Embedded.Svg.gutx", 
                targetFile);

            var gumProject = ProjectState.Self.GumProjectSave;
            var hasSvg = gumProject.StandardElementReferences.Any(item => item.Name == "Svg");
            if(!hasSvg)
            {
                var newReference = new ElementReference();
                newReference.ElementType = ElementType.Standard;
                newReference.Name = "Svg";
                gumProject.StandardElementReferences.Add(newReference);

                if(gumProject.StandardElements.Any(item => item.Name == "Svg") == false)
                {
                    GumLoadResult result = new GumLoadResult();
                    var loaded = newReference.ToElementSave<StandardElementSave>(
                        ProjectState.Self.ProjectDirectory,
                        "gutx",
                        result);

                    // load it:
                    gumProject.StandardElements.Add(loaded);
                    loaded.Initialize(DefaultStateManager.GetSvgState());
                }
                GumCommands.Self.FileCommands.TryAutoSaveProject();
            }

        }

    }
}
